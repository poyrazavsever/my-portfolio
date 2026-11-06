using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Portfolio.Areas.Admin.Models;
using Portfolio.Services;

namespace Portfolio.Areas.Admin.Controllers
{
    // Admin area'sının tamamı Program.cs'te RequireAuthorization() ile korunuyor.
    // [AllowAnonymous]: giriş sayfası bu kuralın tek istisnası (yoksa kimse giriş yapamazdı).
    [Area("Admin")]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly AdminAuthOptions _options;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IOptions<AdminAuthOptions> options, ILogger<AccountController> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // EnableRateLimiting: aynı IP'den dakikada en fazla 5 deneme (Program.cs'teki "login" politikası).
        // Kaba kuvvet (brute force) ile şifre denemeyi pratikte imkânsız hale getirir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var valid = _options.IsConfigured
                        && AdminPasswordHasher.UsernameMatches(model.Username, _options.Username)
                        && AdminPasswordHasher.Verify(model.Password, _options.PasswordHash);

            if (!valid)
            {
                _logger.LogWarning("Başarısız admin girişi. IP: {Ip}", HttpContext.Connection.RemoteIpAddress);
                // Hangisinin yanlış olduğunu söylemiyoruz: kullanıcı adı tahminini zorlaştırır.
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
                model.Password = string.Empty;
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, _options.Username),
                new(ClaimTypes.Role, "Admin"),
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe, // işaretliyse tarayıcı kapanınca da oturum sürer
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(model.RememberMe ? TimeSpan.FromDays(14) : TimeSpan.FromHours(8)),
                });

            _logger.LogInformation("Admin girişi başarılı. IP: {Ip}", HttpContext.Connection.RemoteIpAddress);
            return RedirectToLocal(model.ReturnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private IActionResult RedirectToLocal(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl)
                ? LocalRedirect(returnUrl)
                : RedirectToAction("Index", "Dashboard");
    }
}
