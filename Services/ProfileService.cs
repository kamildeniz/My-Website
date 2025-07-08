using Microsoft.EntityFrameworkCore;
using PortfolioApp.Data;
using PortfolioApp.Models;

namespace PortfolioApp.Services
{
    public class ProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(ApplicationDbContext context, ILogger<ProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Profile?> GetProfileAsync()
        {
            try
            {
                var profile = await _context.Profiles.FirstOrDefaultAsync();
                if (profile == null)
                {
                    // Eğer profil yoksa varsayılan bir profil oluştur
                    profile = new Profile
                    {
                        FullName = "Kamil Deniz",
                        Title = "Junior Full Stack Developer",
                        Description = "Yazılım geliştirme dünyasına olan tutkum ve sürekli öğrenme isteğimle kendimi bu alanda geliştirmeye devam ediyorum.",
                        AboutMe = "Merhaba, ben Kamil Deniz. Yazılım geliştirme konusunda tutkulu bir geliştiriciyim. Yeni teknolojiler öğrenmeyi ve projeler geliştirmeyi seviyorum.",
                        ProfileImageUrl = "/images/profile-placeholder.jpg"
                    };
                    
                    await _context.Profiles.AddAsync(profile);
                    await _context.SaveChangesAsync();
                }
                
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil bilgileri alınırken bir hata oluştu");
                return null;
            }
        }

        public async Task<bool> UpdateProfileAsync(Profile profile)
        {
            try
            {
                profile.LastUpdated = DateTime.UtcNow;
                _context.Profiles.Update(profile);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profil güncellenirken bir hata oluştu");
                return false;
            }
        }
    }
}
