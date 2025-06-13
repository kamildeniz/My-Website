using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.Pages.Admin.Projects
{
    public class DeleteModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(ApplicationDbContext context, IWebHostEnvironment environment, AuthService authService, ILogger<DeleteModel> logger)
            : base(authService, logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public Project Project { get; set; } = default!;

        public override async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var authResult = await base.OnGetAsync();
            if (authResult is not PageResult)
            {
                return authResult;
            }

            var project = await _context.Projects.FirstOrDefaultAsync(m => m.Id == id);

            if (project == null)
            {
                return NotFound();
            }
            
            Project = project;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);

            if (project != null)
            {
                // Delete the associated image if it exists and it's not the default image
                if (!string.IsNullOrEmpty(project.ImageUrl) && !project.ImageUrl.Contains("default-project.jpg"))
                {
                    var imagePath = Path.Combine(_environment.WebRootPath, project.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }


                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }


            return RedirectToPage("./Index");
        }
    }
}
