using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioApp.Data.SeedData
{
    public static class HomeContentSeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<IHost>>();
            var context = services.GetRequiredService<ApplicationDbContext>();

            try
            {
                await SeedHomePageContentAsync(context, logger);
                await SeedAboutPageContentAsync(context, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database with home content.");
                throw;
            }
        }

        private static async Task SeedHomePageContentAsync(ApplicationDbContext context, ILogger logger)
        {
            var existingHomeContent = await context.HomeContents
                .FirstOrDefaultAsync(h => h.PageName == HomeContent.PageNames.Home);

            if (existingHomeContent == null)
            {
                var homeContent = new HomeContent
                {
                    PageName = HomeContent.PageNames.Home,
                    Content = "<h1 class=\"display-4 fw-bold mb-3\">Merhaba, Ben <span class=\"text-primary\">[Ad Soyad]</span></h1>" +
                             "<p class=\"lead mb-4\">[Ünvanınız]</p>" +
                             "<p class=\"mb-4\">[Kısa bir tanıtım yazısı]</p>",
                    MetaTitle = "Ana Sayfa",
                    MetaDescription = "Kişisel portföy ve blog sayfam. Yeteneklerim, projelerim ve blog yazılarım hakkında bilgi alabilirsiniz.",
                    LastUpdated = DateTime.UtcNow
                };

                await context.HomeContents.AddAsync(homeContent);
                await context.SaveChangesAsync();
                logger.LogInformation("Home page content seeded successfully.");
            }
        }

        private static async Task SeedAboutPageContentAsync(ApplicationDbContext context, ILogger logger)
        {
            var existingAboutContent = await context.HomeContents
                .FirstOrDefaultAsync(h => h.PageName == HomeContent.PageNames.About);

            if (existingAboutContent == null)
            {
                var aboutContent = new HomeContent
                {
                    PageName = HomeContent.PageNames.About,
                    Content = "<h2 class=\"h3 mb-4\">Merhaba, ben [Ad Soyad]</h2>" +
                             "<p class=\"lead\">[Ünvanınız]</p>" +
                             "<p>[Hakkınızda detaylı bilgiler...]</p>",
                    MetaTitle = "Hakkımda - [Ad Soyad]",
                    MetaDescription = "Benim hakkımda daha fazla bilgi edinin. Yeteneklerim, deneyimlerim ve çalışma şeklim hakkında detaylar.",
                    LastUpdated = DateTime.UtcNow
                };

                await context.HomeContents.AddAsync(aboutContent);
                await context.SaveChangesAsync();
                logger.LogInformation("About page content seeded successfully.");
            }
        }
    }
}
