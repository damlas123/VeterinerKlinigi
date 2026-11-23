namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Bir hizmetin randevu sayısı ve cirosu.</summary>
/// <param name="AppointmentCount">İptal/ret dışındaki tüm randevular.</param>
/// <param name="CompletedCount">Tamamlanan randevular.</param>
/// <param name="TotalRevenue">Tamamlanan randevuların toplam ücreti (ciro).</param>
public record ServiceRevenueDto(
    int ServiceId,
    string ServiceName,
    string ClinicName,
    int AppointmentCount,
    int CompletedCount,
    decimal TotalRevenue);

/// <summary>Hizmet türüne göre randevu sayısı ve ciro raporu.</summary>
public record RevenueReportDto(
    DateOnly From,
    DateOnly To,
    int? ClinicId,
    IReadOnlyList<ServiceRevenueDto> Items,
    int TotalAppointments,
    int TotalCompleted,
    decimal TotalRevenue);

/// <summary>Grafikler için etiket + sayı.</summary>
public record CountItemDto(string Label, int Count);

/// <summary>Aylık randevu sayısı ve ciro.</summary>
public record MonthlyStatDto(string Month, int AppointmentCount, decimal Revenue);

/// <summary>Admin paneli istatistikleri.</summary>
public record AdminDashboardDto(
    int ClinicCount,
    int VeterinarianCount,
    int MemberCount,
    int PetCount,
    int ServiceCount,
    int TodayAppointmentCount,
    int PendingAppointmentCount,
    decimal MonthRevenue,
    IReadOnlyList<CountItemDto> StatusDistribution,
    IReadOnlyList<MonthlyStatDto> MonthlyTrend,
    IReadOnlyList<CountItemDto> SpeciesDistribution,
    IReadOnlyList<AppointmentDto> PendingAppointments);

/// <summary>Veteriner paneli istatistikleri.</summary>
public record VeterinarianDashboardDto(
    string VeterinarianName,
    int TodayCount,
    int PendingCount,
    int CompletedThisMonth,
    int PatientCount,
    IReadOnlyList<AppointmentDto> TodayAppointments,
    IReadOnlyList<AppointmentDto> PendingAppointments,
    IReadOnlyList<VaccinationDto> UpcomingVaccinations,
    IReadOnlyList<CountItemDto> NextSevenDays);

/// <summary>Üye paneli özeti.</summary>
public record MemberDashboardDto(
    int PetCount,
    int UpcomingAppointmentCount,
    int PendingAppointmentCount,
    int DueVaccinationCount,
    IReadOnlyList<AppointmentDto> UpcomingAppointments,
    IReadOnlyList<VaccinationDto> DueVaccinations,
    IReadOnlyList<PetDto> Pets);
