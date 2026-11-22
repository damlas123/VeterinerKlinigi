# Aşama 3 + 4 — Entity Sınıfları, DbContext ve İlk Migration

> **Gün:** 3  ·  **Önceki aşama:** [Aşama 2 — Solution ve proje yapısı](02-solution-ve-proje-yapisi.md)

## 1. Bu aşamada ne yapıyoruz?

Aşama 1'deki ER diyagramını **koda** ve ardından **gerçek bir SQL Server veritabanına** dönüştürüyoruz. Buna **Code First** yaklaşımı denir: önce C# sınıflarını yazarız, Entity Framework Core bu sınıflardan tabloları oluşturur.

1. **Aşama 3 — Entity'ler:** 10 entity sınıfı, 2 enum ve uzunluk sabitleri (`VeterinerKlinigi.Entities`).
2. **Aşama 4 — DbContext:** `AppDbContext`, her entity için Fluent API ayarları (ilişkiler, silme davranışları, CHECK kısıtları, index'ler) ve DI kaydı (`VeterinerKlinigi.Data`).
3. **Migration:** `InitialCreate` migration'ını **sen** oluşturacaksın ve veritabanını oluşturacaksın. Bu, EF Core'un en önemli komutudur; sunumda mutlaka sorulur.

## 2. Oluşturulan ve değişen dosyalar

```text
src/
├── VeterinerKlinigi.Entities/
│   ├── Constants/FieldLengths.cs           ← YENİ: metin alanı uzunluk sabitleri
│   ├── Enums/AppointmentStatus.cs          ← YENİ: Pending, Approved, Rejected, Completed, Cancelled
│   ├── Enums/PetSpecies.cs                 ← YENİ: Kedi, Köpek, Kuş, Tavşan, Kemirgen, Sürüngen, Diğer
│   ├── ApplicationUser.cs                  ← YENİ
│   ├── Clinic.cs                           ← YENİ
│   ├── Veterinarian.cs                     ← YENİ
│   ├── Service.cs                          ← YENİ
│   ├── VeterinarianService.cs              ← YENİ (çoka-çok ara tablo)
│   ├── VeterinarianAvailability.cs         ← YENİ
│   ├── Pet.cs                              ← YENİ
│   ├── Vaccination.cs                      ← YENİ
│   ├── Appointment.cs                      ← YENİ
│   └── ExaminationNote.cs                  ← YENİ
├── VeterinerKlinigi.Data/
│   ├── Context/AppDbContext.cs             ← YENİ
│   ├── Configurations/*Configuration.cs    ← YENİ: 10 dosya, her entity için bir tane
│   ├── DependencyInjection.cs              ← YENİ: AddDataLayer()
│   └── Migrations/                         ← SENİN OLUŞTURACAĞIN (Bölüm 9)
└── VeterinerKlinigi.Web/
    ├── Program.cs                          ← DEĞİŞTİ: AddDataLayer() çağrısı
    └── appsettings.json                    ← DEĞİŞTİ: ConnectionStrings
```

**Yeni NuGet paketi yok.** Gerekli tüm paketler (EF Core SqlServer 10.0.15, Identity EF 10.0.15, EF Design/Tools 10.0.15) Aşama 2'de kurulmuştu.

## 3. Entity sınıfları

Entity, veritabanındaki bir tabloya karşılık gelen sade bir C# sınıfıdır (POCO — *Plain Old CLR Object*). Her özellik bir kolondur.

```csharp
public class Pet
{
    public int Id { get; set; }                          // PK (adı "Id" olduğu için EF otomatik anlar)
    public string OwnerId { get; set; } = string.Empty;  // FK → AspNetUsers.Id
    public string Name { get; set; } = string.Empty;
    public PetSpecies Species { get; set; }              // enum
    public DateOnly BirthDate { get; set; }              // sadece tarih → SQL "date"
    public decimal Weight { get; set; }
    public bool IsActive { get; set; } = true;           // arşivleme bayrağı

    public ApplicationUser Owner { get; set; } = null!;  // navigation: hayvanın sahibi
    public ICollection<Vaccination> Vaccinations { get; set; } = new List<Vaccination>();
}
```

| Kavram | Açıklama |
|---|---|
| **Foreign key özelliği** (`OwnerId`) | Veritabanındaki FK kolonu. Sorgularda ve sahiplik kontrolünde doğrudan kullanılır. |
| **Navigation özelliği** (`Owner`, `Vaccinations`) | İlişkili nesneye C#'tan ulaşmayı sağlar: `pet.Owner.FullName`, `pet.Vaccinations.Count`. Veritabanında kolon değildir. |
| `= null!` | Zorunlu navigation'lar EF tarafından doldurulur. `null!` ile derleyiciye "bu null olmayacak" deriz (nullable uyarısını kapatır). |
| `= new List<T>()` | Koleksiyon hiçbir zaman null olmaz; `pet.Vaccinations.Add(...)` güvenle çağrılabilir. |
| `string?` | Opsiyonel metin → veritabanında `NULL` olabilir (`Breed`, `PhotoUrl`). |
| `DateOnly` / `TimeOnly` | .NET'in sadece tarih / sadece saat tipleri. SQL Server'da `date` ve `time` kolonlarına dönüşür. Doğum tarihinde saate, müsaitlikte tarihe gerek yoktur. |
| Hesaplanan özellik (`FullName`, `EndDate`) | Sadece okunur, veritabanına yazılmaz. `EndDate` LINQ sorgularının `Where` kısmında **kullanılamaz** (SQL'e çevrilemez); sorgularda `AppointmentDate.AddMinutes(Duration)` yazılır. |

### 3.1 Enum'lar ve Türkçe adlar

```csharp
public enum AppointmentStatus
{
    [Display(Name = "Bekliyor")] Pending = 0,
    [Display(Name = "Onaylandı")] Approved = 1,
    ...
}
```

Kodda İngilizce (`AppointmentStatus.Pending`), arayüzde Türkçe ("Bekliyor") kullanırız. `[Display]` niteliğini Razor'daki `Html.GetEnumSelectList<T>()` gibi yardımcılar otomatik okur.

### 3.2 `FieldLengths` — uzunluk sınırları tek yerde

```csharp
public static class FieldLengths
{
    public const int PetName = 50;
    ...
}
```

Fluent API'de `HasMaxLength(FieldLengths.PetName)`, ileride form modellerinde `[StringLength(FieldLengths.PetName)]` yazacağız. Veritabanı kolonu ile form doğrulaması **her zaman aynı sınırı** kullanır (kod tekrarı yok).

## 4. İlişkiler özeti

| İlişki | Tür | FK | Silme davranışı |
|---|---|---|---|
| ApplicationUser → Veterinarian | 1 – 0..1 | `Veterinarian.UserId` (benzersiz) | Restrict |
| ApplicationUser → Pet | 1 – * | `Pet.OwnerId` | Restrict |
| ApplicationUser → Appointment | 1 – * | `Appointment.OwnerId` | Restrict |
| Clinic → Veterinarian | 1 – * | `Veterinarian.ClinicId` | Restrict |
| Clinic → Service | 1 – * | `Service.ClinicId` | Restrict |
| Veterinarian ↔ Service | * – * | `VeterinarianService` (bileşik PK) | Cascade |
| Veterinarian → VeterinarianAvailability | 1 – * | `VeterinarianAvailability.VeterinarianId` | Cascade |
| Pet → Vaccination | 1 – * | `Vaccination.PetId` | Restrict |
| Veterinarian → Vaccination | 0..1 – * | `Vaccination.VeterinarianId` (null olabilir) | SetNull |
| Pet / Veterinarian / Service → Appointment | 1 – * | `PetId`, `VeterinarianId`, `ServiceId` | Restrict |
| Appointment → ExaminationNote | 1 – 0..1 | `ExaminationNote.AppointmentId` (benzersiz) | Restrict |
| Veterinarian → ExaminationNote | 1 – * | `ExaminationNote.VeterinarianId` | Restrict |

## 5. `AppDbContext`

```csharp
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Pet> Pets => Set<Pet>();
    // ... diğer 8 DbSet

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);                                   // 1) Identity tabloları
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly); // 2) Bizim ayarlarımız
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HavePrecision(0);   // datetime2(0)
        configurationBuilder.Properties<TimeOnly>().HavePrecision(0);   // time(0)
    }
}
```

- **`IdentityDbContext<ApplicationUser>`**: Identity'nin tablolarını (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`…) bizim için oluşturur. `AspNetUsers` tablosu `ApplicationUser` sınıfımızdaki ek alanları (`FirstName`, `LastName`, `CreatedAt`) da içerir.
- **`DbSet<T>`**: Bir tablo. LINQ sorguları bunun üzerinden yazılır: `context.Pets.Where(p => p.OwnerId == userId)`.
- **`base.OnModelCreating` önce çağrılmalı**: Çağrılmazsa Identity tablolarının anahtarları tanımlanmaz ve migration hata verir.
- **`ApplyConfigurationsFromAssembly`**: `Configurations` klasöründeki 10 sınıfı tek satırla uygular. Yeni bir entity eklediğimizde sadece yeni bir configuration dosyası yazarız.
- **`ConfigureConventions`**: Bütün `DateTime` kolonları için ortak kural. Tek tek her özelliğe yazmak zorunda kalmayız.

## 6. Fluent API — neden entity'lerin içinde `[Required]` yok?

Veritabanı ayarlarını iki şekilde yapabiliriz:

| Yöntem | Örnek | Bu projede |
|---|---|---|
| Data Annotations | `[Required, MaxLength(50)] public string Name {get;set;}` | **Form modellerinde** (ViewModel/DTO) kullanılacak — kullanıcıya doğrulama mesajı göstermek için |
| Fluent API | `builder.Property(p => p.Name).IsRequired().HasMaxLength(50);` | **Veritabanı ayarlarında** kullanılıyor |

Fluent API'yi seçmemizin nedenleri:

1. Annotation'lar ile yapılamayan ayarlar var: bileşik anahtar, silme davranışı, CHECK kısıtı, filtreli index, enum → metin dönüşümü.
2. Entity sınıfları sade kalır; "veri nasıl saklanır" bilgisi Data katmanında durur (katman sorumluluğu).
3. Her entity'nin ayarı kendi dosyasında; `AppDbContext` kalabalıklaşmaz.

Örnek — `AppointmentConfiguration`'dan bir kesit:

```csharp
builder.Property(a => a.Status)
    .HasConversion<string>()              // enum → "Pending" metni
    .HasMaxLength(FieldLengths.EnumText); // nvarchar(20)

builder.HasIndex(a => new { a.VeterinarianId, a.AppointmentDate }); // çakışma kontrolü için

builder.HasOne(a => a.Pet)                // Her randevunun BİR hayvanı vardır
    .WithMany(p => p.Appointments)        // Bir hayvanın ÇOK randevusu olabilir
    .HasForeignKey(a => a.PetId)          // İlişkiyi kuran kolon
    .OnDelete(DeleteBehavior.Restrict);   // Randevusu olan hayvan silinemez
```

## 7. Silme davranışları

| Davranış | Ne olur? | Nerede kullandık? |
|---|---|---|
| **Restrict** | Bağlı kayıt varsa ana kayıt **silinemez** (veritabanı hata verir) | Sağlık ve randevu geçmişini koruyan tüm ilişkiler |
| **Cascade** | Ana kayıt silinince bağlı kayıtlar da silinir | `VeterinarianService`, `VeterinarianAvailability` (hekimin "ayarları") |
| **SetNull** | Ana kayıt silinince FK kolonu `NULL` olur | `Vaccination.VeterinarianId` (hekim silinse de aşı kaydı kalır) |

**Neden çoğu ilişki Restrict?**

1. **Veri kaybını önlemek:** EF Core'un varsayılanı Cascade'dir. Bir kullanıcı silinince, hayvanları → aşıları → randevuları → muayene notları zincirleme silinirdi.
2. **SQL Server kuralı:** SQL Server aynı tabloya birden fazla cascade yolu olmasına izin vermez. Örneğin `Kullanıcı → Pet → Appointment` ve `Kullanıcı → Appointment` iki ayrı yoldur; ikisi de Cascade olsaydı migration *"may cause cycles or multiple cascade paths"* hatasıyla uygulanamazdı.

Kullanıcı arayüzünde "Sil" düğmesine basıldığında ne olacağını (bağlı kayıt varsa **arşivle**, yoksa **sil**) servis katmanında (Aşama 6) yazacağız. Veritabanındaki Restrict, kodda bir hata olsa bile verinin silinmesini engelleyen **ikinci savunma hattıdır**.

## 8. CHECK kısıtları ve index'ler

**CHECK kısıtları** — kural ihlali olan veri, uygulamadan bağımsız olarak veritabanına hiç yazılamaz:

| Tablo | Kısıt | Kural |
|---|---|---|
| Clinics | `CK_Clinics_WorkingHours` | `OpeningTime < ClosingTime` |
| Services | `CK_Services_Duration` | `Duration BETWEEN 5 AND 480` |
| Services | `CK_Services_Price` | `Price >= 0` |
| VeterinarianAvailabilities | `CK_VeterinarianAvailabilities_TimeRange` | `StartTime < EndTime` |
| Pets | `CK_Pets_Weight` | `Weight > 0` |
| Vaccinations | `CK_Vaccinations_NextDoseDate` | `NextDoseDate IS NULL OR NextDoseDate > VaccinationDate` |
| Appointments | `CK_Appointments_Duration`, `CK_Appointments_Price` | `Duration > 0`, `Price >= 0` |

**Index'ler** — sık yapılan sorguları hızlandırır:

| Tablo | Index | Hangi sorgu için? |
|---|---|---|
| Appointments | (VeterinarianId, AppointmentDate) | Çakışma kontrolü, uygun saat hesaplama |
| Appointments | (OwnerId, AppointmentDate) | Üyenin "Randevularım" listesi |
| Appointments | (PetId, AppointmentDate) | Hayvanın randevu geçmişi, aynı hayvanın çakışan randevusu |
| Appointments | (Status) | "Bekleyen randevular" filtresi |
| Pets | (OwnerId, Name) | "Evcil hayvanlarım", MCP `asi_takvimi(hayvanAdi)` |
| Vaccinations | (PetId, VaccinationDate), (NextDoseDate) | Aşı geçmişi, yaklaşan aşılar |
| VeterinarianAvailabilities | (VeterinarianId, DayOfWeek) | Hekimin o günkü çalışma saatleri |
| Veterinarians | (UserId) **UNIQUE** | 1 kullanıcı = 1 hekim |
| ExaminationNotes | (AppointmentId) **UNIQUE** | 1 randevu = 1 muayene notu |
| Services | (ClinicId, Name) **UNIQUE, filtreli** | Aynı klinikte aynı isimde iki **aktif** hizmet olamaz |

> **Filtreli index** (`HasFilter("[IsActive] = 1")`): Benzersizlik kuralı sadece aktif hizmetlere uygulanır. Böylece "Tıraş" hizmeti arşivlendikten sonra aynı isimle yeni bir "Tıraş" hizmeti oluşturulabilir.

## 9. Bağlantı dizesi ve veritabanının oluşturulması

### 9.1 Bağlantı dizesi

`appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=VeterinerKlinigiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

| Parça | Anlamı |
|---|---|
| `Server=(localdb)\MSSQLLocalDB` | Visual Studio ile gelen hafif SQL Server (LocalDB) |
| `Database=VeterinerKlinigiDb` | Oluşturulacak veritabanının adı |
| `Trusted_Connection=True` | Windows oturumunla bağlan, **parola yok** |
| `MultipleActiveResultSets=true` | Aynı bağlantıda birden fazla sorgu sonucunu aynı anda okumaya izin verir |
| `TrustServerCertificate=True` | Yerel geliştirmede SQL Server sertifikasını doğrulama |

Bu dize parola içermediği için depoda durabilir. **Kendi bilgisayarında LocalDB yerine farklı bir SQL Server kullanıyorsan** (ör. SQL Server 2025 Express), `appsettings.json`'ı değiştirme; aşağıdaki komutla sadece senin bilgisayarında geçerli olan bir değer tanımla (user-secrets, `appsettings.json`'ı ezer ve GitHub'a gitmez):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.\SQLEXPRESS;Database=VeterinerKlinigiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project src/VeterinerKlinigi.Web
```

LocalDB'nin kurulu olup olmadığını kontrol etmek için: `sqllocaldb info` → listede `MSSQLLocalDB` görünmeli.

### 9.2 Migration nedir?

Migration, entity sınıflarındaki değişikliklerin veritabanına nasıl uygulanacağını anlatan, EF Core'un **otomatik ürettiği** bir C# dosyasıdır. Her migration'ın iki metodu vardır:

- `Up()`: Değişikliği uygular (tabloları oluşturur).
- `Down()`: Değişikliği geri alır (tabloları siler).

Migration'lar `Migrations` klasöründe tarih sırasıyla birikir ve Git'e commit edilir; böylece veritabanının tüm geçmişi kodla birlikte saklanır. Veritabanında `__EFMigrationsHistory` tablosu hangi migration'ların uygulandığını tutar.

### 9.3 Komutlar

**Yöntem A — Visual Studio Package Manager Console (önerilen)**

1. **Araçlar → NuGet Paket Yöneticisi → Paket Yöneticisi Konsolu** (*Tools → NuGet Package Manager → Package Manager Console*).
2. Konsolun üstündeki **Default project** açılır listesinden `VeterinerKlinigi.Data` seç.
3. Solution Explorer'da başlangıç projesinin `VeterinerKlinigi.Web` olduğundan emin ol.
4. Komutlar:

```powershell
Add-Migration InitialCreate -Project VeterinerKlinigi.Data -StartupProject VeterinerKlinigi.Web -OutputDir Migrations
Update-Database -Project VeterinerKlinigi.Data -StartupProject VeterinerKlinigi.Web
```

**Yöntem B — Terminal (depo kökünde)**

```powershell
dotnet tool install --global dotnet-ef --version 10.0.15
dotnet ef migrations add InitialCreate --project src/VeterinerKlinigi.Data --startup-project src/VeterinerKlinigi.Web --output-dir Migrations
dotnet ef database update --project src/VeterinerKlinigi.Data --startup-project src/VeterinerKlinigi.Web
```

**Neden iki proje belirtiyoruz?**

- `-Project` / `--project` **VeterinerKlinigi.Data**: `AppDbContext` burada; migration dosyaları buraya yazılır.
- `-StartupProject` / `--startup-project` **VeterinerKlinigi.Web**: EF, bağlantı dizesini ve servis kayıtlarını bulmak için `Program.cs`'i çalıştırır. `Microsoft.EntityFrameworkCore.Design` paketi de bu projede.

`Add-Migration` şu 3 dosyayı oluşturur:

```text
src/VeterinerKlinigi.Data/Migrations/
├── 2026xxxxxxxxxx_InitialCreate.cs           ← Up() ve Down() metotları
├── 2026xxxxxxxxxx_InitialCreate.Designer.cs  ← O anki modelin özeti (EF kullanır)
└── AppDbContextModelSnapshot.cs              ← Modelin son hâli; sonraki migration'lar bununla karşılaştırılır
```

## 10. Test adımları

| # | Ne yapılacak? | Beklenen sonuç |
|---|---|---|
| 1 | **Derle** (Ctrl+Shift+B) | 5 proje başarılı, Hata Listesi'nde hata yok |
| 2 | `Add-Migration InitialCreate ...` | "Build succeeded" ve `Migrations` klasöründe 3 dosya |
| 3 | `..._InitialCreate.cs` dosyasını aç, `Ctrl+F` ile ara: `ReferentialAction.Restrict` | `Appointments`, `Pets`, `Vaccinations` tablolarının FK'larında görünmeli |
| 4 | Aynı dosyada ara: `CK_Appointments_Duration`, `filter: "[IsActive] = 1"` | CHECK kısıtı ve filtreli index tanımlı olmalı |
| 5 | `Update-Database ...` | Son satırda `Done.` |
| 6 | **Görünüm → SQL Server Nesne Gezgini** (*View → SQL Server Object Explorer*) → `(localdb)\MSSQLLocalDB` → Veritabanları → `VeterinerKlinigiDb` → Tablolar | `AspNet…` ile başlayan Identity tabloları, 9 proje tablosu (`Appointments`, `Clinics`, `ExaminationNotes`, `Pets`, `Services`, `Vaccinations`, `VeterinarianAvailabilities`, `Veterinarians`, `VeterinarianServices`) ve `__EFMigrationsHistory` |
| 7 | `dbo.Appointments` → sağ tık → **Tasarımcıyı Görüntüle** (*View Designer*) | `Status nvarchar(20)`, `Price decimal(10,2)`, `AppointmentDate datetime2(0)` |
| 8 | Veritabanına sağ tık → **Yeni Sorgu** (*New Query*), aşağıdaki SQL'i yapıştır ve çalıştır (Ctrl+Shift+E) | CHECK kısıtı hatası: *"The INSERT statement conflicted with the CHECK constraint "CK_Clinics_WorkingHours""* |
| 9 | Uygulamayı **F5** ile çalıştır | Ana sayfa ve `/api/system/status` hâlâ çalışıyor |

Test 8 için SQL (kapanış saati açılıştan önce → veritabanı reddetmeli):

```sql
INSERT INTO Clinics (Name, Address, Phone, OpeningTime, ClosingTime, IsActive)
VALUES (N'Test Klinik', N'Adres', N'0', '18:00', '09:00', 1);
```

Bu, kurallarımızın sadece C# kodunda değil **veritabanı seviyesinde** de korunduğunu gösterir; sunumda göstermek için iyi bir örnektir. (Kayıt eklenmediği için silmen gereken bir şey yok.)

> **İsteğe bağlı:** SQL Server Management Studio (SSMS) kuruluysa veritabanında **Database Diagrams → New Database Diagram** ile tüm tabloları seçip gerçek ER diyagramının ekran görüntüsünü alabilirsin. Gün 10'da README'ye eklenecek.

## 11. Sık karşılaşılan hatalar

| Hata | Neden | Çözüm |
|---|---|---|
| `The term 'Add-Migration' is not recognized` | EF Tools paketi yüklenmedi | Solution'ı derle, Visual Studio'yu yeniden başlat |
| `No DbContext was found in assembly 'VeterinerKlinigi.Web'` | `-Project` belirtilmedi / Default project yanlış | Komuttaki `-Project VeterinerKlinigi.Data` kısmını kontrol et |
| `Your startup project 'X' doesn't reference Microsoft.EntityFrameworkCore.Design` | Başlangıç projesi yanlış | `-StartupProject VeterinerKlinigi.Web` |
| `Build failed.` | Projede derleme hatası var | Önce Ctrl+Shift+B ile hataları gör ve düzelt (hata mesajını bana gönderebilirsin) |
| `A network-related or instance-specific error occurred...` | LocalDB çalışmıyor / kurulu değil | `sqllocaldb start MSSQLLocalDB`; kurulu değilse Bölüm 9.1'deki user-secrets yöntemiyle kendi SQL Server'ını kullan |
| `dotnet-ef : command not found` | Araç kurulu değil | `dotnet tool install --global dotnet-ef --version 10.0.15`, terminali yeniden aç |
| Migration'ı yanlış oluşturdum | — | Veritabanına uygulamadıysan: `Remove-Migration -Project VeterinerKlinigi.Data -StartupProject VeterinerKlinigi.Web` |

## 12. Aşama sonu kontrol listesi

| Gereksinim | Durum |
|---|---|
| 10 entity: ApplicationUser, Clinic, Veterinarian, Service, VeterinarianService, VeterinarianAvailability, Pet, Vaccination, Appointment, ExaminationNote | ✅ |
| İstenen tüm alanlar (Pet, Vaccination, Appointment, Service, Veterinarian, VeterinarianAvailability, ExaminationNote) | ✅ |
| Appointment Status: Pending, Approved, Rejected, Completed, Cancelled | ✅ |
| İlişkiler ve foreign key'ler | ✅ 12 ilişki, Fluent API |
| Geçmiş kayıtların korunması (silme davranışları) | ✅ Restrict / SetNull / Cascade |
| Veritabanı seviyesinde doğrulama | ✅ 8 CHECK kısıtı |
| Index'ler | ✅ 10 index (3 benzersiz, 1 filtreli) |
| EF Core Code First + Migration | ✅ InitialCreate (senin oluşturacağın) |
| SQL Injection'a karşı altyapı | ✅ Tüm erişim EF Core parametreli sorgularıyla yapılacak |
| Bağlantı dizesinde parola yok | ✅ Windows kimlik doğrulaması |

**Bir sonraki aşamada (Gün 4):** Identity ayarları, 3 rol, `b241210351@sakarya.edu.tr / sau` admin hesabı, örnek veriler (seed), Giriş / Kayıt / Profil sayfaları.

## 13. Sunumda sorulabilecek sorular

**S: Code First nedir?**
C: Önce C# sınıflarını (entity) yazıp veritabanını bu sınıflardan EF Core'a oluşturtmaktır. Tablo, kolon ve ilişki değişiklikleri migration dosyalarıyla kayıt altına alınır ve `Update-Database` ile veritabanına uygulanır.

**S: Migration'ın `Up` ve `Down` metotları ne işe yarar?**
C: `Up` değişikliği uygular, `Down` geri alır. Böylece veritabanını istediğimiz migration'a geri döndürebiliriz.

**S: Neden Data Annotation yerine Fluent API kullandınız?**
C: Silme davranışı, bileşik anahtar, CHECK kısıtı ve filtreli index gibi ayarlar annotation ile yapılamıyor. Ayrıca "verinin nasıl saklanacağı" bilgisi Data katmanında, entity'ler ise sade kalıyor. Data Annotation'ları kullanıcıya doğrulama mesajı göstermek için form modellerinde kullanacağız.

**S: Neden çoğu ilişkide Restrict kullandınız?**
C: Hasta geçmişi kaybolmasın diye. Ayrıca SQL Server birden fazla cascade yoluna izin vermiyor (Kullanıcı → Pet → Randevu ve Kullanıcı → Randevu).

**S: Randevuda `Duration` ve `Price` neden tekrar tutuluyor, hizmetten okunamaz mı?**
C: Hizmetin fiyatı ileride değişebilir. Randevu oluşturulduğu andaki fiyatı saklamazsak geçmiş randevuların ücreti ve ciro raporu yanlış çıkar.

**S: `OwnerId` hem Pet'te hem Appointment'ta var, bu tekrar değil mi?**
C: Bilinçli bir tekrar. "Bu randevu bu kullanıcının mı?" kontrolü her istekte yapılıyor; `OwnerId` randevuda olunca Pet tablosuna JOIN yapmadan, index'li bir kolonla kontrol edebiliyoruz. Değer, randevu oluşturulurken hayvanın sahibinden kopyalanıyor.

**S: Enum'ları neden metin olarak sakladınız?**
C: Veritabanına baktığımızda `2` yerine `Rejected` görüyoruz; okunması ve hata ayıklaması kolay. Enum'a ileride yeni bir değer eklense de eski kayıtların anlamı değişmez.

## 14. Commit

Migration'ı oluşturup `Update-Database` başarılı olduktan **sonra** commit et; `Migrations` klasörü de commit'e dahil olmalı.

- **Summary:** `feat(data): entity sınıfları, DbContext ilişkileri ve ilk migration eklendi`
- **Description:**
  ```text
  - 10 entity, AppointmentStatus ve PetSpecies enum'ları
  - Fluent API konfigürasyonları, Restrict/Cascade/SetNull silme davranışları
  - 8 CHECK kısıtı ve 10 index (filtreli benzersiz index dahil)
  - InitialCreate migration
  ```

→ **Commit to main** → **Push origin**
