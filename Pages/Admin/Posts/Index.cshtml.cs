using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PortfolioApp.Services;

namespace PortfolioApp.Pages.Admin.Posts
{
    public class IndexModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private new readonly ILogger<IndexModel> _logger;

                public IList<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public IndexModel(ApplicationDbContext context, AuthService authService, ILogger<IndexModel> logger) 
            : base(authService, logger)
        {
            _context = context;
            _logger = logger;
        }

                public async Task OnGetAsync()
        {
            var postsQuery = from p in _context.BlogPosts
                             select p;

            if (!string.IsNullOrEmpty(SearchString))
            {
                postsQuery = postsQuery.Where(s => s.Title.Contains(SearchString) 
                                               || s.Excerpt.Contains(SearchString));
            }

            BlogPosts = await postsQuery.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }
    }
}
