using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Pages
{
    public class ContactModel : PageModel
    {
        private readonly ILogger<ContactModel> _logger;

        public ContactModel(ILogger<ContactModel> logger)
        {
            _logger = logger;
        }

        [BindProperty]
        public ContactFormModel ContactForm { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Here you would typically send an email or save to a database
                // For now, we'll just log it
                _logger.LogInformation("Contact form submitted: {Name}, {Email}, {Subject}", 
                    ContactForm.Name, ContactForm.Email, ContactForm.Subject);

                TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi. En kısa sürede size dönüş yapacağım.";
                return RedirectToPage("Contact");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending contact form");
                ModelState.AddModelError(string.Empty, "Bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                return Page();
            }
        }
    }

    public class ContactFormModel
    {
        [Required(ErrorMessage = "Adınızı giriniz")]
        [Display(Name = "Adınız")]
        [StringLength(100, ErrorMessage = "Adınız en fazla 100 karakter olabilir")]
        public string Name { get; set; }

        [Required(ErrorMessage = "E-posta adresinizi giriniz")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
        [Display(Name = "E-posta Adresiniz")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Lütfen bir konu giriniz")]
        [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir")]
        [Display(Name = "Konu")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Lütfen bir mesaj yazınız")]
        [Display(Name = "Mesajınız")]
        [StringLength(5000, ErrorMessage = "Mesajınız en fazla 5000 karakter olabilir")]
        public string Message { get; set; }
    }
}
