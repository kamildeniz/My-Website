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

        public IndexModel(ApplicationDbContext context, AuthService authService, ILogger<IndexModel> logger) 
            : base(authService, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            BlogPosts = await _context.BlogPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
