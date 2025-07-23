using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Pages.Admin.Projects
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public DeleteModel(
            ApplicationDbContext context,
            ILogger<DeleteModel> logger,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _environment = environment;
        }

        [BindProperty]
        public Project Project { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Project == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            Project = await _context.Projects.FindAsync(id);

            if (Project != null)
            {
                // Delete the associated image if it exists and it's not the default image
                if (!string.IsNullOrEmpty(Project.ImageUrl) && 
                    !Project.ImageUrl.Contains("default-project.jpg"))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, 
                        Project.ImageUrl.TrimStart('/'));
                        
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Projects.Remove(Project);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Project deleted: {ProjectId} - {Title} by {UserId}", 
                    Project.Id, Project.Title, user.Id);
            }

            return RedirectToPage("./Index");
        }
    }
}
