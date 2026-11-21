# Aşama 2 — Solution ve Proje Yapısının Oluşturulması

> **Gün:** 2  ·  **Önceki aşama:** [Aşama 1 — Gereksinim analizi ve mimari](01-gereksinim-analizi-ve-mimari.md)

## 1. Bu aşamada ne yapıyoruz?

Aşama 1'de tasarladığımız 5 katmanlı mimariyi gerçek bir Visual Studio solution'ına dönüştürüyoruz:

1. Bir **solution** (`VeterinerKlinigi.slnx`) ve içinde **5 proje** oluşturuyoruz.
2. Projeler arasındaki **referansları** mimarideki bağımlılık yönüne göre kuruyoruz.
3. Tüm projelerde ortak olan ayarları (**.NET 10**, nullable, implicit usings) tek dosyada topluyoruz.
4. Tüm NuGet paket sürümlerini tek dosyada **sabitliyoruz** (.NET 11 paketlerine yanlışlıkla geçmemek için).
5. **API projesini Web projesine bağlıyoruz** ve bunu gerçekten çalışan bir uçla (`/api/system/status`) doğruluyoruz.
6. Bütün sayfaların kullanacağı **tema ve ana sayfa iskeletini** (Bootstrap 5, açık yeşil/pastel) hazırlıyoruz.

Bu aşamanın sonunda henüz veritabanı yok; ama uygulama derleniyor, açılıyor ve katmanlar birbirini görüyor.

## 2. Ön koşullar

