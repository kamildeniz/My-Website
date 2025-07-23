using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Pages.Admin.Posts
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            ApplicationDbContext context, 
            ILogger<IndexModel> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public IList<BlogPost> BlogPosts { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var postsQuery = from p in _context.BlogPosts
                             select p;

            if (!string.IsNullOrEmpty(SearchString))
            {
                postsQuery = postsQuery.Where(s => s.Title.Contains(SearchString) 
                                               || s.Excerpt.Contains(SearchString));
            }

            BlogPosts = await postsQuery.OrderByDescending(p => p.CreatedAt).ToListAsync();
                
            _logger.LogInformation("User {UserId} viewed the blog posts list", user.Id);
            return Page();
        }
    }
}
