using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using VeterinerKlinigi.API.Contracts;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.API.Auth;

/// <summary>JWT (JSON Web Token) üretir.</summary>
public interface IJwtTokenService
{
    Task<TokenResponse> CreateTokenAsync(ApplicationUser user);

    /// <summary>Anahtar rastgele üretildiyse (user-secrets'ta yoksa) true.</summary>
    bool IsEphemeralKey { get; }
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;
    private readonly JwtOptions _jwt;
    private readonly IdentityOptions _identityOptions;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<JwtOptions> jwtOptions,
        IOptions<IdentityOptions> identityOptions,
        TimeProvider timeProvider)
    {
        _claimsFactory = claimsFactory;
        _jwt = jwtOptions.Value;
        _identityOptions = identityOptions.Value;
        _timeProvider = timeProvider;
    }

    public bool IsEphemeralKey => _jwt.IsEphemeralKey;

    public async Task<TokenResponse> CreateTokenAsync(ApplicationUser user)
    {
        // Claim'ler, tarayıcı oturumu (çerez) ile AYNI fabrikadan üretilir:
        // kullanıcı Id, e-posta, roller, ad-soyad ve (hekimse) vet_id.
        var principal = await _claimsFactory.CreateAsync(user);
        var claims = principal.Claims
            .Where(c => c.Type != _identityOptions.ClaimsIdentity.SecurityStampClaimType)
            .ToList();
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        var roles = claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

        return new TokenResponse(token, "Bearer", expires, user.Email ?? string.Empty, roles);
    }
}
