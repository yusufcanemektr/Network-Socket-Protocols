# Güvenli Ağ Soket Protokolleri ve Uzaktan Erişim Simülasyonu (C#)

Bu proje; ağ üzerinden çalışan cihazların uzaktan yönetim protokollerini (Telnet ve SSH) mikro ölçekte simüle eden, modern ağ güvenliği ve kriptografi felsefelerini (AAA Mimarisi, Asimetrik/Simetrik Şifreleme) kod seviyesinde ele alan gelişmiş bir C# konsol uygulamasıdır.

## 🚀 Projenin Evrimi: Neden ve Ne Değişti?

### ❌ Önceki Sürüm (Temel Soket Mimarisi)
* **Hard-coded Yapı:** Komutlar sabitti ve esneklik yoktu.
* **Şifresiz İletişim (Clear-text):** Veriler ağda ham metin olarak dolaşıyordu, güvenlik katmanı yoktu.
* **Single-Threading:** Sunucu aynı anda yalnızca tek bir istemcinin bağlantısını kabul edebiliyor, diğerleri bekletiliyordu.
* **Yetersiz Hata Yönetimi:** Beklenmeyen durumlarda uygulama doğrudan çöküyordu.

### ✅ Yeni Sürüm (Profesyonel Kriptografik Simülasyon)
* **Hibrit Kriptografi Tüneli:** Bağlantı başında RSA (2048-bit) ile güvenli anahtar takası yapıldıktan sonra tüm oturum AES-256 motoruyla şifrelenir.
* **AAA Güvenlik Standardı:** 
  * *Authentication:* Parolalar SHA-256 Hash algoritmalarıyla doğrulanır.
  * *Authorization:* Misafir ve Yönetici rolleri ayrılmıştır. `enable` komutu ile Cisco cihazlarındakine benzer Privileged EXEC (`#`) moduna geçiş yapılır.
  * *Accounting:* Tüm istemci eylemleri thread-safe (`lock`) mekanizmasıyla tarih damgası eklenerek `server_activity.log` dosyasına işlenir.
* **Multi-Threading:** `Task.Run` mimarisi sayesinde sunucu, aynı anda birden fazla istemcinin oturumunu eşzamanlı olarak yönetebilir.
* **Gerçek Ağ Araçları:** Sunucu makinesi üzerinden hedef IP'lere gerçek ICMP Ping istekleri atılarak TTL ve süre analizleri yapılabilir.

---

## 🛠️ Sistem Mimarisi ve Bileşenler

Proje iki ana bağımsız modülden oluşur:
1. **NetworkServer (`TelnetServer` klasörü):** İstemci bağlantılarını dinleyen, şifreleme anahtarlarını yöneten ve komut setlerini işleyen çoklu iş parçacıklı sunucu daemon'u.
2. **NetworkClient (`SshClientApp` klasörü):** Sunucuyla RSA/AES el sıkışması yapan, maskelenmiş parola girişi (`*`) sunan interaktif terminal istemcisi.

---

## ⚙️ Kurulum ve Çalıştırma

1. Bu projeyi bilgisayarınıza klonlayın.
2. Çözüm dosyasını Visual Studio ile açın.
3. Önce **`NetworkServer`** (TelnetServer) projesine sağ tıklayıp *Debug -> Start New Instance* diyerek sunucuyu başlatın.
4. Ardından **`NetworkClient`** (SshClientApp) projesini aynı şekilde çalıştırarak istemciyi ayağa kaldırın.
5. **Giriş Bilgileri:**
   * Kullanıcı Adı: `admin`
   * Parola: `123`
6. Yönetici yetkileri için komut satırına **`enable`** yazıp şifre olarak **`admin123`** girebilirsiniz.