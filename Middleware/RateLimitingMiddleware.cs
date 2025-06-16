using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortfolioApp.Middleware;
using PortfolioApp.Models;
using PortfolioApp.Options;

namespace PortfolioApp.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly RateLimitingOptions _options;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "RateLimit_";

        public RateLimitingMiddleware(
            RequestDelegate next,
            ILogger<RateLimitingMiddleware> logger,
            IOptions<RateLimitingOptions> options,
            IMemoryCache cache)
        {
            _next = next;
            _logger = logger;
            _options = options.Value;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip rate limiting if disabled
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            // Skip whitelisted paths
            if (IsPathWhitelisted(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var clientIp = GetClientIpAddress(context);
            var endpoint = context.Request.Path;
            var cacheKey = $"{CacheKeyPrefix}{clientIp}:{endpoint}";

            var counter = GetOrCreateCounter(cacheKey);

            // Check if rate limit is exceeded
            if (counter.Count >= _options.RequestLimit)
            {
                _logger.LogWarning("Rate limit exceeded for {ClientIp} on {Endpoint}", clientIp, endpoint);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = _options.WindowInSeconds.ToString();
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Increment the counter
            counter.Count++;

            _logger.LogDebug("Request {Count}/{Limit} from {ClientIp} to {Endpoint}", 
                counter.Count, _options.RequestLimit, clientIp, endpoint);

            // Add rate limit headers to response
            context.Response.Headers["X-RateLimit-Limit"] = _options.RequestLimit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = (_options.RequestLimit - counter.Count).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = (counter.Timestamp.AddSeconds(_options.WindowInSeconds) - DateTime.UtcNow).TotalSeconds.ToString("0");

            await _next(context);
        }

        private bool IsPathWhitelisted(PathString path)
        {
            foreach (var whitelistedPath in _options.WhitelistedPaths)
            {
                if (path.StartsWithSegments(whitelistedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private RateLimitCounter GetOrCreateCounter(string cacheKey)
        {
            if (!_cache.TryGetValue(cacheKey, out RateLimitCounter counter))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromSeconds(_options.WindowInSeconds));

                counter = new RateLimitCounter();
                _cache.Set(cacheKey, counter, cacheEntryOptions);
            }

            return counter;
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            // Check for a forwarded header first (if behind a proxy)
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
