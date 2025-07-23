using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.Pages
{
    public class AboutModel : PageModel
    {
        private readonly ILogger<AboutModel> _logger;
        private readonly ProfileService _profileService;
        private readonly ApplicationDbContext _context;

        public Profile? Profile { get; set; }
        public HomeContent? AboutContent { get; set; }

        public AboutModel(ILogger<AboutModel> logger, ProfileService profileService, ApplicationDbContext context)
        {
            _logger = logger;
            _profileService = profileService;
            _context = context;
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

                // Get about page content
                AboutContent = await _context.HomeContents
                    .FirstOrDefaultAsync(h => h.PageName == HomeContent.PageNames.About) ?? new HomeContent
                    {
                        PageName = HomeContent.PageNames.About,
                        Content = $"<h2>Merhaba, ben {Profile.FullName}</h2><p class=\"lead\">{Profile.Title}</p><p>{Profile.AboutMe}</p>"
                    };

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
