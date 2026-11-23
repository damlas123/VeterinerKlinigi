using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Business.Security;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Business.Concrete;

/// <summary>
/// Raporlar ve panel istatistikleri. Tüm hesaplamalar LINQ ile veritabanında yapılır
/// (GroupBy, Count, Sum); sadece sonuçlar belleğe alınır.
/// </summary>
public class ReportManager : ManagerBase, IReportService
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private readonly IVaccinationService _vaccinationService;

    public ReportManager(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        IVaccinationService vaccinationService)
        : base(unitOfWork, currentUser, timeProvider)
    {
        _vaccinationService = vaccinationService;
    }

    private IRepository<Appointment> Appointments => Repo<Appointment>();

    // =====================================================================
    // HİZMET TÜRÜNE GÖRE RANDEVU SAYISI VE CİRO RAPORU
    // =====================================================================
    public async Task<ServiceResult<RevenueReportDto>> GetRevenueByServiceAsync(DateOnly? from, DateOnly? to, int? clinicId, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<RevenueReportDto>.Forbidden();
        }

        // Varsayılan aralık: son 6 ay (bu ay dahil).
        var toDate = to ?? Today;
        var fromDate = from ?? new DateOnly(Today.Year, Today.Month, 1).AddMonths(-5);
        if (fromDate > toDate)
        {
            return ServiceResult<RevenueReportDto>.Invalid("Başlangıç tarihi bitiş tarihinden sonra olamaz.", "from");
        }

        var start = fromDate.ToDateTime(TimeOnly.MinValue);
        var end = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        // İptal edilen ve reddedilen randevular rapora dahil edilmez.
        var query = Appointments.QueryNoTracking()
            .Where(a => a.AppointmentDate >= start && a.AppointmentDate < end
                && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Rejected);

        if (clinicId is int id)
        {
            query = query.Where(a => a.Service.ClinicId == id);
        }

        // LINQ GroupBy → SQL'de GROUP BY. Ciro sadece TAMAMLANAN randevulardan hesaplanır.
        var items = await query
            .GroupBy(a => new { a.ServiceId, ServiceName = a.Service.Name, ClinicName = a.Service.Clinic.Name })
            .Select(g => new ServiceRevenueDto(
                g.Key.ServiceId,
                g.Key.ServiceName,
                g.Key.ClinicName,
                g.Count(),
                g.Count(a => a.Status == AppointmentStatus.Completed),
                g.Sum(a => a.Status == AppointmentStatus.Completed ? a.Price : 0m)))
            .ToListAsync(cancellationToken);

        var ordered = items
            .OrderByDescending(i => i.TotalRevenue)
            .ThenByDescending(i => i.AppointmentCount)
            .ToList();

        return ServiceResult<RevenueReportDto>.Success(new RevenueReportDto(
            fromDate,
            toDate,
            clinicId,
            ordered,
            ordered.Sum(i => i.AppointmentCount),
            ordered.Sum(i => i.CompletedCount),
            ordered.Sum(i => i.TotalRevenue)));
    }

    // =====================================================================
    // ADMIN PANELİ
    // =====================================================================
    public async Task<ServiceResult<AdminDashboardDto>> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<AdminDashboardDto>.Forbidden();
        }

        var today = Today;
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var tomorrowStart = todayStart.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var now = Now;

        var clinicCount = await Repo<Clinic>().QueryNoTracking().CountAsync(c => c.IsActive, cancellationToken);
        var veterinarianCount = await Repo<Veterinarian>().QueryNoTracking().CountAsync(v => v.IsActive, cancellationToken);
        var petCount = await Repo<Pet>().QueryNoTracking().CountAsync(p => p.IsActive, cancellationToken);
        var serviceCount = await Repo<Service>().QueryNoTracking().CountAsync(s => s.IsActive, cancellationToken);

        var memberRoleId = Repo<IdentityRole>().QueryNoTracking().Where(r => r.Name == RoleNames.Member).Select(r => r.Id);
        var memberCount = await Repo<IdentityUserRole<string>>().QueryNoTracking()
            .CountAsync(ur => memberRoleId.Contains(ur.RoleId), cancellationToken);

        var active = Appointments.QueryNoTracking()
            .Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Rejected);

        var todayCount = await active.CountAsync(a => a.AppointmentDate >= todayStart && a.AppointmentDate < tomorrowStart, cancellationToken);
        var pendingCount = await Appointments.QueryNoTracking()
            .CountAsync(a => a.Status == AppointmentStatus.Pending && a.AppointmentDate > now, cancellationToken);
        var monthRevenue = await Appointments.QueryNoTracking()
            .Where(a => a.Status == AppointmentStatus.Completed && a.AppointmentDate >= monthStart && a.AppointmentDate < tomorrowStart)
            .SumAsync(a => (decimal?)a.Price, cancellationToken) ?? 0m;

        // Durumlara göre dağılım (GroupBy).
        var statusRows = await Appointments.QueryNoTracking()
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var statusDistribution = Enum.GetValues<AppointmentStatus>()
            .Select(s => new CountItemDto(DisplayText.Status(s), statusRows.FirstOrDefault(r => r.Status == s)?.Count ?? 0))
            .ToList();

        // Son 6 ayın randevu sayısı ve cirosu.
        var trendStart = monthStart.AddMonths(-5);
        var monthlyRows = await active
            .Where(a => a.AppointmentDate >= trendStart && a.AppointmentDate < tomorrowStart)
            .GroupBy(a => new { a.AppointmentDate.Year, a.AppointmentDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
                Revenue = g.Sum(a => a.Status == AppointmentStatus.Completed ? a.Price : 0m)
            })
            .ToListAsync(cancellationToken);

        var monthlyTrend = Enumerable.Range(0, 6)
            .Select(i => trendStart.AddMonths(i))
            .Select(m =>
            {
                var row = monthlyRows.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);
                return new MonthlyStatDto(MonthLabel(m), row?.Count ?? 0, row?.Revenue ?? 0m);
            })
            .ToList();

        // Türlere göre aktif hayvan sayısı.
        var speciesRows = await Repo<Pet>().QueryNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => p.Species)
            .Select(g => new { Species = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var speciesDistribution = speciesRows
            .OrderByDescending(r => r.Count)
            .Select(r => new CountItemDto(DisplayText.Species(r.Species), r.Count))
            .ToList();

        var pendingAppointments = await Appointments.QueryNoTracking()
            .Where(a => a.Status == AppointmentStatus.Pending && a.AppointmentDate > now)
            .OrderBy(a => a.AppointmentDate)
            .Take(5)
            .Select(Projections.Appointment)
            .ToListAsync(cancellationToken);

        return ServiceResult<AdminDashboardDto>.Success(new AdminDashboardDto(
            clinicCount, veterinarianCount, memberCount, petCount, serviceCount,
            todayCount, pendingCount, monthRevenue,
            statusDistribution, monthlyTrend, speciesDistribution, pendingAppointments));
    }

    // =====================================================================
    // VETERİNER PANELİ
    // =====================================================================
    public async Task<ServiceResult<VeterinarianDashboardDto>> GetVeterinarianDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsVeterinarian || CurrentUser.VeterinarianId is not int veterinarianId)
        {
            return ServiceResult<VeterinarianDashboardDto>.Forbidden();
        }

        var name = await Repo<Veterinarian>().QueryNoTracking()
            .Where(v => v.Id == veterinarianId)
            .Select(v => v.FullName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var today = Today;
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var tomorrowStart = todayStart.AddDays(1);
        var weekEnd = todayStart.AddDays(7);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var now = Now;

        var mine = Appointments.QueryNoTracking().Where(a => a.VeterinarianId == veterinarianId);
        var active = mine.Where(a => a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.Rejected);

        var todayAppointments = await active
            .Where(a => a.AppointmentDate >= todayStart && a.AppointmentDate < tomorrowStart)
            .OrderBy(a => a.AppointmentDate)
            .Select(Projections.Appointment)
            .ToListAsync(cancellationToken);

        var pending = await mine
            .Where(a => a.Status == AppointmentStatus.Pending && a.AppointmentDate > now)
            .OrderBy(a => a.AppointmentDate)
            .Select(Projections.Appointment)
            .ToListAsync(cancellationToken);

        var completedThisMonth = await mine.CountAsync(a => a.Status == AppointmentStatus.Completed && a.AppointmentDate >= monthStart, cancellationToken);
        var patientCount = await mine.Select(a => a.PetId).Distinct().CountAsync(cancellationToken);

        // Önümüzdeki 7 günün randevu sayıları (grafik).
        var weekRows = await active
            .Where(a => a.AppointmentDate >= todayStart && a.AppointmentDate < weekEnd)
            .GroupBy(a => a.AppointmentDate.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var nextSevenDays = Enumerable.Range(0, 7)
            .Select(i => todayStart.AddDays(i))
            .Select(d => new CountItemDto(d.ToString("ddd dd.MM", Turkish), weekRows.FirstOrDefault(r => r.Day == d)?.Count ?? 0))
            .ToList();

        // Hastalarının yaklaşan ve geciken aşıları (VaccinationService sahiplik filtresini zaten uygular).
        var dueVaccinations = await _vaccinationService.GetDueAsync(30, cancellationToken);

        return ServiceResult<VeterinarianDashboardDto>.Success(new VeterinarianDashboardDto(
            name,
            todayAppointments.Count,
            pending.Count,
            completedThisMonth,
            patientCount,
            todayAppointments,
            pending.Take(5).ToList(),
            dueVaccinations.Take(8).ToList(),
            nextSevenDays));
    }

    // =====================================================================
    // ÜYE PANELİ
    // =====================================================================
    public async Task<ServiceResult<MemberDashboardDto>> GetMemberDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsMember || CurrentUser.UserId is not string userId)
        {
            return ServiceResult<MemberDashboardDto>.Forbidden();
        }

        var now = Now;

        var pets = await Repo<Pet>().QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(Projections.Pet)
            .ToListAsync(cancellationToken);

        var upcoming = await Appointments.QueryNoTracking()
            .Where(a => a.OwnerId == userId && a.AppointmentDate > now
                && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
            .OrderBy(a => a.AppointmentDate)
            .Select(Projections.Appointment)
            .ToListAsync(cancellationToken);

        var dueVaccinations = await _vaccinationService.GetDueAsync(30, cancellationToken);

        return ServiceResult<MemberDashboardDto>.Success(new MemberDashboardDto(
            pets.Count,
            upcoming.Count,
            upcoming.Count(a => a.Status == AppointmentStatus.Pending),
            dueVaccinations.Count,
            upcoming.Take(5).ToList(),
            dueVaccinations,
            pets));
    }

    private static string MonthLabel(DateTime month)
        => $"{Turkish.DateTimeFormat.GetAbbreviatedMonthName(month.Month)} {month.Year}";
}
