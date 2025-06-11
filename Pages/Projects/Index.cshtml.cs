using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Pages.Projects;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context, IConfiguration configuration)
    {
        _logger = logger;
        _context = context;
        _configuration = configuration;
    }

    public List<Project> Projects { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 6;
    public int TotalItems { get; set; }

    public async Task<IActionResult> OnGetAsync(int? page)
    {
        try
        {
            CurrentPage = page ?? 1;
            
            // Get total count of projects
            TotalItems = await _context.Projects.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

            // Ensure current page is within valid range
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // Get paginated projects
            Projects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects");
            // In a production app, you might want to show an error message to the user
        }

        return Page();
    }
}
