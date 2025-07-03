using Microsoft.EntityFrameworkCore;
using PortfolioApp.Models;
using Microsoft.Extensions.Logging;
using PortfolioApp.Services;
using System;
using System.Linq;

namespace PortfolioApp.Data.Seeders
{
    public static class SeedData
    {
        public static void EnsureAdmin(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");
            logger.LogInformation("--- [SeedData] Checking for admin user ---");

            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            if (!context.AdminUsers.Any())
            {
                logger.LogWarning("!!! [SeedData] No admin user found. Creating a new one. !!!");
                var email = "admin@example.com";
                var password = "Admin123!";
                var (hash, salt) = AuthService.HashPassword(password);

                logger.LogInformation("[SeedData] New Admin User Details -> Email: {email}, Password: {password}, Salt: {salt}, Hash: {hash}", email, password, salt, hash);

                context.AdminUsers.Add(new AdminUser
                {
                    Email = email,
                    PasswordSalt = salt,
                    PasswordHash = hash
                });
                context.SaveChanges();
                logger.LogInformation("--- [SeedData] New admin user successfully saved to the database. ---");
            }
            else
            {
                logger.LogInformation("--- [SeedData] Admin user already exists. Seeding skipped. ---");
            }
        }
    }
}
