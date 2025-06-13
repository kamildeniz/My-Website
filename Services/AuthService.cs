using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PortfolioApp.Services
{
    public class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _adminEmail;
        private readonly string _adminPasswordHash;
        private readonly string _adminSalt;
        private const int KeySize = 32; // 256 bit
        private const int Iterations = 100000; // PBKDF2 iteration count

        public AuthService(
            IHttpContextAccessor httpContextAccessor, 
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configuration = configuration;
            
            // Yapılandırmadan kimlik bilgilerini al
            _adminEmail = _configuration["AdminCredentials:Email"] ?? "admin@example.com";
            _adminPasswordHash = _configuration["AdminCredentials:PasswordHash"];
            _adminSalt = _configuration["AdminCredentials:PasswordSalt"];
            
            // Eğer yapılandırmada hash yoksa, varsayılan şifre için hash oluştur
            if (string.IsNullOrEmpty(_adminPasswordHash) || string.IsNullOrEmpty(_adminSalt))
            {
                // Sadece geliştirme ortamında kullanılacak varsayılan değerler
                if (_configuration.GetValue<bool>("IsDevelopment"))
                {
                    _adminPasswordHash = HashPassword("Admin123!");
                }
                else
                {
                    throw new InvalidOperationException("Admin credentials are not properly configured.");
                }
            }
            
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

                // Şifre doğrulaması
                bool isPasswordValid = VerifyPassword(password, _adminPasswordHash, _adminSalt);
                
                if (!isPasswordValid)
                {
                    _logger.LogWarning($"Login failed - Invalid password for user: {email}");
                    return false;
                }

                // Kullanıcı kimlik bilgilerini oluştur
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, email),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, "Admin User"),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim("LastLogin", DateTime.UtcNow.ToString("o")),
                    new Claim("CustomClaim", "CustomValue")
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
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during login for user: {email}");
                return false;
            }
        }

        // Şifre hash'ini oluşturur
        public string HashPassword(string password)
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
            
            // Hash ve tuzu birleştir (tuz + hash)
            byte[] hashBytes = new byte[16 + KeySize];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, KeySize);
            
            // Base64'e çevir
            string savedPasswordHash = Convert.ToBase64String(hashBytes);
            
            // Tuzu da kaydet (gerçek uygulamada güvenli bir şekilde saklanmalı)
            string savedSalt = Convert.ToBase64String(salt);
            
            _logger.LogInformation("Password hashed successfully");
            return $"{savedPasswordHash}:{savedSalt}";
        }
        
        // Şifreyi doğrula
        public bool VerifyPassword(string password, string savedPasswordHashWithSalt, string savedSalt = null)
        {
            try
            {
                string[] hashParts = savedPasswordHashWithSalt.Split(':');
                string savedHash = hashParts[0];
                string salt = savedSalt ?? (hashParts.Length > 1 ? hashParts[1] : null);
                
                if (string.IsNullOrEmpty(salt))
                {
                    _logger.LogWarning("Salt not found, using legacy verification");
                    // Eski şifre doğrulama yöntemi (sadece geliştirme için)
                    return savedPasswordHashWithSalt == password;
                }
                
                byte[] hashBytes = Convert.FromBase64String(savedHash);
                byte[] saltBytes = Convert.FromBase64String(salt);
                
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
                    if (hashBytes[16 + i] != computedHash[i])
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

                // Session'ı temizle
                httpContext.Session.Clear();

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


                _logger.LogInformation("Admin credentials matched");
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, 
                    CookieAuthenticationDefaults.AuthenticationScheme);


                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogError("HttpContext is null");
                    return false;
                }


                _logger.LogInformation("Signing in...");
                
                // Sign in with default authentication scheme
                await httpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        AllowRefresh = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                        IssuedUtc = DateTimeOffset.UtcNow
                    });

                _logger.LogInformation("User signed in successfully");
                
                // Log the authentication cookie that was set
                if (httpContext.Response.Headers.TryGetValue("Set-Cookie", out var setCookieHeaders))
                {
                    _logger.LogInformation("Set-Cookie headers:");
                    foreach (var header in setCookieHeaders)
                    {
                        _logger.LogInformation(header);
                    }
                }
                else
                {
                    _logger.LogWarning("No Set-Cookie header found in response");
                }

                _logger.LogInformation("=== AUTH SERVICE LOGIN SUCCESS ===");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                _logger.LogInformation("=== AUTH SERVICE LOGIN ERROR ===");
                return false;
            }
        }

        public async Task Logout()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            else
            {
                _logger.LogWarning("HttpContext is null in Logout method");
            }
        }

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

                // Additional check - verify the user has required claims
                var email = user.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email) || !string.Equals(email, AdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Invalid or missing email in claims: {email}");
                    return Task.FromResult(false);
                }

                _logger.LogInformation($"User {email} is authenticated with valid claims");
                
                // Log all claims for debugging
                _logger.LogInformation("User claims:");
                foreach (var claim in user.Claims)
                {
                    _logger.LogInformation($"{claim.Type} = {claim.Value}");
                }

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsAuthenticatedAsync");
                return Task.FromResult(false);
            }
        }
    }
}
