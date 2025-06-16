using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.Pages.Admin.Projects
{
    public class EditModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private new readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, IWebHostEnvironment environment, AuthService authService, ILogger<EditModel> logger)
            : base(authService, logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public Project Project { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

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

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Handle image upload if a new file is provided
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "projects");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(Project.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, Project.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath) && !oldImagePath.Contains("default-project.jpg"))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Generate unique filename for new image
                var uniqueFileName = $"{Guid.NewGuid()}_{ImageFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the new file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }

                // Update the image URL
                Project.ImageUrl = $"/images/projects/{uniqueFileName}";
            }

            else
            {
                // Keep the existing image URL
                var existingProject = await _context.Projects
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == Project.Id);
                
                if (existingProject != null)
                {
                    Project.ImageUrl = existingProject.ImageUrl;
                }
            }

            // Update the project
            _context.Attach(Project).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProjectExists(Project.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostDelete(int? id)
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

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}
