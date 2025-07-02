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
            _logger.LogInformation("Login işlemi başlatıldı. Email: {Email}", Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState geçersiz. Email: {Email}", Email);
                return Page();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
            var cacheKey = $"{LoginAttemptsCacheKey}{ipAddress}";
            var blockKey = $"{cacheKey}_time";

            try
            {
                if (_cache.TryGetValue(cacheKey, out int attempts) && attempts >= MaxLoginAttempts)
                {
                    var blockUntil = _cache.Get<DateTime>(blockKey);
                    var timeLeft = blockUntil - DateTime.UtcNow;
                    ModelState.AddModelError(string.Empty, $"Çok fazla başarısız giriş denemesi. {timeLeft.Minutes} dk {timeLeft.Seconds} sn sonra tekrar deneyin.");
                    _logger.LogWarning("IP bloklandı. IP: {IP}, Kalan Süre: {Time}", ipAddress, timeLeft);
                    return Page();
                }

                _logger.LogInformation("Giriş denemesi. IP: {IP}, Email: {Email}", ipAddress, Email);

                var loginSuccess = await _authService.LoginAsync(Email, Password, RememberMe);

                if (!loginSuccess)
                {
                    attempts++;
                    _cache.Set(cacheKey, attempts, TimeSpan.FromMinutes(15));

                    if (attempts >= MaxLoginAttempts)
                    {
                        _cache.Set(blockKey, DateTime.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(15));
                        _logger.LogWarning("Çok sayıda hatalı giriş. IP: {IP}, Email: {Email}", ipAddress, Email);
                    }

                    ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
                    return Page();
                }

                // Giriş başarılı, önbelleği temizle
                _cache.Remove(cacheKey);
                _cache.Remove(blockKey);
                _logger.LogInformation("Giriş başarılı. Email: {Email}", Email);

                // Claims oluştur
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Email),
            new Claim(ClaimTypes.Email, Email),
            new Claim(ClaimTypes.Role, "Administrator")
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(1))
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
                _logger.LogInformation("Kimlik doğrulama ve cookie atama başarılı. Email: {Email}", Email);

                return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login sırasında beklenmeyen hata. Email: {Email}", Email);
                ErrorMessage = "Bir hata oluştu. Lütfen tekrar deneyin.";
                return Page();
            }
            finally
            {
                _logger.LogInformation("=== Login işlemi sona erdi ===");
            }
        }

    }
}
