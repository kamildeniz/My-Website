using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortfolioApp.Data;
using PortfolioApp.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PortfolioApp.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public const int KeySize = 32;
        public const int Iterations = 100_000;

        public AuthService(ApplicationDbContext dbContext, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AdminUser?> ValidateCredentialsAsync(string email, string password)
        {
            _logger.LogInformation("--- [AuthService] Attempting to validate credentials for {Email} ---", email);

            var user = await _dbContext.AdminUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning("[AuthService] User not found in database: {Email}", email);
                return null;
            }
            _logger.LogInformation("[AuthService] User found. ID: {UserId}, Stored Salt: {Salt}, Stored Hash: {Hash}", user.Id, user.PasswordSalt, user.PasswordHash);

            bool isPasswordValid = VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
            _logger.LogInformation("[AuthService] Password verification result for {Email}: {Result}", email, isPasswordValid);

            if (!isPasswordValid)
            {
                _logger.LogWarning("!!! [AuthService] Password verification FAILED for user: {Email} !!!", email);
                return null;
            }

            _logger.LogInformation("--- [AuthService] Password verification SUCCEEDED for user: {Email} ---", email);
            return user;
        }

        public async Task LoginAsync(AdminUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public Task<bool> IsAuthenticatedAsync()
        {
            return Task.FromResult(_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated);
        }

        public async Task LogoutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        public static (string hash, string salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(KeySize);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(KeySize);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        private bool VerifyPassword(string password, string hashBase64, string saltBase64)
        {
            var salt = Convert.FromBase64String(saltBase64);
            var hash = Convert.FromBase64String(hashBase64);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(KeySize);

            for (int i = 0; i < KeySize; i++)
            {
                if (computedHash[i] != hash[i]) return false;
            }

            return true;
        }
    }
}
