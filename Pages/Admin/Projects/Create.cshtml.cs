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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public CreateModel(
            ApplicationDbContext context, 
            ILogger<CreateModel> logger,
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
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            public string Title { get; set; }

            [StringLength(500, ErrorMessage = "The {0} must be at max {1} characters long.")]
            public string Description { get; set; }

            [Display(Name = "GitHub URL")]
            [Url]
            public string GitHubUrl { get; set; }

            [Display(Name = "Live Demo URL")]
            [Url]
            public string LiveUrl { get; set; }

            [Display(Name = "Technologies")]
            public string Technologies { get; set; }

            [Display(Name = "Image File")]
            public IFormFile ImageFile { get; set; }
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var project = new Project
            {
                Title = Input.Title,
                Description = Input.Description,
                GitHubUrl = Input.GitHubUrl,
                LiveUrl = Input.LiveUrl,
                CreatedAt = DateTime.UtcNow
            };

            // Handle technologies (convert comma-separated string to List<string>)
            if (!string.IsNullOrWhiteSpace(Input.Technologies))
            {
                project.Technologies = Input.Technologies
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            // Handle image upload
            if (Input.ImageFile != null && Input.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "projects");
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}_{Input.ImageFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ImageFile.CopyToAsync(fileStream);
                }

                // Set the image URL
                project.ImageUrl = $"/images/projects/{uniqueFileName}";
            }
            else
            {
                // Set a default image if none is provided
                project.ImageUrl = "/images/projects/default-project.jpg";
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New project created: {ProjectId} - {Title} by {UserId}", 
                project.Id, project.Title, user.Id);

            return RedirectToPage("./Index");
        }
    }
}
