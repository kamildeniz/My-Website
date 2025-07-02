using Microsoft.EntityFrameworkCore;
using PortfolioApp.Models;

namespace PortfolioApp.Data.Seeders
{
    public static class SeedData
    {
        public static void EnsureAdmin(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            if (!context.AdminUsers.Any())
            {
                var email = "admin@example.com";
                var password = "Admin123!";
                var salt = PasswordHelper.GenerateSalt();
                var hash = PasswordHelper.HashPassword(password, salt);

                context.AdminUsers.Add(new AdminUser
                {
                    Email = email,
                    PasswordSalt = salt,
                    PasswordHash = hash
                });
                context.SaveChanges();
            }
        }
    }
}
