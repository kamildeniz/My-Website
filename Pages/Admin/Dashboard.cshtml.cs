using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Services;
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
        }
    }
}
