using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioApp.Pages.Admin
{
    public class DashboardModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private new readonly ILogger<DashboardModel> _logger;

        public int TotalBlogPosts { get; set; }
        public int TotalProjects { get; set; }
        public int PublishedPosts { get; set; }
        public int TotalComments { get; set; }

        public IList<BlogPost> RecentBlogPosts { get; set; } = new List<BlogPost>();
        public IList<Project> RecentProjects { get; set; } = new List<Project>();

        public DashboardModel(ApplicationDbContext context, AuthService authService, ILogger<DashboardModel> logger)
            : base(authService, logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            TotalBlogPosts = await _context.BlogPosts.CountAsync();
            TotalProjects = await _context.Projects.CountAsync();
            PublishedPosts = await _context.BlogPosts.CountAsync(p => p.IsPublished);
            TotalComments = 0; // You can implement comments later

            RecentBlogPosts = await _context.BlogPosts
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            RecentProjects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
    }
}
