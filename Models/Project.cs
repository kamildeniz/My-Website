using System.ComponentModel.DataAnnotations;

namespace PortfolioApp.Models;

public class Project
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public string? ImageUrl { get; set; }
    
    public string? GitHubUrl { get; set; }
    
    public string? LiveUrl { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<string>? Technologies { get; set; }
}
