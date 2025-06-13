using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        private readonly AuthService _authService;
        private readonly ILogger<LoginModel> _logger;

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

        public LoginModel(AuthService authService, ILogger<LoginModel> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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
            
            try
            {
                // Brute-force koruması
                if (!string.IsNullOrEmpty(ipAddress) && 
                    _cache.TryGetValue($"{LoginAttemptsCacheKey}{ipAddress}", out int attempts) && 
                    attempts >= MaxLoginAttempts)
                {
                    var remainingTime = _cache.Get<DateTime>($"{LoginAttemptsCacheKey}{ipAddress}_time");
                    var timeLeft = remainingTime - DateTime.UtcNow;
                    ModelState.AddModelError(string.Empty, 
                        $"Çok fazla başarısız giriş denemesi. Lütfen {timeLeft.Minutes} dakika {timeLeft.Seconds} saniye sonra tekrar deneyin.");
                    return Page();
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
                        var cacheKey = $"{LoginAttemptsCacheKey}{ipAddress}";
                        var attempts = _cache.GetOrCreate(cacheKey, entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow = LoginAttemptsWindow;
                            return 0;
                        });
                        
                        attempts++;
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

                // Başarılı giriş sonrası yönlendirme
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
