using System;
using System.Collections.Generic;
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

namespace PortfolioApp.Pages.Admin.Projects
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

        public IList<Project> Projects { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var projectsQuery = _context.Projects.AsQueryable();

            if (!string.IsNullOrEmpty(SearchString))
            {
                projectsQuery = projectsQuery.Where(p => 
                    p.Title.Contains(SearchString) || 
                    p.Description.Contains(SearchString));
            }

            Projects = await projectsQuery
                .OrderByDescending(p => p.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            _logger.LogInformation("User {UserId} viewed the projects list", user.Id);
            return Page();
        }
    }
}
