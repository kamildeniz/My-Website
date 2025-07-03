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


    }
}
