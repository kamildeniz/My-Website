using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.Pages.Admin.Posts
{
    public class CreateModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private new readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, IWebHostEnvironment env, AuthService authService, ILogger<CreateModel> logger)
            : base(authService, logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [BindProperty]
        public BlogPost BlogPost { get; set; } = new BlogPost();

        public override async Task<IActionResult> OnGetAsync()
        {
            var authResult = await base.OnGetAsync();
            if (authResult is not PageResult)
            {
                return authResult;
            }

            BlogPost.CreatedAt = DateTime.Now;
            BlogPost.Author = User.Identity?.Name ?? "Admin";
            BlogPost.IsPublished = true;
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Set default values
            BlogPost.CreatedAt = DateTime.Now;
            BlogPost.UpdatedAt = DateTime.Now;
            BlogPost.Author = User.Identity?.Name ?? "Admin";

            // Generate slug if empty
            if (string.IsNullOrWhiteSpace(BlogPost.Slug))
            {
                BlogPost.Slug = BlogPost.Title.ToLower()
                    .Replace(" ", "-")
                    .Replace("ş", "s")
                    .Replace("ğ", "g")
                    .Replace("ı", "i")
                    .Replace("ü", "u")
                    .Replace("ö", "o")
                    .Replace("ç", "c");
            }

            // Ensure slug is unique
            if (await _context.BlogPosts.AnyAsync(p => p.Slug == BlogPost.Slug))
            {
                BlogPost.Slug = $"{BlogPost.Slug}-{DateTime.Now:yyyyMMddHHmmss}";
            }

            _context.BlogPosts.Add(BlogPost);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
