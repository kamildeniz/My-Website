using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.Pages
{
    public class AboutModel : PageModel
    {
        private readonly ILogger<AboutModel> _logger;
        private readonly ProfileService _profileService;

        public Profile? Profile { get; set; }

        public AboutModel(ILogger<AboutModel> logger, ProfileService profileService)
        {
            _logger = logger;
            _profileService = profileService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                Profile = await _profileService.GetProfileAsync();
                if (Profile == null)
                {
                    _logger.LogWarning("Profile not found");
                    return NotFound();
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading about page");
                throw;
            }
        }
    }
}
