using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.API.Auth;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Controllers;

/// <summary>
/// Hesap işlemleri: giriş, kayıt, çıkış, profil, parola değiştirme ve API/MCP anahtarı.
/// Kimlik doğrulama ASP.NET Core Identity ile yapılır (parolalar hash'lenerek saklanır).
/// </summary>
public class AccountController : AppController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService tokenService,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    // ------------------------------------------------------------------
    // GİRİŞ
    // ------------------------------------------------------------------
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Panel");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Kullanıcı adı olarak e-posta kullanılır. lockoutOnFailure: 5 hatalı denemede 5 dk kilit.
        var result = await _signInManager.PasswordSignInAsync(model.Email.Trim(), model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Açık yönlendirme (open redirect) saldırısına karşı sadece site içi adreslere dönülür.
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return LocalRedirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Panel");
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Hesabınız geçici olarak kilitlendi. Lütfen birkaç dakika sonra tekrar deneyin."
            : "E-posta veya parola hatalı.");
        return View(model);
    }

    // ------------------------------------------------------------------
    // KAYIT (sadece Üye rolüyle; Admin ve Veteriner hesaplarını admin oluşturur)
    // ------------------------------------------------------------------
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Panel");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim();
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            CreatedAt = _timeProvider.GetLocalNow().DateTime
        };

        // Parola politikası (en az 8 karakter, büyük/küçük harf, rakam) burada uygulanır.
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var key = error.Code.StartsWith("Password", StringComparison.Ordinal) ? nameof(model.Password)
                    : error.Code.Contains("Email", StringComparison.Ordinal) || error.Code.Contains("UserName", StringComparison.Ordinal) ? nameof(model.Email)
                    : string.Empty;
                ModelState.AddModelError(key, error.Description);
            }

            return View(model);
        }

        await _userManager.AddToRoleAsync(user, RoleNames.Member);
        await _signInManager.SignInAsync(user, isPersistent: false);

        ShowSuccess($"Hoş geldiniz {user.FirstName}! Şimdi ilk evcil hayvanınızı ekleyebilirsiniz.");
        return RedirectToAction("Index", "Dashboard", new { area = "Member" });
    }

    // ------------------------------------------------------------------
    // ÇIKIŞ (POST + anti-forgery: başka bir site kullanıcıyı zorla çıkaramaz)
    // ------------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        ShowSuccess("Güvenli bir şekilde çıkış yaptınız.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ------------------------------------------------------------------
    // PROFİL
    // ------------------------------------------------------------------
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email ?? string.Empty,
            Roles = await _userManager.GetRolesAsync(user),
            CreatedAt = user.CreatedAt
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? string.Empty;
            model.Roles = await _userManager.GetRolesAsync(user);
            model.CreatedAt = user.CreatedAt;
            return View(model);
        }

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        await _userManager.UpdateAsync(user);

        // Menüdeki ad-soyad claim'i güncellensin diye oturum yenilenir.
        await _signInManager.RefreshSignInAsync(user);

        ShowSuccess("Profil bilgileriniz güncellendi.");
        return RedirectToAction(nameof(Profile));
    }

    // ------------------------------------------------------------------
    // PAROLA DEĞİŞTİRME
    // ------------------------------------------------------------------
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                var key = error.Code == "PasswordMismatch" ? nameof(model.CurrentPassword) : nameof(model.NewPassword);
                ModelState.AddModelError(key, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        ShowSuccess("Parolanız değiştirildi.");
        return RedirectToAction(nameof(Profile));
    }

    // ------------------------------------------------------------------
    // API / MCP ERİŞİM ANAHTARI (JWT)
    // ------------------------------------------------------------------
    [HttpGet]
    [Authorize]
    public IActionResult ApiToken() => View(NewTokenModel());

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ApiToken(string? unused = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var token = await _tokenService.CreateTokenAsync(user);
        var model = NewTokenModel();
        model.Token = token.AccessToken;
        model.ExpiresAt = token.ExpiresAt;
        return View(model);
    }

    private ApiTokenViewModel NewTokenModel() => new()
    {
        IsEphemeralKey = _tokenService.IsEphemeralKey,
        BaseUrl = $"{Request.Scheme}://{Request.Host}"
    };
}
