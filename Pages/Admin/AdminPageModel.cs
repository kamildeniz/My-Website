using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using PortfolioApp.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PortfolioApp.Pages.Admin
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public abstract class AdminPageModel : PageModel
    {
        protected readonly AuthService _authService;
        protected readonly ILogger<AdminPageModel> _logger;

        protected AdminPageModel(AuthService authService, ILogger<AdminPageModel> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        public virtual Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation($"AdminPageModel.OnGetAsync called for {GetType().Name}");
            _logger.LogInformation($"User {User.Identity.Name} is authenticated");
            return Task.FromResult<IActionResult>(Page());
        }

        public virtual Task<IActionResult> OnGetAsync(int? id)
        {
            _logger.LogInformation($"AdminPageModel.OnGetAsync(int?) called for {GetType().Name} with id: {id}");
            _logger.LogInformation($"User {User.Identity.Name} is authenticated");
            return Task.FromResult<IActionResult>(Page());
        }

        protected IActionResult RedirectToLogin()
        {
            var returnUrl = $"{Request.Path}{Request.QueryString}";
            _logger.LogInformation($"Redirecting to login with returnUrl: {returnUrl}");
            return RedirectToPage("/Admin/Login", new { returnUrl });
        }
    }
}
