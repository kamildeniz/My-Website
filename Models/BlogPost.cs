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
    
    public string Excerpt { get; set; } = string.Empty;
    
    public string Author { get; set; } = "Admin";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsPublished { get; set; } = true;
    
    public List<string>? Tags { get; set; }
    
    public string GetTagsAsString() => Tags != null ? string.Join(", ", Tags) : string.Empty;
    
    public void SetTagsFromString(string tagsString)
    {
        if (string.IsNullOrWhiteSpace(tagsString))
        {
            Tags = null;
            return;
        }
        
        Tags = tagsString.Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
    }
}
