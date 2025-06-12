using Microsoft.EntityFrameworkCore;
using PortfolioApp.Models;

namespace PortfolioApp.Data.Seeders;

public static class DatabaseSeeder
{
    public static void Initialize(IServiceProvider serviceProvider)
    {
        using (var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
        {
            // Clear existing projects
            context.Projects.RemoveRange(context.Projects);
            context.SaveChanges();

            // Add sample projects
            var projects = new List<Project>
            {
                new Project
                {
                    Title = "E-Ticaret API",
                    Description = "Modern bir e-ticaret platformu için geliştirilmiş RESTful API. Ürün yönetimi, kullanıcı işlemleri ve sipariş takibi gibi temel e-ticaret işlevlerini içerir.",
                    ImageUrl = "/images/eticaret-api.jpg",
                    GitHubUrl = "https://github.com/kamildeniz/ETicaretAPI",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    Technologies = new List<string> { "C#", ".NET Core", "Entity Framework Core", "SQL Server", "REST API" }
                },
                new Project
                {
                    Title = "Kütüphane Yönetim Sistemi",
                    Description = "Kütüphaneler için geliştirilmiş kapsamlı bir yönetim sistemi. Kitap takibi, üye yönetimi ve ödünç alma işlemleri gibi temel kütüphane işlemlerini yönetir.",
                    ImageUrl = "/images/library-management.jpg",
                    GitHubUrl = "https://github.com/kamildeniz/LibraryManagementAPI",
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    Technologies = new List<string> { "C#", "ASP.NET Core", "Entity Framework", "SQL Server", "Web API" }
                },
                new Project
                {
                    Title = "Katmanlı Mimari Demo",
                    Description = "N-Katmanlı mimari prensiplerini gösteren örnek bir uygulama. Temel CRUD işlemlerini içeren ve farklı katmanlar arasındaki etkileşimi gösteren bir demo projesidir.",
                    ImageUrl = "/images/nlayered-demo.jpg",
                    GitHubUrl = "https://github.com/kamildeniz/NLayeredAppDemo",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    Technologies = new List<string> { "C#", ".NET Core", "Entity Framework", "Dependency Injection", "Repository Pattern" }
                },
                new Project
                {
                    Title = "N-Katmanlı Mimarili Uygulama",
                    Description = "Udemy eğitimi kapsamında geliştirilmiş, N-Katmanlı mimari prensiplerine uygun olarak geliştirilmiş örnek bir uygulama.",
                    ImageUrl = "/images/nlayer-udemy.jpg",
                    GitHubUrl = "https://github.com/kamildeniz/NLayerUdemyApp",
                    CreatedAt = DateTime.UtcNow.AddDays(-75),
                    Technologies = new List<string> { "C#", ".NET Core", "Entity Framework Core", "Clean Architecture", "Docker" }
                },
                new Project
                {
                    Title = "Kişisel Web Sitesi",
                    Description = "ASP.NET Core 8 ve Razor Pages kullanılarak geliştirilmiş kişisel portföy web sitesi. Modern ve duyarlı bir arayüz ile kullanıcı deneyimini ön planda tutan bir projedir.",
                    ImageUrl = "/images/personal-website.jpg",
                    GitHubUrl = "https://github.com/kamildeniz/My-Website",
                    LiveUrl = "https://www.kamildeniz.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    Technologies = new List<string> { "C#", "ASP.NET Core 8", "Razor Pages", "Bootstrap 5", "SQLite" }
                }
            };

            context.Projects.AddRange(projects);
            context.SaveChanges();
        }
    }
}
