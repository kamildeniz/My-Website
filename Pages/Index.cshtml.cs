using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext _context;

    public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public List<Project> RecentProjects { get; set; } = new();
    public List<BlogPost> RecentPosts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
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
