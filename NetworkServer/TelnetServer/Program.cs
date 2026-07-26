using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace NetworkServer
{
    public class AesMotoru
    {
        private byte[] Key;
        private byte[] IV;
        public bool AktifMi { get; private set; } = false;

        public void KurulumYap(byte[] key, byte[] iv)
        {
            Key = key;
            IV = iv;
            AktifMi = true;
        }

        public string Sifrele(string duzMetin)
        {
            if (!AktifMi || string.IsNullOrEmpty(duzMetin)) return duzMetin;
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt, Encoding.UTF8))
                    {
                        swEncrypt.Write(duzMetin);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public string Coz(string sifreliMetin)
        {
            if (!AktifMi || string.IsNullOrEmpty(sifreliMetin)) return sifreliMetin;
            try
            {
                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = Key;
                    aesAlg.IV = IV;
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(sifreliMetin)))
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8))
                    {
                        return srDecrypt.ReadToEnd().Trim();
                    }
                }
            }
            catch
            {
                return "KRIPTO_HATASI";
            }
        }
    }

    class Kullanici
    {
        public string Ad { get; set; }
        public string ParolaHash { get; set; }
        public bool EnableYetkisiVarMi { get; set; }
    }

    class SunucuAyarlari
    {
        public int Port { get; set; } = 23;
        public string AktifProtokol { get; set; } = "SSH";
        public string EnableParolaHash { get; set; } = "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9"; // admin123
        public List<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>
        {
            new Kullanici { Ad = "misafir", ParolaHash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3", EnableYetkisiVarMi = false },
            new Kullanici { Ad = "admin", ParolaHash = "a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3", EnableYetkisiVarMi = true }
        };
    }

    class Program
    {
        static Stopwatch uptime = new Stopwatch();
        static readonly object logKilidi = new object();
        static SunucuAyarlari ayarlar;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            uptime.Start();
            ayarlar = new SunucuAyarlari();

            TcpListener server = new TcpListener(IPAddress.Any, ayarlar.Port);
            server.Start();
            Logla($"[SUNUCU] Başlatıldı. Port: {ayarlar.Port} | Varsayılan Mod: {ayarlar.AktifProtokol}");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Task.Run(() => IstemciyiIsle(client));
            }
        }

        static void AgdanGonder(StreamWriter writer, string mesaj, string protokol, AesMotoru kripto)
        {
            if (protokol == "SSH") writer.WriteLine(kripto.Sifrele(mesaj));
            else writer.WriteLine(mesaj);
        }

        static string AgdanOku(StreamReader reader, string protokol, AesMotoru kripto)
        {
            string veri = reader.ReadLine();
            if (veri == null) return null;
            if (protokol == "SSH") return kripto.Coz(veri);
            return veri?.Trim();
        }

        static void IstemciyiIsle(TcpClient client)
        {
            string istemciIP = "Bilinmiyor";
            try
            {
                var ep = client.Client.RemoteEndPoint as IPEndPoint;
                if (ep != null) istemciIP = ep.Address.ToString();
            }
            catch { }

            AesMotoru kripto = new AesMotoru();

            try
            {
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                writer.WriteLine($"[HANDSHAKE:{ayarlar.AktifProtokol}]");

                if (ayarlar.AktifProtokol == "SSH")
                {
                    using (RSA rsa = RSA.Create(2048))
                    {
                        RSAParameters pubKey = rsa.ExportParameters(false);
                        string mod = Convert.ToBase64String(pubKey.Modulus);
                        string exp = Convert.ToBase64String(pubKey.Exponent);
                        writer.WriteLine($"[SSH_PUBKEY:{mod}|{exp}]");

                        string rsaSifreliMesaj = reader.ReadLine();
                        if (rsaSifreliMesaj == null || !rsaSifreliMesaj.StartsWith("[SSH_AESKEY:"))
                        {
                            client.Close(); return;
                        }

                        string sifreliAesBase64 = rsaSifreliMesaj.Split(':')[1].TrimEnd(']');
                        byte[] sifreliAesBytes = Convert.FromBase64String(sifreliAesBase64);
                        byte[] cozulmusAesBytes = rsa.Decrypt(sifreliAesBytes, RSAEncryptionPadding.Pkcs1);

                        byte[] sessionKey = new byte[32];
                        byte[] sessionIV = new byte[16];
                        Buffer.BlockCopy(cozulmusAesBytes, 0, sessionKey, 0, 32);
                        Buffer.BlockCopy(cozulmusAesBytes, 32, sessionIV, 0, 16);

                        kripto.KurulumYap(sessionKey, sessionIV);
                        Logla($"[{istemciIP}] ile AES-256 Oturum Şifrelemesi Başlatıldı.");
                    }
                }

                AgdanGonder(writer, "Kullanıcı Adı: ", ayarlar.AktifProtokol, kripto);
                string kullaniciAdi = AgdanOku(reader, ayarlar.AktifProtokol, kripto);

                AgdanGonder(writer, "Parola: ", ayarlar.AktifProtokol, kripto);
                string parola = AgdanOku(reader, ayarlar.AktifProtokol, kripto);

                if (string.IsNullOrEmpty(kullaniciAdi) || string.IsNullOrEmpty(parola))
                {
                    client.Close(); return;
                }

                string parolaHash = ComputeSha256Hash(parola);
                Kullanici aktifKullanici = ayarlar.Kullanicilar.FirstOrDefault(k => k.Ad == kullaniciAdi && k.ParolaHash == parolaHash);

                if (aktifKullanici != null)
                {
                    Logla($"[{istemciIP}] '{aktifKullanici.Ad}' olarak giriş yaptı.");
                    AgdanGonder(writer, $"[BASARILI] {aktifKullanici.Ad}", ayarlar.AktifProtokol, kripto);
                }
                else
                {
                    Logla($"[{istemciIP}] Başarısız giriş denemesi: {kullaniciAdi}");
                    AgdanGonder(writer, "[HATA] Kullanıcı adı veya parola yanlış!", ayarlar.AktifProtokol, kripto);
                    client.Close();
                    return;
                }

                bool enableModundaMi = false;

                while (true)
                {
                    string command = AgdanOku(reader, ayarlar.AktifProtokol, kripto);
                    if (string.IsNullOrEmpty(command)) break;

                    Logla($"[{istemciIP} - {aktifKullanici.Ad}] Komut: {command}");
                    string response = "";

                    string[] cmdParts = command.ToUpper().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (cmdParts.Length == 0) continue;
                    string anaKomut = cmdParts[0];

                    switch (anaKomut)
                    {
                        case "ENABLE":
                            if (!aktifKullanici.EnableYetkisiVarMi)
                            {
                                response = "[HATA] Bu komutu kullanmaya yetkiniz yok.";
                            }
                            else if (enableModundaMi)
                            {
                                response = "Zaten yönetici (enable) modundasınız.";
                            }
                            else
                            {
                                AgdanGonder(writer, "[ENABLE_PAROLA_GIR]", ayarlar.AktifProtokol, kripto);
                                string enableParola = AgdanOku(reader, ayarlar.AktifProtokol, kripto);

                                if (ComputeSha256Hash(enableParola) == ayarlar.EnableParolaHash)
                                {
                                    enableModundaMi = true;
                                    Logla($"[{istemciIP}] '{aktifKullanici.Ad}' Enable moduna geçti.");
                                    response = "[ENABLE_BASARILI]";
                                }
                                else
                                {
                                    Logla($"[{istemciIP}] '{aktifKullanici.Ad}' hatalı enable parolası girdi.");
                                    response = "[HATA] Yanlış Enable parolası!";
                                }
                            }
                            AgdanGonder(writer, response, ayarlar.AktifProtokol, kripto);
                            continue;

                        case "HELP":
                            response = enableModundaMi
                                ? "Komutlar: SAAT, PING, WHOAMI, UPTIME, DIR, HELP, CIKIS"
                                : "Komutlar: SAAT, PING, ENABLE, HELP, CIKIS (Ek komutlar için 'enable' komutunu kullanın)";
                            break;

                        case "SAAT":
                            response = "Sunucu Yerel Saati: " + DateTime.Now.ToString("HH:mm:ss");
                            break;

                        case "WHOAMI":
                            response = "Oturum Sahibi: " + aktifKullanici.Ad + (enableModundaMi ? " [Yönetici]" : " [Misafir]");
                            break;

                        case "UPTIME":
                            response = $"Sunucu Çalışma Süresi: {uptime.Elapsed:hh\\:mm\\:ss}";
                            break;

                        case "DIR":
                            if (!enableModundaMi) response = "[HATA] Bu komut için Enable moduna geçmelisiniz (#).";
                            else response = "Sunucu Dizini Dosyaları:\n  - " + string.Join("\n  - ", Directory.GetFiles(Directory.GetCurrentDirectory()).Select(Path.GetFileName));
                            break;

                        case "PING":
                            if (cmdParts.Length < 2) response = "Kullanım: PING <Adres> (Örn: PING 127.0.0.1)";
                            else
                            {
                                try
                                {
                                    using (Ping p = new Ping())
                                    {
                                        PingReply rep = p.Send(cmdParts[1], 2000);
                                        response = rep.Status == IPStatus.Success
                                            ? $"[PING BAŞARILI] Süre: {rep.RoundtripTime} ms | TTL: {rep.Options?.Ttl}"
                                            : $"[PING BAŞARISIZ] Durum: {rep.Status}";
                                    }
                                }
                                catch (Exception ex) { response = "[PING HATA] " + ex.Message; }
                            }
                            break;

                        case "CIKIS":
                            AgdanGonder(writer, "Bağlantı güvenle sonlandırılıyor...", ayarlar.AktifProtokol, kripto);
                            goto CikisYap;

                        default:
                            response = $"'{command}' tanınmayan bir komut. Yardım için 'HELP' yazın.";
                            break;
                    }

                    AgdanGonder(writer, response, ayarlar.AktifProtokol, kripto);
                }

            CikisYap:;
            }
            catch { }
            finally
            {
                Logla($"[{istemciIP}] Bağlantı kapandı.");
                client.Close();
            }
        }

        static void Logla(string mesaj)
        {
            lock (logKilidi)
            {
                string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mesaj}";
                Console.WriteLine(log);
                try { File.AppendAllText("server_activity.log", log + Environment.NewLine); } catch { }
            }
        }

        static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}