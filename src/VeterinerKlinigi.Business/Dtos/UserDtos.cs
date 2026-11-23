namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Admin panelindeki kullanıcı listesi satırı.</summary>
public record UserListItemDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    IReadOnlyList<string> Roles,
    bool IsLockedOut,
    int PetCount,
    int AppointmentCount,
    DateTime CreatedAt);

/// <summary>Kullanıcı detayı (hayvanları ve son randevuları).</summary>
public record UserDetailDto(
    UserListItemDto User,
    IReadOnlyList<PetDto> Pets,
    IReadOnlyList<AppointmentDto> RecentAppointments);
