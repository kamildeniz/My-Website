using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortfolioApp.Pages.Admin.HomeContent
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<PortfolioApp.Models.HomeContent> HomeContents { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            HomeContents = await _context.HomeContents.ToListAsync();
            return Page();
        }
    }
}
