using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public EditModel(
            ApplicationDbContext context,
            ILogger<EditModel> logger,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _environment = environment;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public int Id { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            public string Title { get; set; } = string.Empty;

            [StringLength(500, ErrorMessage = "The {0} must be at max {1} characters long.")]
            public string? Description { get; set; }

            [Display(Name = "GitHub URL")]
            [Url]
            public string? GitHubUrl { get; set; }

            [Display(Name = "Live URL")]
            [Url]
            public string? LiveUrl { get; set; }

            [Display(Name = "Image URL")]
            [Url]
            public string? ImageUrl { get; set; }

            [Display(Name = "Project Image")]
            public IFormFile? ImageFile { get; set; }

            [Display(Name = "Technologies (comma-separated)")]
            public string? Technologies { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = project.Id,
                Title = project.Title,
                Description = project.Description,
                GitHubUrl = project.GitHubUrl,
                LiveUrl = project.LiveUrl,
                Technologies = project.Technologies != null ? string.Join(", ", project.Technologies) : string.Empty,
                ImageUrl = project.ImageUrl
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == Input.Id);
            if (project == null)
            {
                return NotFound();
            }

            // Handle file upload first to get the new image URL if a file was uploaded
            if (Input.ImageFile != null && Input.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "projects");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Input.ImageFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ImageFile.CopyToAsync(fileStream);
                }

                // Delete old image if it exists and is not the default
                if (!string.IsNullOrEmpty(project.ImageUrl) && !project.ImageUrl.Contains("default-project.png"))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, project.ImageUrl.TrimStart('/').Replace("/", "\\"));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Update the image URL to the new file
                Input.ImageUrl = $"/uploads/projects/{uniqueFileName}";
            }

            // Update project properties
            project.Title = Input.Title;
            project.Description = Input.Description;
            project.GitHubUrl = Input.GitHubUrl;
            project.LiveUrl = Input.LiveUrl;
            project.Technologies = Input.Technologies?.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList() ?? new List<string>();

            // Update image URL if a new one was uploaded
            if (!string.IsNullOrEmpty(Input.ImageUrl))
            {
                project.ImageUrl = Input.ImageUrl;
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Project updated: {ProjectId} - {Title} by {UserId}", 
                    project.Id, project.Title, user.Id);
                
                TempData["SuccessMessage"] = "Project updated successfully.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await _context.Projects.AnyAsync(e => e.Id == Input.Id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Error updating project {ProjectId}", Input.Id);
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the project. Please try again.");
                    return Page();
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
