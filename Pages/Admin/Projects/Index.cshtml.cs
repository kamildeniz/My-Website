using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using PortfolioApp.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioApp.Pages.Admin.Projects
{
    public class IndexModel : AdminPageModel
    {
        private readonly ApplicationDbContext _context;
        private new readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, AuthService authService, ILogger<IndexModel> logger)
            : base(authService, logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<Project> Projects { get; set; } = new List<Project>();

        public async Task OnGetAsync()
        {
            Projects = await _context.Projects
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }
    }
}
