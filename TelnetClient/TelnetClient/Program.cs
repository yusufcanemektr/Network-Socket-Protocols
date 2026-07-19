using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace TelnetClientApp 
{
    // 1. UYGULAMANIN ÇALIŞMA NOKTASI
    class Program
    {
        static async Task Main(string[] args)
        {
            // Hedef sunucu bilgileri (Test için Localhost / 127.0.0.1 kullanıyoruz)
            string ip = "127.0.0.1";
            int port = 23;
            string command = "test_komutu";

            Console.WriteLine($"[Telnet] {ip}:{port} adresine bağlantı başlatılıyor...");

            // TelnetClient sınıfından bir nesne üretip bağlantı sürecini tetikliyoruz
            TelnetClient client = new TelnetClient();
            await client.ConnectAndSend(ip, port, command);

            Console.WriteLine("\n[Telnet] Bağlantı kapatıldı. Çıkmak için bir tuşa basın...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// Telnet protokolü üzerinden ham TCP bağlantısı kurarak 
    /// ağdaki bir cihaza komut gönderen ve yanıt okuyan istemci sınıfı.
    /// </summary>
    public class TelnetClient
    {
        public async Task ConnectAndSend(string ip, int port, string command)
        {
            Console.WriteLine($"[Telnet] {ip}:{port} adresine bağlanılıyor...");
            try
            {
                // TcpClient, işlem bittiğinde belleği temizlemesi için 'using' bloğu içinde tanımlanır
                using (TcpClient client = new TcpClient())
                {
                    // Hedef adrese asenkron (ana akışı bloklamadan) bağlan
                    await client.ConnectAsync(ip, port);

                    // Verilerin akıp gideceği ağ borusunu (Stream) aç
                    using (NetworkStream stream = client.GetStream())
                    {
                        // --- 1. KOMUT GÖNDERME AŞAMASI ---
                        // Metin tabanlı komutu ASCII formatında bayt dizisine çevir
                        // Not: \n karakteri komutun bittiğini (Enter) ifade eder
                        byte[] data = Encoding.ASCII.GetBytes(command + "\n");
                        await stream.WriteAsync(data, 0, data.Length);
                        Console.WriteLine($"[Telnet] Gönderilen komut: {command}");

                        // --- 2. YANIT OKUMA AŞAMASI ---
                        // Sunucudan gelecek yanıtı tutmak için 1024 baytlık (1KB) bir tampon oluştur
                        byte[] responseData = new byte[1024];
                        int bytesRead = await stream.ReadAsync(responseData, 0, responseData.Length);

                        // Gelen elektrik sinyallerini (baytları) tekrar okunabilir metne çevir
                        string response = Encoding.ASCII.GetString(responseData, 0, bytesRead);
                        Console.WriteLine($"[Telnet] Sunucunun yanıtı: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Portun kapalı olması veya hedefin ulaşılamaz olması gibi ağ hatalarını yakala
                Console.WriteLine($"[HATA] Bağlantı hatası: {ex.Message}");
            }
        }
    }
}