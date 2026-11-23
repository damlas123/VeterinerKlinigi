using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Concrete;
using VeterinerKlinigi.Business.Options;

namespace VeterinerKlinigi.Business;

/// <summary>
/// İş katmanındaki servislerin Dependency Injection kayıtları.
/// Program.cs bu metodu çağırır; böylece Web projesi Business'ın iç yapısını bilmek zorunda kalmaz.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services)
    {
        // TimeProvider: .NET'in "şu an saat kaç?" sorusunu cevaplayan sınıfı.
        // DateTime.Now yerine bunu kullanırız; çünkü testlerde sahte bir saat verebiliriz.
        // Randevu kurallarındaki "geçmiş tarihe randevu verilmez" kontrolü de bu sınıfı kullanır.
        services.TryAddSingleton(TimeProvider.System);

        // Randevu ayarları (appsettings.json → "Appointment"). Web projesi değerleri bağlar;
        // bağlanmazsa sınıftaki varsayılanlar (15 dk, 60 gün) kullanılır.
        services.AddOptions<AppointmentOptions>();

        // İş servisleri: arayüz (Abstract) → uygulama (Concrete).
        // Scoped: her HTTP isteği (veya MCP çağrısı) için bir örnek; aynı DbContext'i paylaşırlar.
        services.AddScoped<IClinicService, ClinicManager>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogManager>();
        services.AddScoped<IVeterinarianService, VeterinarianManager>();
        services.AddScoped<IAvailabilityService, AvailabilityManager>();
        services.AddScoped<IPetService, PetManager>();
        services.AddScoped<IVaccinationService, VaccinationManager>();
        services.AddScoped<IAppointmentService, AppointmentManager>();
        services.AddScoped<IExaminationNoteService, ExaminationNoteManager>();
        services.AddScoped<IUserAdminService, UserAdminManager>();
        services.AddScoped<IReportService, ReportManager>();

        return services;
    }
}