| Araç | Sürüm | Kontrol |
|---|---|---|
| Visual Studio | **2026** (v18) — "ASP.NET and web development" iş yükü seçili | Yardım → Hakkında |
| .NET SDK | **10.0.1xx** veya üzeri | Terminal: `dotnet --list-sdks` → listede `10.0.x` olmalı |
| SQL Server LocalDB | VS ile birlikte gelir (Aşama 3'te kullanılacak) | `sqllocaldb info` → `MSSQLLocalDB` |
| GitHub Desktop | Güncel | — |

> `dotnet --list-sdks` çıktısında 10.0 yoksa <https://dotnet.microsoft.com/download/dotnet/10.0> adresinden **.NET 10 SDK**'yı kurun. Bilgisayarınızda sadece .NET 11 SDK varsa da proje derlenir (SDK 11, net10.0 hedefleyebilir); ama ödev .NET 10 istediği için 10 SDK'nın kurulu olması önerilir.

## 3. Oluşturulan dosyalar

```text
VeterinerKlinigi/                               (depo kökü)
├── VeterinerKlinigi.slnx                       ← Solution (yeni XML formatı)
├── Directory.Build.props                       ← Ortak proje ayarları
├── Directory.Packages.props                    ← Merkezi NuGet sürüm yönetimi
├── README.md                                   ← (güncellendi)
├── docs/02-solution-ve-proje-yapisi.md         ← Bu doküman
└── src/
    ├── VeterinerKlinigi.Entities/
    │   └── VeterinerKlinigi.Entities.csproj
    ├── VeterinerKlinigi.Data/
    │   └── VeterinerKlinigi.Data.csproj
    ├── VeterinerKlinigi.Business/
    │   ├── VeterinerKlinigi.Business.csproj
    │   └── DependencyInjection.cs              ← AddBusinessLayer()
    ├── VeterinerKlinigi.API/
    │   ├── VeterinerKlinigi.API.csproj
    │   ├── DependencyInjection.cs              ← AddVeterinerKlinigiApi()
    │   ├── Contracts/SystemStatusResponse.cs   ← DTO
    │   └── Controllers/SystemController.cs     ← GET /api/system/status
    └── VeterinerKlinigi.Web/
        ├── VeterinerKlinigi.Web.csproj
        ├── Program.cs                          ← Kompozisyon kökü
        ├── Properties/launchSettings.json      ← Portlar: 7080 (https), 5080 (http)
        ├── appsettings.json / appsettings.Development.json
        ├── Controllers/HomeController.cs
        ├── Models/ErrorViewModel.cs
        ├── Views/_ViewImports.cshtml, _ViewStart.cshtml
        ├── Views/Shared/_Layout.cshtml         ← Ortak tema
        ├── Views/Shared/Error.cshtml           ← Türkçe hata sayfası
        ├── Views/Shared/_ValidationScriptsPartial.cshtml
        ├── Views/Home/Index.cshtml             ← Ana sayfa
        └── wwwroot/
            ├── css/site.css                    ← Renk paleti ve bileşen stilleri
            ├── js/site.js                      ← Ortak jQuery kodları
            ├── img/pativet-logo.svg            ← Logo ve favicon
            └── lib/                            ← bootstrap 5.3.3, bootstrap-icons 1.13.2, jquery, jquery-validation
```

Entities ve Data projelerinde henüz `.cs` dosyası yok; içerikleri Aşama 3 ve 4'te yazılacak. Projelerin şimdiden oluşturulmasının sebebi referans zincirinin ve paketlerin baştan doğru kurulmasıdır.

## 4. Kurulum

### 4.1 Projeyi Visual Studio'da açma

1. Aşama 2 dosyalarının depo klasöründe olduğunu kontrol edin: kökte `VeterinerKlinigi.slnx`, `Directory.Build.props`, `Directory.Packages.props` ve `src` klasörü görünmeli.
2. Visual Studio'da **Dosya → Aç → Proje/Çözüm** → `VeterinerKlinigi.slnx`.
3. Solution Explorer'da **VeterinerKlinigi.Web** projesine sağ tıklayın → **Set as Startup Project** (Başlangıç Projesi Olarak Ayarla).
4. Visual Studio NuGet paketlerini otomatik olarak geri yükler (alt durum çubuğunda "Restoring packages…"). İnternet bağlantısı gerekir.

### 4.2 Bu yapı hangi komutlarla oluşturuldu?

Dosyalar depo klasörüne hazır olarak eklendi. Ancak sunumda "projeyi nasıl oluşturdun?" diye sorulabilir; yapı, depo kökünde şu komutlarla oluşturulup ardından dosyalar düzenlenmiştir:

```powershell
# 1) Solution (.NET 10'da varsayılan format .slnx)
dotnet new sln -n VeterinerKlinigi

# 2) Projeler
dotnet new classlib -n VeterinerKlinigi.Entities -o src/VeterinerKlinigi.Entities
dotnet new classlib -n VeterinerKlinigi.Data     -o src/VeterinerKlinigi.Data
dotnet new classlib -n VeterinerKlinigi.Business -o src/VeterinerKlinigi.Business
dotnet new classlib -n VeterinerKlinigi.API      -o src/VeterinerKlinigi.API
dotnet new mvc      -n VeterinerKlinigi.Web      -o src/VeterinerKlinigi.Web

# 3) Projeleri solution'a ekle
dotnet sln add src/VeterinerKlinigi.Entities src/VeterinerKlinigi.Data src/VeterinerKlinigi.Business src/VeterinerKlinigi.API src/VeterinerKlinigi.Web

# 4) Proje referansları (oklar her zaman aşağı doğru)
dotnet add src/VeterinerKlinigi.Data     reference src/VeterinerKlinigi.Entities
dotnet add src/VeterinerKlinigi.Business reference src/VeterinerKlinigi.Data src/VeterinerKlinigi.Entities
dotnet add src/VeterinerKlinigi.API      reference src/VeterinerKlinigi.Business
dotnet add src/VeterinerKlinigi.Web      reference src/VeterinerKlinigi.API src/VeterinerKlinigi.Business src/VeterinerKlinigi.Data

# 5) NuGet paketleri (Directory.Packages.props oluşturulduktan sonra; sürüm oradan gelir)
dotnet add src/VeterinerKlinigi.Entities package Microsoft.Extensions.Identity.Stores
dotnet add src/VeterinerKlinigi.Data     package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/VeterinerKlinigi.Data     package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/VeterinerKlinigi.Web      package Microsoft.EntityFrameworkCore.Design
dotnet add src/VeterinerKlinigi.Web      package Microsoft.EntityFrameworkCore.Tools

# 6) Gizli ayarlar için user-secrets kimliği
dotnet user-secrets init --project src/VeterinerKlinigi.Web
```

Ardından: `Class1.cs` dosyaları silindi, şablondaki `Privacy` sayfası ve `_Layout.cshtml.css` kaldırıldı, `TargetFramework`/`Nullable`/`ImplicitUsings` satırları csproj'lardan alınıp `Directory.Build.props`'a taşındı.

## 5. NuGet paketleri

Tüm sürümler `Directory.Packages.props` dosyasındadır. **11.x sürümlerine yükseltmeyin**: .NET 11 Kasım 2026'da çıktı ve NuGet yöneticisi bunları "güncelleme" olarak gösterebilir; ama bu proje .NET 10 hedefliyor.

| Paket | Sürüm | Proje | Neden? | Eklendiği aşama |
|---|---|---|---|---|
| Microsoft.Extensions.Identity.Stores | 10.0.15 | Entities | `ApplicationUser : IdentityUser` | 2 |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.15 | Data | EF Core + SQL Server sağlayıcısı | 2 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.15 | Data | `IdentityDbContext` (Identity tabloları) | 2 |
| Microsoft.EntityFrameworkCore.Design | 10.0.15 | Web | Migration oluşturma altyapısı | 2 |
| Microsoft.EntityFrameworkCore.Tools | 10.0.15 | Web | VS Package Manager Console komutları (`Add-Migration`) | 2 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.15 | API | REST API ve MCP için token doğrulama | 9 (sürümü şimdiden sabit) |
| Microsoft.AspNetCore.OpenApi | 10.0.15 | Web | API dokümanı (`/openapi/v1.json`) | 9 (sürümü şimdiden sabit) |
| ModelContextProtocol.AspNetCore | 2.4.0 | Web | Resmî MCP C# SDK | 10 (sürümü şimdiden sabit) |

> `Directory.Packages.props` içindeki `PackageVersion` satırı **paketi kurmaz**, sadece "bu paket kullanılırsa sürümü bu olsun" der. Paket, bir `.csproj` dosyasına `PackageReference` eklendiğinde kurulur. Bu yüzden JWT, OpenAPI ve MCP paketleri şu an hiçbir projeye kurulu değildir.

## 6. Dosyaların açıklamaları

### 6.1 `VeterinerKlinigi.slnx`

.NET 10 ile varsayılan hâle gelen **XML tabanlı solution formatı**. Eski `.sln` dosyasındaki GUID kalabalığı yerine okunabilir bir liste tutar. `Solution Items` klasörü, ortak ayar dosyalarının Solution Explorer'da görünmesini sağlar.

### 6.2 `Directory.Build.props`

MSBuild, bir projeyi derlerken klasör ağacında yukarı doğru `Directory.Build.props` arar ve bulduğu dosyayı projeye **otomatik ekler**. Böylece `net10.0`, `Nullable` ve `ImplicitUsings` ayarlarını 5 projeye ayrı ayrı yazmıyoruz (kod tekrarından kaçınma).

- `Nullable=enable`: `string? x` ile `string x` arasındaki farkı derleyiciye kontrol ettirir; `NullReferenceException` hatalarını çalıştırmadan önce yakalarız.
- `ImplicitUsings=enable`: `System`, `System.Linq`, `System.Collections.Generic` gibi using'ler her dosyaya otomatik eklenir.

### 6.3 `Directory.Packages.props` (Central Package Management)

`ManagePackageVersionsCentrally=true` ile paket sürümleri sadece bu dosyada yazılır; `.csproj`'larda `<PackageReference Include="..." />` şeklinde **sürümsüz** referans verilir. Faydası: EF Core'un tüm parçaları (SqlServer, Design, Tools) her projede **aynı sürümde** kalır.

### 6.4 Proje dosyaları (`.csproj`)

| Proje | SDK | Öne çıkan satır | Açıklama |
|---|---|---|---|
| Entities | `Microsoft.NET.Sdk` | `Identity.Stores` paketi | En alt katman, başka projeye referans yok |
| Data | `Microsoft.NET.Sdk` | `→ Entities` | EF Core ve Identity EF paketleri burada |
| Business | `Microsoft.NET.Sdk` | `→ Data, Entities` | EF Core'u Data üzerinden dolaylı (transitive) olarak görür |
| API | `Microsoft.NET.Sdk` | `<FrameworkReference Include="Microsoft.AspNetCore.App" />` | Class Library olduğu halde `ControllerBase`, `[ApiController]` gibi ASP.NET Core tiplerini kullanabilmesi için |
| Web | `Microsoft.NET.Sdk.Web` | `UserSecretsId`, `→ API, Business, Data` | Çalıştırılan proje; tüm katmanları bilir |

`UserSecretsId`: Aşama 9'da JWT imzalama anahtarını `dotnet user-secrets set "Jwt:Key" "..."` komutuyla saklayacağız. Bu değer proje klasöründe değil, `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` dosyasında tutulur, yani **GitHub'a gitmez** (ödevdeki "API anahtarları depoya konmamalıdır" şartı).

### 6.5 `Program.cs` — kompozisyon kökü

Uygulama iki bölümden oluşur:

**1) Servis kayıtları (`builder.Services`)** — Dependency Injection konteynerine "bir sınıf X isterse ona Y'yi ver" bilgisini kaydederiz.

