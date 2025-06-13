using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.Pages.Blog
{
    public class DetailsModel : PageModel
    {
        private readonly ILogger<DetailsModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly BlogService _blogService;

        public DetailsModel(
            ILogger<DetailsModel> logger, 
            ApplicationDbContext context,
            BlogService blogService)
        {
            _logger = logger;
            _context = context;
            _blogService = blogService;
        }

        public BlogPost? BlogPost { get; set; }
        public List<BlogPost> RelatedPosts { get; set; } = new();
        public string? HtmlContent { get; set; }

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            try
            {
                // Get the blog post by slug
                BlogPost = await _context.BlogPosts
                    .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

                if (BlogPost == null)
                {
                    _logger.LogWarning("Blog post with slug {Slug} not found", slug);
                    return NotFound();
                }

                // Get related posts (posts with similar tags)
                if (BlogPost.Tags != null && BlogPost.Tags.Count > 0)
                {
                    // Find posts that share at least one tag with the current post
                    RelatedPosts = await _context.BlogPosts
                        .Where(p => p.Id != BlogPost.Id && 
                                  p.IsPublished &&
                                  p.Tags != null && 
                                  p.Tags.Any(t => BlogPost.Tags.Contains(t)))
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(3)
                        .ToListAsync();
                }

                // If not enough related posts, get the most recent ones
                if (RelatedPosts.Count < 3)
                {
                    var additionalPosts = await _context.BlogPosts
                        .Where(p => p.Id != BlogPost.Id && p.IsPublished)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(3 - RelatedPosts.Count)
                        .ToListAsync();

                    // Add only the ones that aren't already in the list
                    RelatedPosts = RelatedPosts
                        .Union(additionalPosts.Where(p => !RelatedPosts.Any(rp => rp.Id == p.Id)))
                        .Take(3)
                        .ToList();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading blog post with slug {Slug}", slug);
                // In a production app, you might want to show an error page
                return StatusCode(500);
            }
        }
    }
}
