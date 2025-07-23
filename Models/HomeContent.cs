using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel;
using PortfolioApp.Attributes;

namespace PortfolioApp.Models
{
    public static class PageNames
    {
        public const string Home = "home";
        public const string About = "about";
        public const string Contact = "contact";
    }

    public class HomeContent
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Sayfa adı zorunludur.")]
        [StringLength(200, ErrorMessage = "Sayfa adı en fazla 200 karakter olabilir.")]
        [Display(Name = "Sayfa Adı")]
        public string PageName { get; set; } = string.Empty;
        
        [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir.")]
        [Display(Name = "Başlık")]
        public string? Title { get; set; }
        
        [Required(ErrorMessage = "İçerik zorunludur.")]
        [Display(Name = "İçerik")]
        public string Content { get; set; } = string.Empty;
        
        [Display(Name = "Resim URL")]
        public string? ImageUrl { get; set; }
        
        [StringLength(100, ErrorMessage = "Meta başlık en fazla 100 karakter olabilir.")]
        [Display(Name = "Meta Başlık")]
        public string? MetaTitle { get; set; }
        
        [StringLength(200, ErrorMessage = "Meta açıklaması en fazla 200 karakter olabilir.")]
        [Display(Name = "Meta Açıklama")]
        public string? MetaDescription { get; set; }
        
        [NotMapped]
        [Display(Name = "Resim Yükle")]
        [DataType(DataType.Upload)]
        [PortfolioApp.Attributes.FileExtensions("jpg,jpeg,png,gif", ErrorMessage = "Sadece JPG, JPEG, PNG veya GIF formatında resim yükleyebilirsiniz.")]
        [MaxFileSize(5 * 1024 * 1024, ErrorMessage = "Resim boyutu en fazla 5MB olabilir.")]
        public IFormFile? ImageFile { get; set; }
        
        [Display(Name = "Buton Metni")]
        [StringLength(50, ErrorMessage = "Buton metni en fazla 50 karakter olabilir.")]
        public string? ButtonText { get; set; }
        
        [Display(Name = "Buton Linki")]
        [StringLength(500, ErrorMessage = "Buton linki en fazla 500 karakter olabilir.")]
        public string? ButtonUrl { get; set; }
        
        [Display(Name = "Son Güncelleme")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Güncellenme Tarihi")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Sayfa adı için sabit değerler
        public static class PageNames
        {
            public const string Home = "Home";
            public const string About = "About";
            
            public static IEnumerable<string> GetAll()
            {
                return new[] { Home, About };
            }
        }
    }
}
