using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PortfolioApp.Models;

namespace PortfolioApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<BlogPost> BlogPosts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure Project entity
        modelBuilder.Entity<Project>()
            .Property(p => p.Technologies)
            .HasConversion(
                v => string.Join(';', v ?? new List<string>()),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );
            
        // Configure BlogPost entity
        modelBuilder.Entity<BlogPost>()
            .Property(p => p.Tags)
            .HasConversion(
                v => string.Join(';', v ?? new List<string>()),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );
            
        // Create unique index for BlogPost Slug
        modelBuilder.Entity<BlogPost>()
            .HasIndex(b => b.Slug)
            .IsUnique();
    }
}
