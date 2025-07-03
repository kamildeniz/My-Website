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
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            ReturnUrl = returnUrl ?? "/Admin/Dashboard";
        }

        public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("--- [Login Page] POST request received. ---");
            _logger.LogInformation("[Login Page] Attempting login for Email: {Email}", Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("[Login Page] ModelState is INVALID. Returning page.");
                return Page();
            }
            _logger.LogInformation("[Login Page] ModelState is VALID.");

            var user = await _authService.ValidateCredentialsAsync(Email, Password);

            if (user != null)
            {
                _logger.LogInformation("[Login Page] AuthService.ValidateCredentialsAsync SUCCEEDED for user ID {UserId}.", user.Id);
                _logger.LogInformation("[Login Page] Proceeding to sign in user...");

                await _authService.LoginAsync(user);

                _logger.LogInformation("[Login Page] SignInAsync completed. User should be authenticated.");

                var returnUrl = ReturnUrl ?? Url.Content("~/Admin/Dashboard");
                _logger.LogInformation("[Login Page] Login successful. Redirecting to final destination: {ReturnUrl}", returnUrl);

                return LocalRedirect(returnUrl);
            }

            _logger.LogWarning("!!! [Login Page] AuthService.ValidateCredentialsAsync FAILED for {Email}. !!!", Email);
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

    }
}