```csharp
builder.Services.AddBusinessLayer();          // Business katmanının servisleri
builder.Services
    .AddControllersWithViews()                // MVC (Controller + Razor View)
    .AddVeterinerKlinigiApi();                // API projesindeki controller'lar
```

Web projesi, Business'ın içinde hangi sınıfların olduğunu bilmez; sadece `AddBusinessLayer()` metodunu çağırır. Her katman kendi kayıtlarından sorumludur.

**2) Middleware hattı (`app.Use...`)** — Her HTTP isteği bu hattan **yukarıdan aşağıya** geçer:

| Sıra | Middleware | Görevi |
|---|---|---|
| 1 | `UseExceptionHandler` / `UseHsts` | (Sadece Production) Hata olursa `/Home/Error`'a yönlendir; tarayıcıya "hep HTTPS kullan" de |
| 2 | `UseHttpsRedirection` | `http://` isteklerini `https://`'e yönlendir |
| 3 | `UseRouting` | İsteğin hangi controller/action'a gideceğini belirle |
| 4 | `UseAuthorization` | `[Authorize]` kontrolü (Aşama 5'te önüne `UseAuthentication` gelecek) |
| 5 | `MapStaticAssets` | `wwwroot` dosyalarını sıkıştırarak ve önbelleğe uygun şekilde sun |
| 6 | `MapControllerRoute("areas")` | `/Admin/Clinics/Edit/5` gibi Area adresleri |
| 7 | `MapControllerRoute("default")` | `/Home/Index` gibi normal adresler |
| 8 | `MapControllers` | `[Route("api/...")]` ile işaretli API uçları |

> **Sıra neden önemli?** Örneğin `UseAuthorization`, `UseRouting`'den önce yazılsaydı hangi action'a gidildiği henüz bilinmeyeceği için `[Authorize]` kontrolü yapılamazdı. Area rotası da varsayılan rotadan önce yazılmalıdır; yoksa `/Admin/Clinics` adresi "Admin adlı controller" olarak yorumlanır.

### 6.6 API projesinin Web'e bağlanması: Application Part

```csharp
public static IMvcBuilder AddVeterinerKlinigiApi(this IMvcBuilder mvcBuilder)
{
    mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
    return mvcBuilder;
}
```

ASP.NET Core, controller'ları **çalışan uygulamanın derlemesinde** arar. API ayrı bir derleme (`VeterinerKlinigi.API.dll`) olduğu için `AddApplicationPart` ile "bu derlemedeki controller'ları da kullan" diyoruz. `this IMvcBuilder` ifadesi bunu bir **extension method** yapar; bu yüzden `AddControllersWithViews().AddVeterinerKlinigiApi()` şeklinde zincirleme çağırabiliyoruz.

**Neden ayrı bir Web API uygulaması değil?** (Mimari karar K-01) Tek uygulama = tek port, tek oturum, CORS ayarı yok. Tarayıcıdaki jQuery kodu, oturum çerezini kullanarak aynı adresteki `/api/...` uçlarını çağırabilir.

### 6.7 `SystemController` — ilk API ucu

```csharp
[ApiController]                 // Otomatik model doğrulama, 400 cevapları, [FromBody] çıkarımı
[Route("api/system")]           // Bu controller'ın adres öneki
public sealed class SystemController : ControllerBase   // View desteği olmayan, API'ye özel taban sınıf
{
    public SystemController(IHostEnvironment environment, TimeProvider timeProvider) { ... }

    [HttpGet("status")]         // GET /api/system/status
    public ActionResult<SystemStatusResponse> GetStatus() => Ok(new SystemStatusResponse(...));
}
```

- **Constructor injection:** `IHostEnvironment` ASP.NET Core'dan, `TimeProvider` ise **Business katmanının** `AddBusinessLayer()` ile kaydettiği servisten gelir. Bu uç çalışıyorsa Web → API → Business zincirinin doğru kurulduğu kanıtlanmış olur.
- **DTO:** Cevap olarak bir `record` (`SystemStatusResponse`) döner. Aşama 9'daki tüm API uçları da entity yerine DTO döndürecek.
- `Controller` yerine `ControllerBase`: API controller'larında View'a ihtiyaç yoktur.

### 6.8 `TimeProvider` — neden `DateTime.Now` değil?

```csharp
services.TryAddSingleton(TimeProvider.System);
```

Randevu kurallarında "geçmiş tarihe randevu verilmez" kontrolü yapacağız. Kod doğrudan `DateTime.Now` kullanırsa, testte "saat 10:00'ken 09:30'a randevu alınamamalı" gibi bir durumu sınamak mümkün olmaz. `TimeProvider` ise bir servistir; testte sabit saat döndüren sahte bir sürümüyle değiştirilebilir. `Singleton` çünkü durumu yoktur, tek bir örnek tüm uygulamaya yeter. `TryAdd...`: daha önce kaydedilmişse (ör. testte sahte saat) üzerine yazmaz.

### 6.9 `launchSettings.json`

Portları sabitledik: **https://localhost:7080** ve **http://localhost:5080**. README, Postman ve MCP Inspector adımları bu adresleri kullanacak. İki profil var:

- `https`: Normal geliştirme (varsayılan).
- `http`: Aşama 10'da MCP Inspector ile test için (Node.js tabanlı Inspector, geliştirme sertifikasına güvenmeyebilir).

### 6.10 Tema: `_Layout.cshtml`, `site.css`, `site.js`

- `_Layout.cshtml`: Tüm sayfaların ortak iskeleti (menü, içerik alanı, alt bilgi). `@RenderBody()` sayfanın kendi içeriğini, `@RenderSectionAsync("Scripts")` sayfaya özel JavaScript'i yerleştirir. `lang="tr"` ile Türkçe olduğu belirtilir.
- `site.css`: Bootstrap'in CSS değişkenlerini (`--bs-primary` vb.) ezerek tüm Bootstrap bileşenlerinin **açık yeşil** ana rengi kullanmasını sağlar. `pv-` önekli sınıflar (`pv-card`, `pv-icon`, `pv-badge`) projeye özel bileşenlerdir; dashboard'larda ve CRUD sayfalarında da aynı sınıflar kullanılarak **ortak tasarım dili** korunur. `prefers-reduced-motion` ile hareket hassasiyeti olan kullanıcılar için animasyon kapanır.
- `site.js` (jQuery): aktif menü bağlantısını vurgular, Bootstrap tooltip'lerini başlatır, bilgi mesajlarını 6 saniye sonra kapatır.
- Kütüphaneler (`wwwroot/lib`) CDN yerine **yerel** olarak tutulur: internet olmadan da çalışır, sunumda risk yoktur.
- `asp-append-version="true"`: `site.css?v=...` şeklinde dosya içeriğine göre değişen bir parametre ekler; CSS değiştiğinde tarayıcı eski önbelleği kullanmaz.

## 7. Çalıştırma

**Visual Studio:** Üst araç çubuğunda yeşil ok yanındaki profilden `https` seçili olsun → **F5** (veya hata ayıklamasız: **Ctrl+F5**).

İlk çalıştırmada "ASP.NET Core SSL sertifikasına güvenilsin mi?" sorusu gelirse **Evet** deyin.

**Terminal (depo kökünde):**

```powershell
dotnet restore
dotnet build
dotnet run --project src/VeterinerKlinigi.Web --launch-profile https
```

Sertifika uyarısı alırsanız bir kez: `dotnet dev-certs https --trust`

## 8. Test adımları

| # | Ne yapılacak? | Beklenen sonuç |
|---|---|---|
| 1 | **Derle** (Ctrl+Shift+B) | `Build: 5 succeeded, 0 failed` (Hata Listesi'nde hata olmamalı) |
| 2 | <https://localhost:7080> adresini aç | Yeşil temalı PatiVet ana sayfası, "Ana Sayfa" menüsü vurgulu |
| 3 | <https://localhost:7080/api/system/status> adresini aç | JSON: `"status":"Çalışıyor"`, `"framework":".NET 10.0.x"`, `"environment":"Development"` |
| 4 | Tarayıcıda **F12 → cihaz araç çubuğu** (Ctrl+Shift+M), iPhone boyutu seç | Menü hamburger düğmesine dönüşür, kartlar alt alta dizilir |
| 5 | <https://localhost:7080/olmayan-sayfa> | 404 (Aşama 11'de özel 404 sayfası eklenecek) |
| 6 | Postman: `GET https://localhost:7080/api/system/status` | `200 OK`, `Content-Type: application/json` |
| 7 | GitHub Desktop → Changes sekmesi | `bin/`, `obj/`, `.vs/` klasörleri **listelenmemeli** |

## 9. Sık karşılaşılan hatalar

| Hata | Neden | Çözüm |
|---|---|---|
| `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0` | .NET 10 SDK kurulu değil | .NET 10 SDK'yı kurun, VS'yi yeniden başlatın |
| `NU1101/NU1102: Unable to find package` | NuGet kaynağı kapalı | VS → Araçlar → Seçenekler → NuGet Package Manager → Package Sources → `nuget.org` işaretli olsun |
| `NU1008: Projects that use central package version management should not define the version` | Bir csproj'a sürümlü `PackageReference` eklenmiş | Sürümü csproj'dan silin, `Directory.Packages.props`'a yazın |
| NuGet yöneticisi 11.0.0 güncellemesi öneriyor | .NET 11 çıktı | **Güncellemeyin**; 10.0.x'te kalın |
| `Failed to determine the https port for redirect` uyarısı | `http` profiliyle çalıştırıldı | Normaldir; `https` profilinde görünmez |
| `Address already in use` (port 7080/5080) | Uygulama zaten açık | Çalışan örneği kapatın veya `launchSettings.json`'da portu değiştirin |
| Sayfa stilsiz (düz HTML) görünüyor | `wwwroot/lib` klasörü eksik | `src/VeterinerKlinigi.Web/wwwroot/lib` altında `bootstrap`, `bootstrap-icons`, `jquery` klasörleri olmalı |

## 10. Aşama sonu kontrol listesi

| Gereksinim | Durum |
|---|---|
| Önerilen 5 katmanlı yapı (Web, Data, Entities, Business, API) | ✅ |
| Proje referansları mimarideki yöne uygun (döngüsel referans yok) | ✅ |
| .NET 10 hedefleniyor | ✅ `Directory.Build.props` |
| Paket sürümleri .NET 10 ile uyumlu ve sabit | ✅ 10.0.15 / MCP 2.4.0 |
| Gizli bilgiler için altyapı (user-secrets) | ✅ `UserSecretsId` |
| Bootstrap 5, HTML5, CSS3, JavaScript, jQuery | ✅ Tema ve `site.js` |
| API katmanı gerçekten çalışıyor | ✅ `/api/system/status` |
| Kullanılmayan teknoloji eklenmedi | ✅ JWT/OpenAPI/MCP sadece sürüm olarak sabit, henüz kurulu değil |
| Derleme hatasız | ✅ |

**Bir sonraki aşamada (Gün 3):** Entity sınıfları, enum'lar, `AppDbContext`, Fluent API konfigürasyonları ve ilk migration.

## 11. Sunumda sorulabilecek sorular

**S: Neden 5 ayrı proje var, hepsi tek projede olamaz mıydı?**
C: Olabilirdi, ama o zaman iş kuralları controller'ların içine dağılırdı. Ödev, MVC + API + MCP'nin **aynı iş kurallarını** kullanmasını istiyor. Kuralları ayrı bir Business projesine koyduğumuzda üç kanal da aynı metodu çağırır; kural bir kez yazılır.

**S: API projesi neden Class Library, kendi başına çalışmıyor mu?**
C: Bilerek böyle tasarladık. API controller'ları Web uygulamasının içinde "application part" olarak çalışır. Tek port, tek kimlik doğrulama, CORS yok. Katman ayrımı ise korunuyor (API kodu ayrı projede).

**S: `Directory.Packages.props` ne işe yarıyor?**
C: Tüm NuGet sürümlerini tek yerde tutuyor. EF Core'un parçalarının farklı projelerde farklı sürümde olmasını engelliyor ve .NET 11 paketlerine yanlışlıkla geçmeyi önlüyor.

**S: Middleware sırası değişirse ne olur?**
C: Örneğin `UseAuthorization`, `UseRouting`'den önce gelirse hangi endpoint'e gidildiği bilinmediği için yetki kontrolü yapılamaz. Aşama 5'te `UseAuthentication` da mutlaka `UseAuthorization`'dan önce eklenecek: önce "kimsin?", sonra "buna yetkin var mı?".

**S: `TimeProvider` neden kullandınız?**
C: "Şu an"ı bir servis olarak alıyoruz. Böylece "geçmiş tarihe randevu verilmez" kuralını test ederken saati sabitleyebiliyoruz.

## 12. Commit

GitHub Desktop'ta:

- **Summary:** `chore: katmanlı solution yapısı ve proje referansları oluşturuldu`
- **Description:**
  ```text
  - Entities, Data, Business, API, Web projeleri eklendi
  - Directory.Build.props ve merkezi paket yönetimi (10.0.x)
  - API projesi Web'e application part olarak bağlandı (/api/system/status)
  - Bootstrap 5 tabanlı ortak tema ve ana sayfa
  ```

→ **Commit to main** → **Push origin**
