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
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public AuthService(ApplicationDbContext dbContext, ILogger<AuthService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AdminUser?> ValidateCredentialsAsync(string email, string password)
        {
            _logger.LogInformation("ValidateCredentials: Trying to authenticate {Email}", email);

            var user = await _dbContext.AdminUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Email}", email);
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                _logger.LogWarning("Invalid password for user: {Email}", email);
                return null;
            }

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
