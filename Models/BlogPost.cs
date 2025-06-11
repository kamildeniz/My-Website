using System.ComponentModel.DataAnnotations;

namespace PortfolioApp.Models;

public class BlogPost
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Summary { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? ImageUrl { get; set; }
    
    [Required]
    public string Slug { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsPublished { get; set; } = true;
    
    public List<string>? Tags { get; set; }
}
