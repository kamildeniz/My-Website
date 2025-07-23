using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Pages.Admin.Posts
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreateModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(
            ApplicationDbContext context, 
            ILogger<CreateModel> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(200, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 5)]
            public string Title { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
            public string Slug { get; set; }

            [Required]
            [StringLength(1000, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 10)]
            public string Summary { get; set; }

            [Required]
            public string Content { get; set; }

            [Display(Name = "Featured Image URL")]
            [Url]
            public string FeaturedImageUrl { get; set; }

            [Display(Name = "Is Published")]
            public bool IsPublished { get; set; } = true;
        }

        private string GenerateSlug(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;
                
            return title.ToLower()
                .Replace(" ", "-")
                .Replace("ş", "s")
                .Replace("ğ", "g")
                .Replace("ı", "i")
                .Replace("ü", "u")
                .Replace("ö", "o")
                .Replace("ç", "c");
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

            var post = new BlogPost
            {
                Title = Input.Title,
                Summary = Input.Summary,
                Content = Input.Content,
                ImageUrl = Input.FeaturedImageUrl,
                IsPublished = Input.IsPublished,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Slug = GenerateSlug(Input.Title ?? string.Empty),
                Author = user.UserName ?? "Admin"
            };

            // Generate slug if empty
            if (string.IsNullOrWhiteSpace(post.Slug))
            {
                post.Slug = GenerateSlug(post.Title);
            }

            // Ensure slug is unique
            if (await _context.BlogPosts.AnyAsync(p => p.Slug == post.Slug))
            {
                post.Slug = $"{post.Slug}-{DateTime.Now:yyyyMMddHHmmss}";
            }

            _context.BlogPosts.Add(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New blog post created: {PostId} - {Title} by {UserId}", 
                post.Id, post.Title, user.Id);
                
            return RedirectToPage("./Index");
        }
    }
}
