using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;
using System.Threading.Tasks;

namespace PortfolioApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ProfileService _profileService;
    private readonly ApplicationDbContext _context;

    public List<BlogPost> BlogPosts { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public Models.Profile? Profile { get; set; }
    public int CurrentPage { get; set; } = 1;
    public List<Project> RecentProjects { get; set; } = new();
    public List<BlogPost> RecentPosts { get; set; } = new();
    public HomeContent? HomeContent { get; set; }

    public IndexModel(ILogger<IndexModel> logger, ProfileService profileService, ApplicationDbContext context)
    {
        _logger = logger;
        _profileService = profileService;
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync()
    {
       
    
        try
        {
            // Get paginated projects
            Projects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            // Get profile data
            Profile = await _profileService.GetProfileAsync();
            
            // Get home page content
            HomeContent = await _context.HomeContents
                .FirstOrDefaultAsync(h => h.PageName == HomeContent.PageNames.Home) ?? new HomeContent
                {
                    PageName = HomeContent.PageNames.Home,
                    Content = "<h1>Hoş Geldiniz</h1><p>Web siteme hoş geldiniz. Bu alanı yönetim panelinden düzenleyebilirsiniz.</p>"
                };
            // Get all published posts ordered by date (newest first)
            var query = _context.BlogPosts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt);

            // Get posts for current page
            BlogPosts = await query.ToListAsync();
            // Get 3 most recent projects
            RecentProjects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();

            // Get 3 most recent published blog posts
            RecentPosts = await _context.BlogPosts
                .Where(p => p.IsPublished)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading data for home page");
            // In a production app, you might want to handle this differently
            // For now, we'll just continue with empty lists
        }

        return Page();
    }
}
