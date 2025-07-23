using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(
            ILogger<DashboardModel> logger, 
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public int TotalProjects { get; set; }
        public int TotalBlogPosts { get; set; }
        public int PublishedPosts { get; set; }
        public string AdminName { get; set; }
        public DateTime? LastLogin { get; set; }
        
        public List<BlogPost> RecentBlogPosts { get; set; } = new List<BlogPost>();
        public List<Project> RecentProjects { get; set; } = new List<Project>();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToPage("/Identity/Account/Login");
                }

                AdminName = user.UserName;
                LastLogin = user.LastLoginAt;

                TotalProjects = await _context.Projects.CountAsync();
                TotalBlogPosts = await _context.BlogPosts.CountAsync();
                PublishedPosts = await _context.BlogPosts.CountAsync(p => p.IsPublished);
                
                // Get recent blog posts
                RecentBlogPosts = await _context.BlogPosts
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();
                    
                // Get recent projects
                RecentProjects = await _context.Projects
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading the dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again later.";
                return RedirectToPage("/Identity/Account/Login");
            }
        }
    }
}
