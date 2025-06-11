using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Pages.Projects;

public class DetailsModel : PageModel
{
    private readonly ILogger<DetailsModel> _logger;
    private readonly ApplicationDbContext _context;

    public DetailsModel(ILogger<DetailsModel> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public Project? Project { get; set; }
    public List<Project> RelatedProjects { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            // Get the project with the specified ID
            Project = await _context.Projects.FindAsync(id);

            if (Project == null)
            {
                _logger.LogWarning("Project with ID {ProjectId} not found", id);
                return Page();
            }

            // Get related projects (projects with similar technologies)
            if (Project.Technologies != null && Project.Technologies.Any())
            {
                var techList = Project.Technologies.ToList();
                
                // Get projects that share at least one technology with the current project
                RelatedProjects = await _context.Projects
                    .Where(p => p.Id != id && 
                              p.Technologies != null && 
                              p.Technologies.Any(t => techList.Contains(t)))
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(4) // Get one more than needed in case we need to exclude the current project
                    .ToListAsync();

                // If we don't have enough related projects, get the most recent ones
                if (RelatedProjects.Count < 3)
                {
                    var additionalProjects = await _context.Projects
                        .Where(p => p.Id != id)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(3 - RelatedProjects.Count)
                        .ToListAsync();

                    // Add only the ones that aren't already in the list
                    RelatedProjects = RelatedProjects
                        .Union(additionalProjects.Where(ap => !RelatedProjects.Any(rp => rp.Id == ap.Id)))
                        .Take(3)
                        .ToList();
                }
                else
                {
                    RelatedProjects = RelatedProjects.Take(3).ToList();
                }
            }
            else
            {
                // If no technologies are specified, just get the most recent projects
                RelatedProjects = await _context.Projects
                    .Where(p => p.Id != id)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project details for ID {ProjectId}", id);
            // In a production app, you might want to show an error message to the user
        }

        return Page();
    }
}
