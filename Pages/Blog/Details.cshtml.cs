using System;
using System.Collections.Generic;
using System.Linq;
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
                _logger.LogWarning("Empty slug provided to Blog/Details");
                return NotFound();
            }

            try
            {
                _logger.LogInformation("Attempting to load blog post with slug: {Slug}", slug);
                
                // Get the blog post by slug with tracking disabled for better performance
                BlogPost = await _context.BlogPosts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

                if (BlogPost == null)
                {
                    _logger.LogWarning("Blog post with slug {Slug} not found or not published", slug);
                    return NotFound();
                }

                _logger.LogInformation("Successfully loaded blog post with ID: {BlogPostId}", BlogPost.Id);

                try 
                {
                    // Get related posts (posts with similar tags)
                    if (BlogPost.Tags != null && BlogPost.Tags.Count > 0)
                    {
                        _logger.LogInformation("Looking for related posts with matching tags");
                        RelatedPosts = await _context.BlogPosts
                            .AsNoTracking()
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
                        _logger.LogInformation("Found {Count} related posts, looking for more", RelatedPosts.Count);
                        var additionalPosts = await _context.BlogPosts
                            .AsNoTracking()
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading related posts for blog post {BlogPostId}", BlogPost.Id);
                    // Continue execution with empty related posts
                    RelatedPosts = new List<BlogPost>();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading blog post with slug {Slug}", slug);
                // Return a 404 instead of 500 when the post isn't found
                if (ex is InvalidOperationException || ex is NullReferenceException)
                {
                    return NotFound();
                }
                return StatusCode(500, "An error occurred while loading the blog post. Please try again later.");
            }
        }
    }
}
