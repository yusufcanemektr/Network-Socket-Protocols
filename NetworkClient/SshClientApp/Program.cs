using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace SshClientApp
{
    public class AesMotoru
    {
        private byte[] Key;
        private byte[] IV;
        public bool AktifMi { get; private set; } = false;

        public void RastgeleAnahtarUret()
        {
            using (Aes aesAlg = Aes.Create())
            {
                Key = aesAlg.Key;
                IV = aesAlg.IV;
                AktifMi = true;
            }
        }

        public byte[] AnahtarlariBirlestir()
        {
            byte[] birlesik = new byte[48];
            Buffer.BlockCopy(Key, 0, birlesik, 0, 32);
            Buffer.BlockCopy(IV, 0, birlesik, 32, 16);
            return birlesik;
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
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
            catch
            {
                return "KRIPTO_HATASI";
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            Console.WriteLine("[İSTEMCİ] Güvenli Ağ İstemcisi Başlatılıyor...");
            string hedefIp = "127.0.0.1";

            TcpClient client = new TcpClient();

            try
            {
                client.Connect(hedefIp, 23);
                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                string handshake = reader.ReadLine();
                string aktifProtokol = "TELNET";
                AesMotoru kripto = new AesMotoru();

                if (handshake != null && handshake.StartsWith("[HANDSHAKE:"))
                {
                    aktifProtokol = handshake.Split(':')[1].TrimEnd(']');
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[SİSTEM] Sunucuyla el sıkışıldı. Protokol Modu: {aktifProtokol}");

                if (aktifProtokol == "SSH")
                {
                    string pubKeyMesaj = reader.ReadLine();
                    if (pubKeyMesaj != null && pubKeyMesaj.StartsWith("[SSH_PUBKEY:"))
                    {
                        string[] rsaParts = pubKeyMesaj.Split(':')[1].TrimEnd(']').Split('|');

                        kripto.RastgeleAnahtarUret();
                        byte[] birlestirilmisAES = kripto.AnahtarlariBirlestir();

                        using (RSA rsa = RSA.Create())
                        {
                            RSAParameters pubKey = new RSAParameters
                            {
                                Modulus = Convert.FromBase64String(rsaParts[0]),
                                Exponent = Convert.FromBase64String(rsaParts[1])
                            };
                            rsa.ImportParameters(pubKey);

                            byte[] sifreliAES = rsa.Encrypt(birlestirilmisAES, RSAEncryptionPadding.Pkcs1);
                            writer.WriteLine($"[SSH_AESKEY:{Convert.ToBase64String(sifreliAES)}]");
                        }
                        Console.WriteLine("[SİSTEM] RSA ile AES Oturum Anahtarı güvenle iletildi. Tünel şifrelendi!");
                    }
                }
                Console.ResetColor();

                // 1. Kullanıcı Adı
                Console.Write(AgdanOku(reader, aktifProtokol, kripto));
                AgdanGonder(writer, Console.ReadLine(), aktifProtokol, kripto);

                // 2. Parola
                Console.Write(AgdanOku(reader, aktifProtokol, kripto));
                AgdanGonder(writer, SifreliGirisOku(), aktifProtokol, kripto);
                Console.WriteLine();

                // Giriş Sonucu
                string girisSonucu = AgdanOku(reader, aktifProtokol, kripto);

                if (string.IsNullOrEmpty(girisSonucu) || girisSonucu.StartsWith("[HATA]"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(girisSonucu ?? "[HATA] Sunucu yanıt vermedi.");
                    Console.ResetColor();
                    return;
                }

                string kullaniciAdi = girisSonucu.Replace("[BASARILI] ", "").Trim();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Oturum başarıyla açıldı.");
                Console.ResetColor();

                string promptKarakteri = ">";

                while (true)
                {
                    Console.Write($"{kullaniciAdi}@{hedefIp}{promptKarakteri} ");
                    string komut = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(komut)) continue;

                    AgdanGonder(writer, komut, aktifProtokol, kripto);

                    if (komut.ToUpper() == "CIKIS") break;

                    string cevap = AgdanOku(reader, aktifProtokol, kripto);
                    if (string.IsNullOrEmpty(cevap)) break;

                    if (cevap == "[ENABLE_PAROLA_GIR]")
                    {
                        Console.Write("Yönetici (Enable) Parolası: ");
                        AgdanGonder(writer, SifreliGirisOku(), aktifProtokol, kripto);
                        Console.WriteLine();

                        cevap = AgdanOku(reader, aktifProtokol, kripto);

                        if (cevap == "[ENABLE_BASARILI]")
                        {
                            promptKarakteri = "#";
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Yetki başarıyla yükseltildi (Privileged Mode).");
                            Console.ResetColor();
                            continue;
                        }
                    }

                    Console.WriteLine(cevap);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[HATA] {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                client.Close();
                Console.WriteLine("Bağlantı kapatıldı. Çıkmak için bir tuşa basın...");
                Console.ReadKey();
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
            return veri;
        }

        static string SifreliGirisOku()
        {
            string parola = "";
            while (true)
            {
                ConsoleKeyInfo tus = Console.ReadKey(true);
                if (tus.Key == ConsoleKey.Enter) break;
                else if (tus.Key == ConsoleKey.Backspace)
                {
                    if (parola.Length > 0)
                    {
                        parola = parola.Substring(0, parola.Length - 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    parola += tus.KeyChar;
                    Console.Write("*");
                }
            }
            return parola;
        }
    }
}