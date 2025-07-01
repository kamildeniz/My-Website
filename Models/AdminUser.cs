using System.ComponentModel.DataAnnotations;

namespace PortfolioApp.Models;
public class AdminUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string PasswordSalt { get; set; } = string.Empty;
}
