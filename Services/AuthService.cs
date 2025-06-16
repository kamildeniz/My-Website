using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PortfolioApp.Services
{
    public class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _adminEmail;
        private string _adminPasswordHash;
        private string _adminSalt;
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 100000; // PBKDF2 iteration count

        public AuthService(
            IHttpContextAccessor httpContextAccessor, 
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Yapılandırmadan kimlik bilgilerini al
            _adminEmail = _configuration["AdminCredentials:Email"] ?? "admin@example.com";
            _adminPasswordHash = _configuration["AdminCredentials:PasswordHash"];
            _adminSalt = _configuration["AdminCredentials:PasswordSalt"];
            
            _logger.LogInformation("AuthService initialized");
        }

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe = false)
        {
            _logger.LogInformation("=== AUTH SERVICE LOGIN START ===");
            _logger.LogInformation($"Login attempt for email: {email}");
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Email or password is null or empty");
                return false;
            }

            try
            {
                // E-posta karşılaştırmasında büyük/küçük harf duyarsız olarak karşılaştır
                if (!string.Equals(email, _adminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Login failed - User not found: {email}");
                    return false;
                }

                // Eğer yapılandırmada hash yoksa, varsayılan şifre için kontrol yap
                if (string.IsNullOrEmpty(_adminPasswordHash) || string.IsNullOrEmpty(_adminSalt))
                {
                    // Varsayılan şifre kontrolü (sadece geliştirme ortamında)
                    if (password == "Admin123!" && _configuration.GetValue<bool>("IsDevelopment", true))
                    {
                        _logger.LogInformation("Using default password for development environment");
                        // Geliştirme ortamında şifreyi kaydet
                        var (hash, salt) = HashPassword("Admin123!");
                        
                        // appsettings.json'ı güncelle
                        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                        var appSettingsJson = System.Text.Json.JsonSerializer.Serialize(
                            new {
                                ConnectionStrings = _configuration.GetSection("ConnectionStrings").Get<object>(),
                                Logging = _configuration.GetSection("Logging").Get<object>(),
                                AdminCredentials = new 
                                {
                                    Email = _configuration["AdminCredentials:Email"],
                                    PasswordHash = hash,
                                    PasswordSalt = salt
                                },
                                Security = _configuration.GetSection("Security").Get<object>(),
                                Cors = _configuration.GetSection("Cors").Get<object>(),
                                AllowedHosts = _configuration["AllowedHosts"],
                                IsDevelopment = true
                            }, 
                            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        
                        await File.WriteAllTextAsync(appSettingsPath, appSettingsJson);
                        
                        _adminPasswordHash = hash;
                        _adminSalt = salt;
                    }
                    else
                    {
                        _logger.LogWarning("Login failed - Invalid password or not in development mode");
                        return false;
                    }
                }
                else
                {
                    // Şifre doğrulaması
                    bool isPasswordValid = VerifyPassword(password, _adminPasswordHash, _adminSalt);
                    
                    if (!isPasswordValid)
                    {
                        _logger.LogWarning($"Login failed - Invalid password for user: {email}");
                        return false;
                    }
                }

                // Kullanıcı kimlik bilgilerini oluştur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, "Admin User"),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim("LastLogin", DateTime.UtcNow.ToString("o"))
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, 
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    // Oturum süresini ayarla
                    ExpiresUtc = rememberMe 
                        ? DateTimeOffset.UtcNow.AddDays(30) // Beni Hatırla işaretliyse 30 gün
                        : DateTimeOffset.UtcNow.AddMinutes(30), // Değilse 30 dakika
                    IsPersistent = rememberMe,
                    AllowRefresh = true,
                    IssuedUtc = DateTimeOffset.UtcNow
                };

                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogError("HttpContext is null");
                    return false;
                }

                // Önceki oturumu temizle
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                
                // Yeni oturumu başlat
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"Login successful for user: {email}");
                _logger.LogInformation("=== AUTH SERVICE LOGIN SUCCESS ===");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for user: {email}");
                return false;
            }
        }

        // Şifre hash'ini oluşturur
        public (string hash, string salt) HashPassword(string password)
        {
            // Rastgele tuz oluştur
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            
            // PBKDF2 ile hash oluştur
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password: password,
                salt: salt,
                iterations: Iterations,
                hashAlgorithm: HashAlgorithmName.SHA256);
                
            byte[] hash = pbkdf2.GetBytes(KeySize);
            
            _logger.LogInformation("Password hashed successfully");
            return (
                hash: Convert.ToBase64String(hash),
                salt: Convert.ToBase64String(salt)
            );
        }
        
        // Şifreyi doğrula
        public bool VerifyPassword(string password, string savedHash, string savedSalt)
        {
            try
            {
                if (string.IsNullOrEmpty(savedHash) || string.IsNullOrEmpty(savedSalt))
                {
                    _logger.LogWarning("Hash or salt is null or empty");
                    return false;
                }
                
                byte[] hashBytes = Convert.FromBase64String(savedHash);
                byte[] saltBytes = Convert.FromBase64String(savedSalt);
                
                // PBKDF2 ile hash hesapla
                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password: password,
                    salt: saltBytes,
                    iterations: Iterations,
                    hashAlgorithm: HashAlgorithmName.SHA256);
                    
                byte[] computedHash = pbkdf2.GetBytes(KeySize);
                
                // Hash'leri karşılaştır
                for (int i = 0; i < KeySize; i++)
                {
                    if (hashBytes[i] != computedHash[i])
                    {
                        _logger.LogWarning("Password verification failed - hashes do not match");
                        return false;
                    }
                }
                
                _logger.LogInformation("Password verified successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password");
                return false;
            }
        }

        /// <summary>
        /// Kullanıcıyı sistemden çıkış yaptırır
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogWarning("HttpContext is null during logout");
                    return;
                }

                // Kullanıcı bilgilerini al
                var user = httpContext.User;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                
                _logger.LogInformation($"Logging out user: {userId}");

                // Oturumu sonlandır
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);


                // Tarayıcıdaki çerezleri sil
                var cookies = httpContext.Request.Cookies.Keys;
                foreach (var cookie in cookies)
                {
                    httpContext.Response.Cookies.Delete(cookie);
                }

                _logger.LogInformation($"User {userId} has been successfully logged out");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout");
                throw; // Hatanın yönetilmesi için yukarı fırlat
            }
        }

        /// <summary>
        /// Kullanıcının kimlik doğrulama durumunu kontrol eder
        /// </summary>
        public Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogWarning("HttpContext is null in IsAuthenticatedAsync");
                    return Task.FromResult(false);
                }

                var user = httpContext.User;
                if (user?.Identity == null)
                {
                    _logger.LogWarning("User or Identity is null in IsAuthenticatedAsync");
                    return Task.FromResult(false);
                }

                if (!user.Identity.IsAuthenticated)
                {
                    _logger.LogInformation("User is not authenticated");
                    return Task.FromResult(false);
                }

                // Kullanıcının email claim'ini kontrol et
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email) || !string.Equals(email, _adminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Invalid or missing email in claims: {email}");
                    return Task.FromResult(false);
                }

                // Kullanıcı claim'lerini logla (debug için)
                _logger.LogInformation("User claims:");
                foreach (var claim in user.Claims)
                {
                    _logger.LogInformation($"{claim.Type} = {claim.Value}");
                }

                _logger.LogInformation($"User {email} is authenticated");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication status");
                return Task.FromResult(false);
            }
        }
    }
}
