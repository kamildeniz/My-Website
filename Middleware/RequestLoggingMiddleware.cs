using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;

namespace PortfolioApp.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/favicon.ico") ||
                context.Request.Path.StartsWithSegments("/_framework") ||
                context.Request.Path.StartsWithSegments("/_blazor") ||
                context.Request.Path.Value.EndsWith(".js") ||
                context.Request.Path.Value.EndsWith(".css"))
            {
                await _next(context);
                return;
            }

            var requestInfo = await FormatRequest(context.Request);
            var originalBodyStream = context.Response.Body;

            await using var responseBody = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                var responseInfo = await FormatResponse(context.Response);

                var level = context.Response.StatusCode switch
                {
                    >= 500 => LogLevel.Error,
                    >= 400 => LogLevel.Warning,
                    _ => LogLevel.Information
                };

                _logger.Log(level, "HTTP Request: {@RequestInfo} | HTTP Response: {@ResponseInfo}", requestInfo, responseInfo);

                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Request Information: {@RequestInfo}", requestInfo);
                // We must rethrow the exception so that the global exception handler can process it.
                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task<object> FormatRequest(HttpRequest request)
        {
            var body = "[No Body]";
            try
            {
                if (request.Body.CanRead && request.ContentLength > 0)
                {
                    request.EnableBuffering();
                    using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    body = await reader.ReadToEndAsync();
                    request.Body.Position = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read request body.");
                body = "[Could not read body]";
            }

            return new
            {
                Scheme = request.Scheme,
                Host = request.Host.Value,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = body
            };
        }

        private async Task<object> FormatResponse(HttpResponse response)
        {
            var body = "[No Body]";
            try
            {
                if (response.Body.CanRead && response.Body.Length > 0)
                {
                    response.Body.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(response.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                    body = await reader.ReadToEndAsync();
                    response.Body.Seek(0, SeekOrigin.Begin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read response body.");
                body = "[Could not read body]";
            }

            return new
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = body
            };
        }
    }

    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}
