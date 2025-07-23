using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PortfolioApp.Pages.Admin.HomeContent
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EditModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public PortfolioApp.Models.HomeContent HomeContent { get; set; } = default!;

        [BindProperty]
        [Display(Name = "Resim Dosyası")]
        [FileExtensions(Extensions = "jpg,jpeg,png,gif")]
        public IFormFile? ImageFile { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var homecontent = await _context.HomeContents.FirstOrDefaultAsync(m => m.Id == id);
            if (homecontent == null)
            {
                return NotFound();
            }
            HomeContent = homecontent;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingContent = await _context.HomeContents.FindAsync(HomeContent.Id);
            if (existingContent == null)
            {
                return NotFound();
            }

            // Handle file upload
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingContent.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_environment.WebRootPath, existingContent.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{ImageFile.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }

                existingContent.ImageUrl = $"/uploads/{uniqueFileName}";
            }

            // Update other properties
            existingContent.Title = HomeContent.Title;
            existingContent.Content = HomeContent.Content;
            existingContent.ButtonText = HomeContent.ButtonText;
            existingContent.ButtonUrl = HomeContent.ButtonUrl;
            existingContent.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "İçerik başarıyla güncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HomeContentExists(HomeContent.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool HomeContentExists(int id)
        {
            return _context.HomeContents.Any(e => e.Id == id);
        }
    }
}
