using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.API.Auth;
using VeterinerKlinigi.API.Contracts;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>
/// Token işlemleri. Postman, .http dosyası ve MCP Inspector gibi istemciler çerez taşımaz;
/// bu yüzden önce buradan bir JWT alır, sonraki isteklerde "Authorization: Bearer ..." başlığıyla gönderir.
/// </summary>
[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _tokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    /// <summary>
    /// POST /api/auth/token — e-posta ve parola ile JWT üretir.
    /// </summary>
    [HttpPost("token")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> CreateToken([FromBody] TokenRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return InvalidCredentials();
        }

        // lockoutOnFailure: true → 5 hatalı denemede hesap 5 dakika kilitlenir (kaba kuvvet saldırısına karşı).
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Hesap kilitli",
                detail: "Çok fazla hatalı deneme yapıldı veya hesap yönetici tarafından kilitlendi.");
        }

        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        return Ok(await _tokenService.CreateTokenAsync(user));
    }

    /// <summary>GET /api/auth/me — token'ın (veya oturumun) sahibi.</summary>
    [HttpGet("me")]
    [Authorize(Policy = ApiPolicies.ReadAccess)]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> Me()
    {
        var vetClaim = User.FindFirstValue(AppClaimTypes.VeterinarianId);
        return Ok(new CurrentUserResponse(
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            User.FindFirstValue(AppClaimTypes.FullName),
            User.FindFirstValue(ClaimTypes.Email),
            User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
            int.TryParse(vetClaim, out var vetId) ? vetId : null));
    }

    private ObjectResult InvalidCredentials()
        => Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Giriş başarısız", detail: "E-posta veya parola hatalı.");
}
