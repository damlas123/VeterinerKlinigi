# PatiVet — Veteriner Kliniği Randevu ve Hasta Takip Sistemi

| | |
|---|---|
| **Öğrenci Numarası** | _(öğrenci numaranız)_ |
| **Adı Soyadı** | Damla Söylemez |
| **Ders Grubu** | 1B |
| **Ders** | Web Programlama — 2026-2027 Güz Dönemi |
| **GitHub** | https://github.com/damlas123/VeterinerKlinigi |

---

## Proje hakkında

PatiVet, ASP.NET Core MVC (.NET 10) ile geliştirilen bir **veteriner kliniği randevu ve hasta takip sistemidir**.

- Klinikler; **hizmetlerini** (muayene, aşı, kısırlaştırma, diş temizliği, tıraş ve bakım…) süre ve ücretleriyle tanımlar.
- **Veteriner hekimler** uzmanlık alanları, verebildikleri hizmetler ve **müsaitlik saatleri** ile sisteme eklenir.
- **Hayvan sahipleri** üye olur, birden fazla evcil hayvan kaydeder, **aşı geçmişini** takip eder ve uygun saatlere **randevu** alır.
- Randevular *Bekliyor* durumunda oluşur; **Admin veya ilgili veteriner** onaylar ya da reddeder. Sistem, **hizmet süresini dikkate alarak çakışan randevuları engeller.**
- Tamamlanan randevular için veteriner **muayene notu** girer.
- Veriler bir **REST API** üzerinden de sunulur (DTO, LINQ, ciro raporu).
- Uygulama bir **MCP (Model Context Protocol) sunucusu** içerir; böylece bir yapay zekâ asistanına *"Pamuk'un bir sonraki aşısı ne zaman?"* diye sorulduğunda asistan cevabı doğrudan bu uygulamadan alır.

## Kullanılan teknolojiler

| Alan | Teknoloji |
|---|---|
| Platform | .NET 10 (LTS), C# |
| Web | ASP.NET Core MVC, Razor, Areas |
| Veri erişimi | Entity Framework Core 10 (Code First, Migration), LINQ |
| Veritabanı | SQL Server (LocalDB) |
| Kimlik / yetki | ASP.NET Core Identity, rol bazlı + kaynak bazlı yetkilendirme, JWT |
| API | RESTful API, DTO, ProblemDetails |
| Yapay zekâ entegrasyonu | ModelContextProtocol.AspNetCore (resmî MCP C# SDK) |
| Arayüz | Bootstrap 5, Bootstrap Icons, HTML5, CSS3, JavaScript, jQuery, Chart.js |

## Roller

| Rol | Özet |
|---|---|
| **Admin** | Klinik, hizmet, veteriner ve kullanıcı yönetimi; tüm randevular; raporlar |
| **Veteriner** | Kendi randevularını onaylar/reddeder, muayene notu ve aşı kaydı girer, müsaitlik saatlerini yönetir |
| **Üye** | Evcil hayvan ekler, aşı takvimini görür, randevu alır ve takip eder |

## Dokümanlar

| Doküman | İçerik |
|---|---|
| [Aşama 1 — Gereksinim analizi ve mimari](docs/01-gereksinim-analizi-ve-mimari.md) | Gereksinimler, yetki matrisi, **ER diyagramı**, iş kuralları, katmanlı mimari, API ve MCP tasarımı |
| [10 günlük geliştirme planı](docs/gelistirme-plani.md) | Gün gün yapılacaklar ve commit mesajları |

## Geliştirme durumu

- [x] **Gün 1 —** Gereksinim analizi, ER diyagramı ve mimari tasarım
- [ ] **Gün 2 —** Solution ve katmanlı proje yapısı
- [ ] **Gün 3 —** Entity sınıfları, DbContext ve ilk migration
- [ ] **Gün 4 —** Identity, roller ve hesap sayfaları
- [ ] **Gün 5 —** Repository ve servis katmanı
- [ ] **Gün 6 —** CRUD modülleri
- [ ] **Gün 7 —** Randevu sistemi ve çakışma kontrolü
- [ ] **Gün 8 —** REST API
- [ ] **Gün 9 —** MCP sunucusu
- [ ] **Gün 10 —** Dashboard'lar, testler ve final rapor

> Kurulum adımları, API örnekleri, MCP Inspector testi ve ekran görüntüleri proje ilerledikçe bu dosyaya eklenecektir.
