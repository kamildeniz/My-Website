using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PortfolioApp.Pages.Admin
{

    [AllowAnonymous]

    public class LoginModel : PageModel
    {
        private const int MaxLoginAttempts = 5;
        private static readonly TimeSpan LoginAttemptsWindow = TimeSpan.FromMinutes(15);
        private const string LoginAttemptsCacheKey = "LoginAttempts_{0}";

        private readonly AuthService _authService;
        private readonly ILogger<LoginModel> _logger;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _dbContext;
        [BindProperty]
        [Required(ErrorMessage = "E-posta zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        public string Email { get; set; } = "admin@example.com";

        [BindProperty]
        [Required(ErrorMessage = "Şifre zorunludur")]
        public string Password { get; set; } = "Admin123!";

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; } = "/Admin/Dashboard";

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public LoginModel(AuthService authService, ILogger<LoginModel> logger, IMemoryCache cache, ApplicationDbContext dbContext)
        {
            _authService = authService;
            _logger = logger;
            _cache = cache;
            _dbContext = dbContext;
        }

        [TempData]
        public string SuccessMessage { get; set; }

        [BindProperty]
        public bool RememberMe { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            // Çıkış yapıldıysa başarı mesajı göster
            if (TempData["LogoutMessage"] is string logoutMessage)
            {
                SuccessMessage = logoutMessage;
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            ReturnUrl = returnUrl;

            // Hatalı giriş denemelerini kontrol et
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress) &&
                _cache.TryGetValue($"{LoginAttemptsCacheKey}{ipAddress}", out int attempts) &&
                attempts >= MaxLoginAttempts)
            {
                var remainingTime = _cache.Get<DateTime>($"{LoginAttemptsCacheKey}{ipAddress}_time");
                var timeLeft = remainingTime - DateTime.UtcNow;
                ModelState.AddModelError(string.Empty,
                    $"Çok fazla başarısız giriş denemesi. Lütfen {timeLeft.Minutes} dakika {timeLeft.Seconds} saniye sonra tekrar deneyin.");
            }
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Login started for email: {Email}", Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid.");
                return Page();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var cacheKey = string.Format(LoginAttemptsCacheKey, ipAddress);

            if (_cache.TryGetValue(cacheKey, out int attempts) && attempts >= MaxLoginAttempts)
            {
                var blockUntil = _cache.Get<DateTime>($"{cacheKey}_time");
                var timeLeft = blockUntil - DateTime.UtcNow;
                ModelState.AddModelError(string.Empty, $"Çok fazla başarısız giriş. {timeLeft.Minutes} dk {timeLeft.Seconds} sn bekleyin.");
                return Page();
            }

            var admin = await _authService.ValidateCredentialsAsync(Email, Password);
            if (admin == null)
            {
                attempts++;
                _cache.Set(cacheKey, attempts, LoginAttemptsWindow);

                if (attempts >= MaxLoginAttempts)
                    _cache.Set($"{cacheKey}_time", DateTime.UtcNow.Add(LoginAttemptsWindow), LoginAttemptsWindow);

                ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
                return Page();
            }

            _cache.Remove(cacheKey);
            _cache.Remove($"{cacheKey}_time");

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
        new Claim(ClaimTypes.Name, admin.Email),
        new Claim(ClaimTypes.Email, admin.Email),
        new Claim(ClaimTypes.Role, "Administrator")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.Add(RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(1))
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            _logger.LogInformation("User signed in: {Email}", Email);

            return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Admin/Dashboard");
        }

    }
}
