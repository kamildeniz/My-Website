using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;

namespace PortfolioApp.Pages.Blog
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly BlogService _blogService;
        private readonly ApplicationDbContext _context;
        private const int PageSize = 5; // Number of posts per page

        public IndexModel(ILogger<IndexModel> logger, BlogService blogService, ApplicationDbContext context)
        {
            _logger = logger;
            _blogService = blogService;
            _context = context;
        }

        public List<BlogPost> BlogPosts { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalPosts { get; set; }

        public async Task<IActionResult> OnGetAsync(int page = 1)
        {
            try
            {
                // Get all published posts ordered by date (newest first)
                var query = _context.BlogPosts
                    .Where(p => p.IsPublished)
                    .OrderByDescending(p => p.CreatedAt);

                // Get total count for pagination
                TotalPosts = await query.CountAsync();
                TotalPages = (int)Math.Ceiling(TotalPosts / (double)PageSize);

                // Validate page number
                CurrentPage = page < 1 ? 1 : (page > TotalPages && TotalPages > 0 ? TotalPages : page);

                // Get posts for current page
                BlogPosts = await query
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Process markdown content for preview
                foreach (var post in BlogPosts)
                {
                    if (!string.IsNullOrEmpty(post.Content))
                    {
                        // Truncate content for preview
                        post.Summary = post.Summary ?? TruncateText(post.Content, 200);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading blog posts");
                // In a production app, you might want to show an error message to the user
            }

            return Page();
        }

        // Helper method to calculate read time
        public string CalculateReadTime(string content)
        {
            if (string.IsNullOrEmpty(content)) return "1";
            
            // Average reading speed: 200 words per minute
            const int wordsPerMinute = 200;
            var wordCount = content.Split(new[] { ' ', '\t', '\n', '\r' }, 
                StringSplitOptions.RemoveEmptyEntries).Length;
            var minutes = Math.Max(1, (int)Math.Ceiling(wordCount / (double)wordsPerMinute));
            
            return minutes.ToString();
        }

        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            
            // Remove markdown formatting for preview
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[#*_~`\[\]()\-+.!?\n]", " ");
            
            // Truncate to max length
            if (text.Length <= maxLength) return text;
            
            // Find the last space before max length
            var lastSpace = text.LastIndexOf(' ', maxLength);
            var length = lastSpace > 0 ? lastSpace : maxLength;
            
            return text[..length] + "...";
        }
    }
}
