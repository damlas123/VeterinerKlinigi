using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Concrete;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Business.Options;
using VeterinerKlinigi.Data.Context;
using VeterinerKlinigi.Entities.Enums;
using VeterinerKlinigi.Tests.Infrastructure;

namespace VeterinerKlinigi.Tests;

/// <summary>
/// Randevu servisinin (AppointmentManager) iş kuralları. MVC, REST API ve MCP bu servisi ortak kullandığı için
/// buradaki testler üç kanalın da davranışını doğrular.
/// </summary>
public sealed class AppointmentManagerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly List<AppDbContext> _contexts = [];

    public void Dispose()
    {
        _contexts.ForEach(c => c.Dispose());
        _db.Dispose();
    }

    private AppointmentManager ManagerFor(ICurrentUserService user)
    {
        var context = _db.CreateContext();
        _contexts.Add(context);
        return new AppointmentManager(_db.CreateUnitOfWork(context), user, TestDatabase.Clock, Options.Create(new AppointmentOptions()));
    }

    private AppointmentSaveDto Request(int hour, int minute, int? serviceId = null, int? petId = null, int? vetId = null) => new()
    {
        PetId = petId ?? _db.Member1PetId,
        ServiceId = serviceId ?? _db.ExamServiceId,
        VeterinarianId = vetId ?? _db.VetId,
        AppointmentDate = TestDatabase.Tomorrow.ToDateTime(new TimeOnly(hour, minute))
    };

    [Fact]
    public async Task Create_StartsAsPending_AndCopiesDurationAndPriceFromService()
    {
        var result = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0));

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(AppointmentStatus.Pending, result.Data!.Status);
        Assert.Equal(30, result.Data.Duration);
        Assert.Equal(750m, result.Data.Price);
    }

    [Fact]
    public async Task Create_OverlappingByServiceDuration_ReturnsConflict_ButBackToBackIsAccepted()
    {
        Assert.True((await ManagerFor(_db.Member1).CreateAsync(Request(10, 0))).IsSuccess);

        // 10:00-10:30 dolu iken 10:15 başlangıcı (10:15-10:45) çakışır.
        var overlapping = await ManagerFor(_db.Member2).CreateAsync(Request(10, 15, petId: _db.Member2PetId));
        Assert.Equal(ErrorType.Conflict, overlapping.ErrorType);
        Assert.Contains("10:00-10:30", overlapping.Message);

        // 10:30 başlangıcı çakışmaz.
        var backToBack = await ManagerFor(_db.Member2).CreateAsync(Request(10, 30, petId: _db.Member2PetId));
        Assert.True(backToBack.IsSuccess, backToBack.Message);
    }

    [Fact]
    public async Task Create_LongServiceOverlappingLaterAppointment_ReturnsConflict()
    {
        Assert.True((await ManagerFor(_db.Member2).CreateAsync(Request(11, 0, petId: _db.Member2PetId))).IsSuccess);

        // 120 dakikalık hizmet 09:30'da başlarsa 11:30'a kadar sürer → 11:00 randevusuyla çakışır.
        var result = await ManagerFor(_db.Member1).CreateAsync(Request(9, 30, serviceId: _db.SurgeryServiceId));

        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Create_RejectedAppointmentFreesTheSlot()
    {
        var first = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0));
        Assert.True((await ManagerFor(_db.Vet).RejectAsync(first.Data!.Id, "Hekim o gün izinli")).IsSuccess);

        var second = await ManagerFor(_db.Member2).CreateAsync(Request(10, 0, petId: _db.Member2PetId));

        Assert.True(second.IsSuccess, second.Message);
    }

    [Theory]
    [InlineData(17, 0)]  // hekimin müsaitliği 17:00'de bitiyor
    [InlineData(16, 45)] // 30 dk sürdüğü için 17:15'te biter → müsaitlik dışı
    [InlineData(8, 30)]  // klinik 09:00'da açılıyor
    public async Task Create_OutsideAvailabilityOrClinicHours_IsInvalid(int hour, int minute)
    {
        var result = await ManagerFor(_db.Member1).CreateAsync(Request(hour, minute));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_VeterinarianWithoutWorkingHours_IsInvalid()
    {
        var result = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0, serviceId: _db.SurgeryServiceId, vetId: _db.Vet2Id));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_VeterinarianNotProvidingService_IsInvalid()
    {
        var result = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0, serviceId: _db.ExamServiceId, vetId: _db.Vet2Id));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_PastTimeOrUnalignedMinute_IsInvalid()
    {
        var past = new AppointmentSaveDto
        {
            PetId = _db.Member1PetId,
            ServiceId = _db.ExamServiceId,
            VeterinarianId = _db.VetId,
            AppointmentDate = TestDatabase.FixedNow.UtcDateTime.AddHours(-1)
        };

        Assert.Equal(ErrorType.Validation, (await ManagerFor(_db.Member1).CreateAsync(past)).ErrorType);
        Assert.Equal(ErrorType.Validation, (await ManagerFor(_db.Member1).CreateAsync(Request(10, 7))).ErrorType);
    }

    [Fact]
    public async Task Create_ForAnotherMembersPet_ReturnsNotFound()
    {
        var result = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0, petId: _db.Member2PetId));

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Create_ByVeterinarian_IsForbidden()
    {
        var result = await ManagerFor(_db.Vet).CreateAsync(Request(10, 0));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Approve_OnlyAssignedVeterinarianOrAdmin()
    {
        var created = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0));
        var id = created.Data!.Id;

        Assert.Equal(ErrorType.NotFound, (await ManagerFor(_db.Vet2).ApproveAsync(id)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await ManagerFor(_db.Member1).ApproveAsync(id)).ErrorType);
        Assert.True((await ManagerFor(_db.Vet).ApproveAsync(id)).IsSuccess);

        // Onaylanan randevu ikinci kez onaylanamaz.
        Assert.Equal(ErrorType.Validation, (await ManagerFor(TestDatabase.Admin).ApproveAsync(id)).ErrorType);
    }

    [Fact]
    public async Task Reject_RequiresReason()
    {
        var created = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0));

        var result = await ManagerFor(_db.Vet).RejectAsync(created.Data!.Id, "  ");

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Cancel_OwnerCanCancel_OtherMemberCannotSeeIt()
    {
        var created = await ManagerFor(_db.Member1).CreateAsync(Request(10, 0));
        var id = created.Data!.Id;

        Assert.Equal(ErrorType.NotFound, (await ManagerFor(_db.Member2).CancelAsync(id)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await ManagerFor(_db.Member2).GetDetailAsync(id)).ErrorType);
        Assert.True((await ManagerFor(_db.Member1).CancelAsync(id)).IsSuccess);

        await using var context = _db.CreateContext();
        Assert.Equal(AppointmentStatus.Cancelled, (await context.Appointments.SingleAsync(a => a.Id == id)).Status);
    }

    [Fact]
    public async Task AvailableSlots_ExcludeBookedTimes()
    {
        Assert.True((await ManagerFor(_db.Member1).CreateAsync(Request(10, 0))).IsSuccess);

        var result = await ManagerFor(_db.Member2).GetAvailableSlotsAsync(_db.ExamServiceId, TestDatabase.Tomorrow);

        Assert.True(result.IsSuccess, result.Message);
        var ayse = Assert.Single(result.Data!); // Mehmet bu hizmeti vermiyor
        Assert.DoesNotContain("10:00", ayse.Slots);
        Assert.DoesNotContain("09:45", ayse.Slots); // 09:45-10:15 çakışır
        Assert.Contains("09:30", ayse.Slots);
        Assert.Contains("10:30", ayse.Slots);
        Assert.Equal("16:30", ayse.Slots[^1]); // 30 dk'lık son randevu 17:00'de biter
    }
}
