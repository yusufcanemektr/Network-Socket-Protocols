using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TelnetServer
{
    /// <summary>
    /// Basit bir Telnet dinleyici (Listener) sunucusu.
    /// Belirtilen portu dinler, gelen bağlantıyı kabul eder ve iki yönlü mesajlaşmayı sağlar.
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            // Sunucuyu tüm yerel ağ arayüzlerinde (IPAddress.Any) ve 23 numaralı Telnet portunda tanımla
            TcpListener sunucu = new TcpListener(IPAddress.Any, 23);

            try
            {
                // Dinlemeyi başlat
                sunucu.Start();
                Console.WriteLine("[Server] 23. portta tetikte bekliyorum...");

                // Gelen bağlantıyı bekle (Bağlantı gelene kadar kod bu satırda donup bekler)
                using (TcpClient istemci = sunucu.AcceptTcpClient())
                {
                    Console.WriteLine("[Server] Bağlantı alındı.");

                    // İstemci ile veri alışverişi yapacağımız köprüyü (Stream) kur
                    using (NetworkStream stream = istemci.GetStream())
                    {
                        // --- 1. İSTEMCİDEN GELEN MESAJI OKUMA AŞAMASI ---
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);

                        // Elektrik sinyallerini (Baytları) okunabilir ASCII formatına çevir
                        string gelenMesaj = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"[Server] Gelen komut: {gelenMesaj}");

                        // --- 2. İSTEMCİYE CEVAP GÖNDERME AŞAMASI ---
                        // Not: \n karakteri alt satıra geçmek ve metnin bittiğini belirtmek için önemlidir
                        byte[] cevap = Encoding.ASCII.GetBytes("Merhaba, bağlantı kuruldu.\n");
                        stream.Write(cevap, 0, cevap.Length);

                        Console.WriteLine("[Server] Cevap gönderildi.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Portun dolu olması veya ağ erişim engeli gibi durumları yakala
                Console.WriteLine($"[HATA] Sunucu çalışırken bir hata oluştu: {ex.Message}");
            }
            finally
            {
                // İşlem başarılı da olsa hata da verse sunucuyu güvenli bir şekilde kapat
                sunucu.Stop();
                Console.WriteLine("\n[Server] Sunucu kapatıldı. Çıkmak için ENTER tuşuna basın...");
                Console.ReadLine();
            }
        }
    }
}