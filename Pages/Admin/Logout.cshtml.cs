using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;
using PortfolioApp.Services;

namespace PortfolioApp.Pages.Admin
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;
        private readonly AuthService _authService;

        public LogoutModel(ILogger<LogoutModel> logger, AuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Sadece POST isteklerine izin ver
            if (HttpContext.Request.Method != "POST")
            {
                return Page();
            }
            
            return await OnPostAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
            _logger.LogInformation($"User {userEmail} is logging out.");

            try
            {
                // Kullanıcıyı çıkış yaptır
                await _authService.LogoutAsync();
                _logger.LogInformation($"User {userEmail} has been logged out successfully.");

                // Çıkış sonrası login sayfasına yönlendir
                TempData["LogoutMessage"] = "Başarıyla çıkış yapıldı.";
                return RedirectToPage("/Admin/Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred during logout for user {userEmail}");
                
                // Hata durumunda da login sayfasına yönlendir
                TempData["ErrorMessage"] = "Çıkış işlemi sırasında bir hata oluştu. Lütfen tekrar deneyiniz.";
                return RedirectToPage("/Admin/Login");
            }
        }
    }
}
