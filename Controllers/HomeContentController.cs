using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace PortfolioApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/home-content")]
    public class HomeContentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<HomeContentController> _logger;
        private const string UploadsFolder = "uploads";

        public HomeContentController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<HomeContentController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var homeContents = await _context.HomeContents
                    .OrderBy(h => h.PageName)
                    .ToListAsync();
                
                // Varsayılan sayfaları kontrol et
                var defaultPages = new[] { PageNames.Home, PageNames.About, PageNames.Contact };
                var existingPageNames = homeContents.Select(h => h.PageName.ToLower()).ToList();
                
                // Eksik sayfaları ekle
                foreach (var pageName in defaultPages)
                {
                    if (!existingPageNames.Contains(pageName.ToLower()))
                    {
                        _context.HomeContents.Add(new HomeContent
                        {
                            PageName = pageName,
                            Title = pageName == PageNames.Home ? "Hoş Geldiniz" : 
                                    pageName == PageNames.About ? "Hakkımda" : "İletişim",
                            Content = pageName == PageNames.Home ? "<p>Web siteme hoş geldiniz. Bu alanı yönetim panelinden düzenleyebilirsiniz.</p>" :
                                     pageName == PageNames.About ? "<p>Hakkımda sayfası içeriği...</p>" : "<p>İletişim bilgileriniz burada yer alacak.</p>",
                            ButtonText = pageName == PageNames.Home ? "Hakkımda" : "İletişime Geçin",
                            ButtonUrl = pageName == PageNames.Home ? "/about" : "/contact"
                        });
                    }
                }
                
                await _context.SaveChangesAsync();
                
                // Tüm sayfaları tekrar çek
                homeContents = await _context.HomeContents
                    .OrderBy(h => h.PageName)
                    .ToListAsync();
                
                return View(homeContents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while loading home contents");
                TempData["ErrorMessage"] = "İçerikler yüklenirken bir hata oluştu.";
                return View(new List<HomeContent>());
            }
        }

        [HttpGet("edit/{pageName}")]
        public async Task<IActionResult> Edit(string pageName)
        {
            var content = await _context.HomeContents.FirstOrDefaultAsync(h => h.PageName == pageName);
            
            if (content == null)
            {
                content = new HomeContent { PageName = pageName };
                _context.HomeContents.Add(content);
                await _context.SaveChangesAsync();
            }

            return View(content);
        }

        [HttpPost("edit/{pageName}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string pageName, HomeContent model)
        {
            if (ModelState.IsValid)
            {
                var content = await _context.HomeContents.FirstOrDefaultAsync(h => h.PageName == pageName);
                
                if (content == null)
                {
                    return NotFound();
                }

                // Handle file upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(content.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, content.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Create uploads directory if it doesn't exist
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, UploadsFolder);
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    var fileExtension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    content.ImageUrl = $"/{UploadsFolder}/{uniqueFileName}";
                }
                else if (!string.IsNullOrEmpty(content.ImageUrl))
                {
                    // Keep the existing image if no new file is uploaded
                    model.ImageUrl = content.ImageUrl;
                }

                content.Content = model.Content;
                content.MetaTitle = model.MetaTitle;
                content.MetaDescription = model.MetaDescription;
                content.LastUpdated = DateTime.UtcNow;

                _context.Update(content);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{pageName} sayfası başarıyla güncellendi.";
                return RedirectToAction("Index");
            }

            return View(model);
        }
    }
}
