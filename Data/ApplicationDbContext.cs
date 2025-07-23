using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PortfolioApp.Models;

namespace PortfolioApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Old AdminUsers table will be removed after migration
    // public DbSet<AdminUser> AdminUsers { get; set; }
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<BlogPost> BlogPosts { get; set; } = null!;
    public DbSet<Profile> Profiles { get; set; } = null!;
    public DbSet<HomeContent> HomeContents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Identity tables
        modelBuilder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.Id).HasMaxLength(255);
            b.Property(u => u.NormalizedUserName).HasMaxLength(255);
            b.Property(u => u.NormalizedEmail).HasMaxLength(255);
        });

        modelBuilder.Entity<IdentityUserLogin<string>>(b =>
        {
            b.Property(l => l.LoginProvider).HasMaxLength(255);
            b.Property(l => l.ProviderKey).HasMaxLength(255);
        });

        modelBuilder.Entity<IdentityUserToken<string>>(b =>
        {
            b.Property(t => t.LoginProvider).HasMaxLength(255);
            b.Property(t => t.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<IdentityRole>(b =>
        {
            b.Property(r => r.Name).HasMaxLength(255);
            b.Property(r => r.NormalizedName).HasMaxLength(255);
        });

        modelBuilder.Entity<IdentityUserClaim<string>>(b =>
        {
            b.Property(uc => uc.ClaimType).HasMaxLength(255);
            b.Property(uc => uc.ClaimValue).HasMaxLength(255);
        });

        modelBuilder.Entity<IdentityUserRole<string>>().HasKey(ur => new { ur.UserId, ur.RoleId });
        modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(l => new { l.LoginProvider, l.ProviderKey });
        modelBuilder.Entity<IdentityUserToken<string>>().HasKey(t => new { t.UserId, t.LoginProvider, t.Name });
        
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
            
        // Configure HomeContent entity
        modelBuilder.Entity<HomeContent>()
            .HasIndex(h => h.PageName)
            .IsUnique();
            
        // Configure default values for HomeContent
        modelBuilder.Entity<HomeContent>()
            .Property(h => h.LastUpdated)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
        // Configure string lengths and required fields
        modelBuilder.Entity<HomeContent>()
            .Property(h => h.PageName)
            .IsRequired()
            .HasMaxLength(100);
            
        modelBuilder.Entity<HomeContent>()
            .Property(h => h.MetaTitle)
            .HasMaxLength(100);
            
        modelBuilder.Entity<HomeContent>()
            .Property(h => h.MetaDescription)
            .HasMaxLength(200);
            
        // Configure image URL length
        modelBuilder.Entity<HomeContent>()
            .Property(h => h.ImageUrl)
            .HasMaxLength(500);
            
        // Create unique index for BlogPost Slug
        modelBuilder.Entity<BlogPost>()
            .HasIndex(b => b.Slug)
            .IsUnique();
    }
}
