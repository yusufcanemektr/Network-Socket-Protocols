using System;
using Renci.SshNet; // NuGet'ten yüklediğimiz SSH kütüphanesi

namespace SshClientApp
{
    // 1. UYGULAMANIN ÇALIŞMA NOKTASI
    internal class Program
    {
        static void Main(string[] args)
        {
            // Genel kullanıma açık SSH Test Sunucusu bilgileri
            string host = "test.rebex.net";
            int port = 22;
            string username = "demo";
            string password = "password";

            // Uzak sunucuda çalıştırılacak komut (Linux dizin listeleme komutu)
            string command = "ls -l";

            Console.WriteLine($"[SSH] {host}:{port} adresine bağlantı başlatılıyor...");

            // SSH İstemcimizi oluşturup komutu tetikliyoruz
            SecureShellClient sshClient = new SecureShellClient();
            sshClient.ExecuteCommand(host, port, username, password, command);

            Console.WriteLine("\n[SSH] İşlem tamamlandı. Çıkmak için bir tuşa basın...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Renci.SshNet kütüphanesini kullanarak uzak sunucuyla güvenli (şifreli)
    /// bağlantı kuran ve komut çalıştıran istemci sınıfı.
    /// </summary>
    public class SecureShellClient
    {
        public void ExecuteCommand(string host, int port, string username, string password, string commandText)
        {
            // 1. KİMLİK DOĞRULAMA (Authentication)
            // Hedef sunucuya gönderilecek kullanıcı adı ve şifre bilgilerini paketliyoruz
            var connectionInfo = new ConnectionInfo(host, port, username,
                new PasswordAuthenticationMethod(username, password));

            // 2. ŞİFRELİ TÜNELİ KURMA (SSH Client)
            // using bloğu, işlem bitince güvenli bağlantıyı otomatik kapatır ve belleği temizler
            using (var client = new SshClient(connectionInfo))
            {
                try
                {
                    Console.WriteLine("[SSH] Sunucuyla el sıkışılıyor...");
                    client.Connect(); // Asimetrik anahtar değişimi ve şifreleme burada başlar

                    if (client.IsConnected)
                    {
                        Console.WriteLine("[SSH] Bağlantı başarılı! Şifreli tünel kuruldu.");

                        // 3. KOMUT ÇALIŞTIRMA VE YANIT OKUMA
                        var command = client.CreateCommand(commandText);
                        string result = command.Execute(); // Komutu tünelden gönder ve yanıtı al

                        Console.WriteLine("\n--- SUNUCU ÇIKTISI ---");
                        Console.WriteLine(result);
                        Console.WriteLine("--- ÇIKTI SONU ---\n");
                    }
                    else
                    {
                        Console.WriteLine("[SSH] Bağlantı kurulamadı.");
                    }

                    // İşlem bitince bağlantıyı güvenli şekilde kes
                    client.Disconnect();
                }
                catch (Exception ex)
                {
                    // Yanlış şifre, kapalı port veya ağ hatalarını yakala
                    Console.WriteLine($"[HATA] SSH işlemi sırasında hata oluştu: {ex.Message}");
                }
            }
        }
    }
}