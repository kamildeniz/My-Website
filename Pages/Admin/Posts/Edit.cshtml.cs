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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(
            ApplicationDbContext context, 
            ILogger<EditModel> logger,
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
            public int Id { get; set; }

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
            public bool IsPublished { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var blogPost = await _context.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (blogPost == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = blogPost.Id,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Summary = blogPost.Summary,
                Content = blogPost.Content,
                FeaturedImageUrl = blogPost.ImageUrl,
                IsPublished = blogPost.IsPublished
            };

            return Page();
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

            var existingPost = await _context.BlogPosts
                .FirstOrDefaultAsync(p => p.Id == Input.Id);

            if (existingPost == null)
            {
                return NotFound();
            }

            // Update the existing post with the new values
            existingPost.Title = Input.Title;
            existingPost.Slug = string.IsNullOrWhiteSpace(Input.Slug) ? 
                GenerateSlug(Input.Title) : 
                GenerateSlug(Input.Slug);
            existingPost.Summary = Input.Summary;
            existingPost.Content = Input.Content;
            existingPost.ImageUrl = Input.FeaturedImageUrl;
            existingPost.IsPublished = Input.IsPublished;
            existingPost.UpdatedAt = DateTime.UtcNow;

            // Ensure slug is unique
            if (await _context.BlogPosts.AnyAsync(p => p.Id != existingPost.Id && p.Slug == existingPost.Slug))
            {
                existingPost.Slug = $"{existingPost.Slug}-{DateTime.Now:yyyyMMddHHmmss}";
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Blog post updated: {PostId} - {Title} by {UserId}", 
                    existingPost.Id, existingPost.Title, user.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!await _context.BlogPosts.AnyAsync(e => e.Id == Input.Id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Error updating blog post {PostId}", Input.Id);
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

            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost != null)
            {
                _context.BlogPosts.Remove(blogPost);
                await _context.SaveChangesAsync();
            }


            return RedirectToPage("./Index");
        }

        private bool BlogPostExists(int id)
        {
            return _context.BlogPosts.Any(e => e.Id == id);
        }
    }
}
