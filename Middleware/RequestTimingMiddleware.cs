using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PortfolioApp.Middleware
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;
        private readonly RequestTimingOptions _options;

        public RequestTimingMiddleware(
            RequestDelegate next, 
            ILogger<RequestTimingMiddleware> logger,
            IOptions<RequestTimingOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip timing for these paths
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/_framework") ||
                context.Request.Path.StartsWithSegments("/_blazor") ||
                context.Request.Path.StartsWithSegments("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Add a unique identifier to the response headers
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Add("X-Request-ID", context.TraceIdentifier);
                    return Task.CompletedTask;
                });

                await _next(context);
                
                stopwatch.Stop();
                
                // Log slow requests
                if (stopwatch.ElapsedMilliseconds > _options.SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow Request: {Method} {Path} took {ElapsedMilliseconds}ms - Status: {StatusCode}",
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds,
                        context.Response.StatusCode);
                }
                
                // Log request timing
                if (_options.LogAllRequests)
                {
                    _logger.LogInformation(
                        "Request: {Method} {Path} - Status: {StatusCode} - Time: {ElapsedMilliseconds}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex) when (LogException(context, stopwatch, ex))
            {
                // Exception was logged, rethrow it
                throw;
            }
        }
        
        private bool LogException(HttpContext context, Stopwatch stopwatch, Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                ex,
                "Request Error: {Method} {Path} - Status: {StatusCode} - Time: {ElapsedMilliseconds}ms - Error: {ErrorMessage}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
                
            return false; // Don't handle the exception, just log it
        }
    }
    
    public class RequestTimingOptions
    {
        public bool LogAllRequests { get; set; } = true;
        public long SlowRequestThresholdMs { get; set; } = 1000; // 1 second
    }
    
    public static class RequestTimingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestTimingMiddleware>();
        }
        
        public static IServiceCollection AddRequestTiming(this IServiceCollection services, Action<RequestTimingOptions> configure = null)
        {
            services.Configure(configure ?? (options => { }));
            return services;
        }
    }
}
