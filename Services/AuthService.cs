using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PortfolioApp.Services
{
    public class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;
        private readonly ApplicationDbContext _dbContext;

        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public AuthService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger,
            ApplicationDbContext dbContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe = false)
        {
            _logger.LogInformation("Login attempt for email: {email}", email);

            if (_httpContextAccessor.HttpContext == null)
            {
                _logger.LogError("HttpContext is null");
                return false;
            }

            var admin = await _dbContext.AdminUsers.FirstOrDefaultAsync(a => a.Email == email);
            if (admin == null)
            {
                _logger.LogWarning("No user found with email: {email}", email);
                return false;
            }

            if (!VerifyPassword(password, admin.PasswordHash, admin.PasswordSalt))
            {
                _logger.LogWarning("Invalid password for user: {email}", email);
                return false;
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, "Administrator")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddMinutes(30),
                AllowRefresh = true
            };

            await _httpContextAccessor.HttpContext.SignOutAsync();
            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                authProps);

            _logger.LogInformation("Login successful for {email}", email);
            return true;
        }

        public async Task LogoutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("User signed out successfully");
            }
            else
            {
                _logger.LogWarning("HttpContext was null during logout");
            }
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var result = httpContext?.User?.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation("Authentication check result: {auth}", result);
            return Task.FromResult(result);
        }

        private bool VerifyPassword(string password, string hashBase64, string saltBase64)
        {
            _logger.LogInformation("Verifying password...");
            _logger.LogInformation("Password input: {Password}", password);
            _logger.LogInformation("Hash (short): {Hash}", hashBase64.Substring(0, 10));
            _logger.LogInformation("Salt (short): {Salt}", saltBase64.Substring(0, 10));

            var salt = Convert.FromBase64String(saltBase64);
            var hash = Convert.FromBase64String(hashBase64);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(KeySize);

            for (int i = 0; i < KeySize; i++)
            {
                if (computedHash[i] != hash[i])
                {
                    _logger.LogWarning("Password verification failed at byte index: {Index}", i);
                    return false;
                }
            }

            _logger.LogInformation("Password verified successfully");
            return true;
        }
    }
}
