using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Pages.Admin.Posts
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeleteModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public DeleteModel(
            ApplicationDbContext context, 
            ILogger<DeleteModel> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        [BindProperty]
        public BlogPost BlogPost { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            BlogPost = await _context.BlogPosts
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (BlogPost == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            BlogPost = await _context.BlogPosts.FindAsync(id);

            if (BlogPost != null)
            {
                _context.BlogPosts.Remove(BlogPost);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Blog post deleted: {PostId} - {Title} by {UserId}", 
                    BlogPost.Id, BlogPost.Title, user.Id);
            }

            return RedirectToPage("./Index");
        }
    }
}
