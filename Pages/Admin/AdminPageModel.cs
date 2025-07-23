using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioApp.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AdminPageModel : PageModel
    {
        protected readonly ILogger<AdminPageModel> _logger;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly SignInManager<ApplicationUser> _signInManager;
        protected readonly IAuthorizationService _authorizationService;

        public AdminPageModel(
            ILogger<AdminPageModel> logger,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuthorizationService authorizationService)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _authorizationService = authorizationService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Identity/Account/Login", new { returnUrl = Request.Path });
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, "AdminPolicy");
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }
    }
}
