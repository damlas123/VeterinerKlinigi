using System.Linq.Expressions;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Business.Concrete;

/// <summary>
/// Entity → DTO dönüşümleri (LINQ projeksiyonları).
/// Bunlar Expression olduğu için EF Core tarafından SQL'e çevrilir: veritabanından sadece
/// DTO'da gereken kolonlar okunur (SELECT * yapılmaz) ve entity'ler dışarı hiç çıkmaz.
/// Aynı dönüşüm tüm servislerde tekrar kullanılır (kod tekrarı yok).
/// </summary>
internal static class Projections
{
    public static readonly Expression<Func<Clinic, ClinicDto>> Clinic = c => new ClinicDto(
        c.Id, c.Name, c.Address, c.Phone, c.Email, c.Description, c.OpeningTime, c.ClosingTime, c.IsActive,
        c.Veterinarians.Count(v => v.IsActive),
        c.Services.Count(s => s.IsActive));

    public static readonly Expression<Func<Service, ServiceDto>> Service = s => new ServiceDto(
        s.Id, s.ClinicId, s.Clinic.Name, s.Name, s.Description, s.Duration, s.Price, s.IsActive,
        s.VeterinarianServices.Count(vs => vs.Veterinarian.IsActive));

    public static readonly Expression<Func<Veterinarian, VeterinarianDto>> Veterinarian = v => new VeterinarianDto(
        v.Id, v.FullName, v.Specialty, v.Biography, v.ClinicId, v.Clinic.Name,
        v.User.Email ?? string.Empty, v.User.PhoneNumber, v.IsActive,
        v.VeterinarianServices
            .Where(vs => vs.Service.IsActive)
            .OrderBy(vs => vs.Service.Name)
            .Select(vs => new ServiceBriefDto(vs.Service.Id, vs.Service.Name, vs.Service.Duration, vs.Service.Price))
            .ToList());

    public static readonly Expression<Func<VeterinarianAvailability, AvailabilityDto>> Availability = a => new AvailabilityDto(
        a.Id, a.VeterinarianId, a.DayOfWeek, a.StartTime, a.EndTime);

    public static readonly Expression<Func<Pet, PetDto>> Pet = p => new PetDto(
        p.Id, p.Name, p.Species, p.Breed, p.BirthDate, p.Weight, p.PhotoUrl, p.IsActive,
        p.OwnerId, p.Owner.FirstName + " " + p.Owner.LastName);

    public static readonly Expression<Func<Vaccination, VaccinationDto>> Vaccination = v => new VaccinationDto(
        v.Id, v.PetId, v.Pet.Name, v.VaccineName, v.VaccinationDate, v.NextDoseDate, v.Description,
        v.VeterinarianId, v.Veterinarian != null ? v.Veterinarian.FullName : null);

    public static readonly Expression<Func<Appointment, AppointmentDto>> Appointment = a => new AppointmentDto(
        a.Id, a.PetId, a.Pet.Name, a.Pet.Species, a.OwnerId, a.Owner.FirstName + " " + a.Owner.LastName,
        a.VeterinarianId, a.Veterinarian.FullName, a.ServiceId, a.Service.Name, a.Service.Clinic.Name,
        a.AppointmentDate, a.Duration, a.Price, a.Status, a.OwnerNote, a.RejectionReason, a.CreatedAt,
        a.ExaminationNote != null);

    public static readonly Expression<Func<ExaminationNote, ExaminationNoteDto>> ExaminationNote = n => new ExaminationNoteDto(
        n.Id, n.AppointmentId, n.VeterinarianId, n.Veterinarian.FullName,
        n.Appointment.PetId, n.Appointment.Pet.Name, n.Appointment.Service.Name, n.Appointment.AppointmentDate,
        n.Diagnosis, n.Treatment, n.Notes, n.CreatedAt);

    /// <summary>
    /// Aşıların "yaklaşan / geciken" durumunu bugüne göre hesaplar.
    /// Aynı hayvanın aynı aşısından birden fazla kayıt varsa sadece EN SON yapılanın
    /// sonraki doz tarihi takip edilir (eski dozun tarihi yeni doz yapıldığı için geçersizdir).
    /// </summary>
    public static List<VaccinationDto> WithDueStates(IEnumerable<VaccinationDto> vaccinations, DateOnly today)
    {
        var list = vaccinations.ToList();

        var latestIds = list
            .GroupBy(v => (v.PetId, Name: v.VaccineName.Trim().ToUpperInvariant()))
            .Select(g => g.OrderByDescending(v => v.VaccinationDate).ThenByDescending(v => v.Id).First().Id)
            .ToHashSet();

        return list.Select(v =>
        {
            if (v.NextDoseDate is not DateOnly next || !latestIds.Contains(v.Id))
            {
                return v with { DueState = VaccinationDueState.None, DaysUntilNextDose = null };
            }

            var days = next.DayNumber - today.DayNumber;
            var state = days < 0
                ? VaccinationDueState.Overdue
                : days <= 30 ? VaccinationDueState.Upcoming : VaccinationDueState.Planned;

            return v with { DueState = state, DaysUntilNextDose = days };
        }).ToList();
    }
}
