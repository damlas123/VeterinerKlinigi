using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Concrete;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Data.Context;
using VeterinerKlinigi.Tests.Infrastructure;

namespace VeterinerKlinigi.Tests;

/// <summary>
/// Ödev şartı: "Bir üye, adres çubuğunda id değiştirerek başka bir üyenin hayvanını veya randevusunu görememelidir."
/// Sahiplik kuralları (AccessScopes) servis katmanında uygulandığı için MVC, REST API ve MCP'de aynıdır.
/// </summary>
public sealed class AccessScopeTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly List<AppDbContext> _contexts = [];

    public void Dispose()
    {
        _contexts.ForEach(c => c.Dispose());
        _db.Dispose();
    }

    private PetManager PetsFor(ICurrentUserService user)
    {
        var context = _db.CreateContext();
        _contexts.Add(context);
        return new PetManager(_db.CreateUnitOfWork(context), user, TestDatabase.Clock);
    }

    private VaccinationManager VaccinationsFor(ICurrentUserService user)
    {
        var context = _db.CreateContext();
        _contexts.Add(context);
        return new VaccinationManager(_db.CreateUnitOfWork(context), user, TestDatabase.Clock);
    }

    [Fact]
    public async Task Member_SeesOnlyOwnPets()
    {
        var pets = await PetsFor(_db.Member1).GetListAsync();

        Assert.Equal(["Pamuk"], pets.Select(p => p.Name).ToArray());
    }

    [Fact]
    public async Task Member_GettingAnotherMembersPetById_ReturnsNotFound()
    {
        var result = await PetsFor(_db.Member1).GetDetailAsync(_db.Member2PetId);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Member_CannotDeleteAnotherMembersPet()
    {
        var result = await PetsFor(_db.Member1).DeleteAsync(_db.Member2PetId);

        Assert.False(result.IsSuccess);
        Assert.Single(await PetsFor(_db.Member2).GetListAsync());
    }

    [Fact]
    public async Task Admin_SeesAllPets()
    {
        Assert.Equal(2, (await PetsFor(TestDatabase.Admin).GetListAsync()).Count);
    }

    [Fact]
    public async Task Veterinarian_SeesOnlyPatientsWithAnAppointment()
    {
        // Henüz hiç randevu yokken veterinerin hastası yoktur.
        Assert.Empty(await PetsFor(_db.Vet).GetListAsync());
        Assert.Equal(ErrorType.NotFound, (await PetsFor(_db.Vet).GetDetailAsync(_db.Member1PetId)).ErrorType);
    }

    [Fact]
    public async Task Member_CannotReadAnotherMembersVaccinationSchedule()
    {
        var result = await VaccinationsFor(_db.Member1).GetScheduleByPetAsync(_db.Member2PetId);

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Admin_AddsVaccination_OwnerSeesUpcomingDose()
    {
        var created = await VaccinationsFor(TestDatabase.Admin).CreateAsync(new VaccinationSaveDto
        {
            PetId = _db.Member1PetId,
            VaccineName = "Kuduz",
            VaccinationDate = new DateOnly(2026, 11, 20),
            NextDoseDate = new DateOnly(2027, 11, 20)
        });
        Assert.True(created.IsSuccess, created.Message);

        var schedule = await VaccinationsFor(_db.Member1).GetScheduleByPetAsync(_db.Member1PetId);

        Assert.True(schedule.IsSuccess, schedule.Message);
        Assert.Contains(schedule.Data!.History, v => v.VaccineName == "Kuduz");
        Assert.Contains(schedule.Data.Upcoming, v => v.VaccineName == "Kuduz");
    }
}
