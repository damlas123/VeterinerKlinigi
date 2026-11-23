using System.ComponentModel;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Web.Mcp;

/// <summary>
/// MCP (Model Context Protocol) ARAÇLARI.
/// Bir yapay zekâ asistanı (Claude Desktop, VS Code, MCP Inspector ...) bu metotları "araç" olarak çağırır.
///
/// ÖNEMLİ:
/// - Araçlar iş kuralı İÇERMEZ; MVC sayfaları ve REST API ile AYNI Business servislerini çağırır.
/// - /mcp ucu JWT ile korunur (Program.cs → RequireAuthorization). Servisler giriş yapan kullanıcıyı
///   ICurrentUserService ile bilir; bu yüzden araçlar sadece o kullanıcının verisini döndürür.
/// - Hata durumunda McpException fırlatılır; mesaj asistana "isError: true" olarak iletilir.
///
/// Metotlar static olduğu için sınıfın örneği oluşturulmaz.
/// Servisler metot parametresi olarak Dependency Injection'dan gelir (MCP SDK bunları otomatik çözer);
/// diğer parametreler (hizmetId, tarih ...) asistanın gönderdiği JSON'dan doldurulur.
/// </summary>
[McpServerToolType]
public sealed class VeterinerKlinigiTools
{
    // Türkçe karakterler ç gibi kaçışlarla değil, okunabilir şekilde yazılsın.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    // ------------------------------------------------------------------
    // 1) uygun_saatler
    // ------------------------------------------------------------------
    [McpServerTool(Name = "uygun_saatler", ReadOnly = true, Title = "Uygun randevu saatleri")]
    [Description("Seçilen hizmet ve tarih için o gün boş olan veteriner hekimleri ve boş başlangıç saatlerini listeler. " +
                 "Hizmet id'lerini öğrenmek için önce 'hizmet_listesi' aracını kullanın.")]
    public static async Task<string> UygunSaatler(
        IAppointmentService appointmentService,
        [Description("Hizmetin id'si (hizmet_listesi aracından).")] int hizmetId,
        [Description("Tarih, yyyy-MM-dd biçiminde. Örnek: 2026-11-25")] string tarih,
        [Description("İsteğe bağlı: sadece bu veterinerin saatleri.")] int? veterinerId = null,
        CancellationToken cancellationToken = default)
    {
        if (!DateOnly.TryParseExact(tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new McpException("Tarih yyyy-MM-dd biçiminde olmalıdır (ör. 2026-11-25).");
        }

        var result = await appointmentService.GetAvailableSlotsAsync(hizmetId, date, veterinerId, null, cancellationToken);
        var slots = Unwrap(result);

        return Serialize(new
        {
            tarih = date.ToString("yyyy-MM-dd"),
            gun = DisplayText.Day(date.DayOfWeek),
            hizmetId,
            uygunHekimSayisi = slots.Count(s => s.Slots.Count > 0),
            hekimler = slots.Select(s => new
            {
                veterinerId = s.VeterinarianId,
                veteriner = s.VeterinarianName,
                uzmanlik = s.Specialty,
                saatler = s.Slots
            }),
            not = slots.All(s => s.Slots.Count == 0) ? "Bu tarihte boş saat yok. Başka bir gün deneyin." : null
        });
    }

    // ------------------------------------------------------------------
    // 2) asi_takvimi
    // ------------------------------------------------------------------
    [McpServerTool(Name = "asi_takvimi", ReadOnly = true, Title = "Aşı takvimi")]
    [Description("Kullanıcının evcil hayvanının geçmiş aşılarını, yaklaşan ve geciken aşılarını döndürür. " +
                 "Örnek soru: 'Pamuk'un bir sonraki aşısı ne zaman?'")]
    public static async Task<string> AsiTakvimi(
        IVaccinationService vaccinationService,
        [Description("Evcil hayvanın adı. Örnek: Pamuk")] string hayvanAdi,
        CancellationToken cancellationToken = default)
    {
        var result = await vaccinationService.GetScheduleByPetNameAsync(hayvanAdi, cancellationToken);
        var schedules = Unwrap(result);

        return Serialize(schedules.Select(s => new
        {
            petId = s.PetId,
            hayvan = s.PetName,
            tur = s.SpeciesName,
            sonrakiAsi = s.Upcoming.FirstOrDefault() is { } next
                ? $"{next.VaccineName}: {next.NextDoseDate:dd.MM.yyyy} ({next.DueStateText})"
                : "Planlanmış bir sonraki aşı yok.",
            gecikenAsilar = s.Overdue.Select(Format),
            yaklasanAsilar = s.Upcoming.Select(Format),
            asiGecmisi = s.History.Select(v => new
            {
                asi = v.VaccineName,
                yapildigiTarih = v.VaccinationDate.ToString("dd.MM.yyyy"),
                sonrakiDoz = v.NextDoseDate?.ToString("dd.MM.yyyy"),
                hekim = v.VeterinarianName ?? "Başka klinik",
                aciklama = v.Description
            })
        }));

        static object Format(VaccinationDto v) => new
        {
            asi = v.VaccineName,
            sonrakiDoz = v.NextDoseDate?.ToString("dd.MM.yyyy"),
            durum = v.DueStateText
        };
    }

    // ------------------------------------------------------------------
    // 3) randevu_olustur  (VERİ YAZAN ARAÇ)
    // ------------------------------------------------------------------
    [McpServerTool(Name = "randevu_olustur", Destructive = false, Idempotent = false, Title = "Randevu oluştur")]
    [Description("Giriş yapan üyenin evcil hayvanı için 'Bekliyor' durumunda randevu oluşturur. " +
                 "Saat dolu, hekim müsait değil veya kural ihlali varsa nedenini açıklayan bir hata döner. " +
                 "Önce 'uygun_saatler' ile boş saatleri kontrol etmeniz önerilir.")]
    public static async Task<string> RandevuOlustur(
        IAppointmentService appointmentService,
        [Description("Evcil hayvanın id'si (hayvanlarim aracından).")] int petId,
        [Description("Hizmetin id'si (hizmet_listesi aracından).")] int serviceId,
        [Description("Veterinerin id'si (uygun_saatler aracından).")] int veterinarianId,
        [Description("Randevu başlangıcı, yyyy-MM-ddTHH:mm biçiminde. Örnek: 2026-11-25T10:30")] string appointmentDate,
        [Description("İsteğe bağlı not / şikâyet.")] string? not = null,
        CancellationToken cancellationToken = default)
    {
        string[] formats = ["yyyy-MM-ddTHH:mm", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss"];
        if (!DateTime.TryParseExact(appointmentDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
        {
            throw new McpException("appointmentDate yyyy-MM-ddTHH:mm biçiminde olmalıdır (ör. 2026-11-25T10:30).");
        }

        var request = new AppointmentSaveDto
        {
            PetId = petId,
            ServiceId = serviceId,
            VeterinarianId = veterinarianId,
            AppointmentDate = start,
            OwnerNote = not
        };

        // MVC formu ve POST /api/appointments ile AYNI metot: tüm çakışma kuralları burada da geçerli.
        var result = await appointmentService.CreateAsync(request, cancellationToken);
        var appointment = Unwrap(result);

        return Serialize(new
        {
            mesaj = result.Message,
            randevuId = appointment.Id,
            durum = appointment.StatusName,
            hayvan = appointment.PetName,
            hizmet = appointment.ServiceName,
            veteriner = appointment.VeterinarianName,
            klinik = appointment.ClinicName,
            baslangic = appointment.AppointmentDate.ToString("dd.MM.yyyy HH:mm"),
            bitis = appointment.EndDate.ToString("HH:mm"),
            sureDakika = appointment.Duration,
            ucretTL = appointment.Price
        });
    }

    // ------------------------------------------------------------------
    // Yardımcı araçlar: asistanın id'leri bulabilmesi için
    // ------------------------------------------------------------------
    [McpServerTool(Name = "hayvanlarim", ReadOnly = true, Title = "Evcil hayvanlarım")]
    [Description("Giriş yapan kullanıcının evcil hayvanlarını id'leriyle listeler (randevu_olustur için petId burada bulunur).")]
    public static async Task<string> Hayvanlarim(IPetService petService, CancellationToken cancellationToken = default)
    {
        var pets = await petService.GetListAsync(null, cancellationToken);
        return Serialize(pets.Select(p => new
        {
            petId = p.Id,
            ad = p.Name,
            tur = p.SpeciesName,
            irk = p.Breed,
            dogumTarihi = p.BirthDate.ToString("dd.MM.yyyy"),
            kiloKg = p.Weight
        }));
    }

    [McpServerTool(Name = "hizmet_listesi", ReadOnly = true, Title = "Hizmet listesi")]
    [Description("Kliniklerin sunduğu aktif hizmetleri id, süre ve ücretleriyle listeler.")]
    public static async Task<string> HizmetListesi(
        IServiceCatalogService serviceCatalog,
        [Description("İsteğe bağlı: sadece bu kliniğin hizmetleri.")] int? klinikId = null,
        CancellationToken cancellationToken = default)
    {
        var services = await serviceCatalog.GetAllAsync(klinikId, false, cancellationToken);
        return Serialize(services.Select(s => new
        {
            hizmetId = s.Id,
            hizmet = s.Name,
            klinik = s.ClinicName,
            sureDakika = s.Duration,
            ucretTL = s.Price,
            aciklama = s.Description
        }));
    }

    [McpServerTool(Name = "randevularim", ReadOnly = true, Title = "Randevularım")]
    [Description("Giriş yapan kullanıcının randevularını listeler. Durum: Pending, Approved, Rejected, Completed, Cancelled (boş = tümü).")]
    public static async Task<string> Randevularim(
        IAppointmentService appointmentService,
        [Description("İsteğe bağlı durum filtresi: Pending, Approved, Rejected, Completed veya Cancelled.")] string? durum = null,
        CancellationToken cancellationToken = default)
    {
        AppointmentStatus? status = null;
        if (!string.IsNullOrWhiteSpace(durum))
        {
            if (!Enum.TryParse<AppointmentStatus>(durum, ignoreCase: true, out var parsed))
            {
                throw new McpException("Durum şunlardan biri olmalıdır: Pending, Approved, Rejected, Completed, Cancelled.");
            }

            status = parsed;
        }

        var appointments = await appointmentService.GetListAsync(new AppointmentFilter { Status = status }, cancellationToken);
        return Serialize(appointments.Select(a => new
        {
            randevuId = a.Id,
            tarih = a.AppointmentDate.ToString("dd.MM.yyyy HH:mm"),
            hayvan = a.PetName,
            hizmet = a.ServiceName,
            veteriner = a.VeterinarianName,
            durum = a.StatusName,
            retGerekcesi = a.RejectionReason
        }));
    }

    // ------------------------------------------------------------------
    // Yardımcı metotlar
    // ------------------------------------------------------------------

    /// <summary>Başarılıysa veriyi döndürür; değilse hata mesajını McpException olarak fırlatır.</summary>
    private static T Unwrap<T>(ServiceResult<T> result)
    {
        if (!result.IsSuccess)
        {
            var prefix = result.ErrorType switch
            {
                ErrorType.Conflict => "Çakışma: ",
                ErrorType.NotFound => "Bulunamadı: ",
                ErrorType.Forbidden => "Yetki hatası: ",
                _ => "Geçersiz istek: "
            };
            throw new McpException(prefix + result.Message);
        }

        return result.Data!;
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);
}
