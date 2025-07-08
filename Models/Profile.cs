using System.ComponentModel.DataAnnotations;

namespace PortfolioApp.Models
{
    public class Profile
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public string? ProfileImageUrl { get; set; }
        
        public string? AboutMe { get; set; }
        
        // Sosyal Medya Bağlantıları
        public string? GithubUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? InstagramUrl { get; set; }
        
        public DateTime? LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
