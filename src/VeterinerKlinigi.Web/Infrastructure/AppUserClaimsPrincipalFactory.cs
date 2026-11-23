using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Data.Context;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Web.Infrastructure;

/// <summary>
/// Kullanıcı giriş yaptığında oturuma (çerez) ve JWT'ye eklenen claim'leri üretir.
/// Identity'nin standart claim'lerine (Id, e-posta, roller) ek olarak:
///   full_name → menüde göstermek için,
///   vet_id    → kullanıcı veteriner ise hekim Id'si (her istekte veritabanına sormamak için).
/// </summary>
public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly AppDbContext _context;

    public AppUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options,
        AppDbContext context)
        : base(userManager, roleManager, options)
    {
        _context = context;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        identity.AddClaim(new Claim(AppClaimTypes.FullName, user.FullName));

        var veterinarianId = await _context.Veterinarians
            .Where(v => v.UserId == user.Id)
            .Select(v => (int?)v.Id)
            .FirstOrDefaultAsync();

        if (veterinarianId is int id)
        {
            identity.AddClaim(new Claim(AppClaimTypes.VeterinarianId, id.ToString()));
        }

        return identity;
    }
}
