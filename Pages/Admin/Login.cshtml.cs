using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
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

        public LoginModel(AuthService authService, ILogger<LoginModel> logger, IMemoryCache cache)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var attempts = 0;
            
            try
            {
                // Brute-force koruması
                if (!string.IsNullOrEmpty(ipAddress) && 
                    _cache.TryGetValue($"{LoginAttemptsCacheKey}{ipAddress}", out int cachedAttempts))
                {
                    attempts = cachedAttempts;
                    if (attempts >= MaxLoginAttempts)
                    {
                        var remainingTime = _cache.Get<DateTime>($"{LoginAttemptsCacheKey}{ipAddress}_time");
                        var timeLeft = remainingTime - DateTime.UtcNow;
                        ModelState.AddModelError(string.Empty, 
                            $"Çok fazla başarısız giriş denemesi. Lütfen {timeLeft.Minutes} dakika {timeLeft.Seconds} saniye sonra tekrar deneyin.");
                        return Page();
                    }
                }

                // Giriş denemesini logla
                _logger.LogInformation("Login attempt for email: {Email} from IP: {IP}", 
                    Email, ipAddress ?? "unknown");

                // Kimlik doğrulama işlemi
                var loginResult = await _authService.LoginAsync(Email, Password, RememberMe);
                
                if (!loginResult)
                {
                    // Başarısız giriş denemesi sayısını artır
                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        attempts++;
                        var cacheKey = $"{LoginAttemptsCacheKey}{ipAddress}";
                        _cache.Set(cacheKey, attempts, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = LoginAttemptsWindow
                        });
                        
                        // Zaman damgasını kaydet
                        if (attempts >= MaxLoginAttempts)
                        {
                            _cache.Set($"{cacheKey}_time", DateTime.UtcNow.Add(LoginAttemptsWindow), 
                                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = LoginAttemptsWindow });
                            
                            _logger.LogWarning("Too many failed login attempts for IP: {IP}. Blocked for {Minutes} minutes.", 
                                ipAddress, LoginAttemptsWindow.TotalMinutes);
                        }
                    }

                    _logger.LogWarning("Login failed for email: {Email}", Email);
                    ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre. Lütfen tekrar deneyin.");
                    return Page();
                }


                // Başarılı girişte önbelleği temizle
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    _cache.Remove($"{LoginAttemptsCacheKey}{ipAddress}");
                    _cache.Remove($"{LoginAttemptsCacheKey}{ipAddress}_time");
                }

                _logger.LogInformation("Login successful for email: {Email}", Email);

                // Kullanıcı için kimlik oluştur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Email),
                    new Claim(ClaimTypes.Email, Email),
                    new Claim(ClaimTypes.Role, "Administrator")
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    IsPersistent = RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(1))
                };

                // Başarılı giriş sonrası yönlendirme
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                
                _logger.LogInformation("User signed in successfully");
                
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    _logger.LogInformation("Redirecting to return URL: {ReturnUrl}", ReturnUrl);
                    return LocalRedirect(ReturnUrl);
                }
                
                _logger.LogInformation("Redirecting to Dashboard");
                return RedirectToPage("/Admin/Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", Email);
                ErrorMessage = $"Giriş işlemi sırasında bir hata oluştu: {ex.Message}";
                return Page();
            }
            finally
            {
                _logger.LogInformation("=== LOGIN END ===");
            }
        }
    }
}
