# Network Socket Protocols (Telnet & SSH)

Bu proje, temel ağ protokollerinin çalışma mantığını ve güvenlik farklarını analiz etmek amacıyla **C#** ve **.NET** kullanılarak geliştirilmiş soket (socket) tabanlı istemci ve sunucu uygulamalarını içerir. 

Projenin temel amacı; Telnet protokolünün şifresiz düz metin (Clear text) zafiyetini ve SSH protokolünün asimetrik şifreleme ile sağladığı kriptografik güvenliği **Wireshark** gibi ağ analiz araçları üzerinden uygulamalı olarak test etmektir.

## 📌 İçerik
Bu depo 3 farklı konsol uygulamasından oluşmaktadır:

1. **TelnetServer:** `TcpListener` sınıfı kullanılarak 23. port üzerinden bağlantı kabul eden yerel (localhost) bir test sunucusu.
2. **TelnetClient:** `TcpClient` ve `NetworkStream` kullanılarak hedef sunucuya ham TCP bağlantısı kuran ve komut ileten istemci.
3. **SshClientApp:** `SSH.NET` kütüphanesi kullanılarak uzak sunucuyla el sıkışan (handshake) ve komutları şifreli bir tünel (Ciphertext) içerisinden gönderen güvenli istemci.

## 🚀 Nasıl Çalıştırılır?

### Telnet Testi (Localhost)
1. Visual Studio üzerinden önce `TelnetServer` projesini çalıştırın. Sunucu 23. portu dinlemeye başlayacaktır.
2. Ardından `TelnetClient` projesini çalıştırarak yerel sunucuya komut gönderin.
3. Arka planda **Wireshark** ile "Adapter for loopback traffic capture" arayüzünü dinleyerek, iletilen paketlerin şifresiz (Clear text) olduğunu gözlemleyebilirsiniz.

### SSH Testi
1. `SshClientApp` projesini çalıştırın.
2. Uygulama otomatik olarak genel kullanıma açık bir test sunucusuna (test.rebex.net) bağlanacak ve komut çalıştıracaktır.
3. Wireshark üzerinden ağ trafiğini dinlediğinizde, Telnet'in aksine tüm paketlerin tamamen şifrelenmiş olduğunu göreceksiniz.

## 👨‍💻 Geliştirici
**Yusuf Can Emektar**