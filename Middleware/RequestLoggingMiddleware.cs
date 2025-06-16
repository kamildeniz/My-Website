using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IO;
using System.IO;
using PortfolioApp.Middleware; // RecyclableMemoryStreamManager için

namespace PortfolioApp.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;
        private readonly Microsoft.IO.RecyclableMemoryStreamManager _recyclableMemoryStreamManager;
        private const int ReadChunkBufferLength = 4096;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        public async Task Invoke(HttpContext context)
        {
            // Skip logging for these paths
            if (context.Request.Path.StartsWithSegments("/health") || 
                context.Request.Path.StartsWithSegments("/favicon.ico") ||
                context.Request.Path.StartsWithSegments("/_framework") ||
                context.Request.Path.StartsWithSegments("/_blazor"))
            {
                await _next(context);
                return;
            }

            var request = await FormatRequest(context.Request);
            
            // Copy the original response body stream
            var originalBodyStream = context.Response.Body;
            
            using (var responseBody = _recyclableMemoryStreamManager.GetStream())
            {
                context.Response.Body = responseBody;
                
                try
                {
                    await _next(context);
                    
                    var response = await FormatResponse(context.Response);
                    
                    var logMessage = $"Request: {request}\nResponse: {response}";
                    
                    // Log based on status code
                    if (context.Response.StatusCode >= 500)
                    {
                        _logger.LogError(logMessage);
                    }
                    else if (context.Response.StatusCode >= 400)
                    {
                        _logger.LogWarning(logMessage);
                    }
                    else
                    {
                        _logger.LogInformation(logMessage);
                    }
                    
                    await responseBody.CopyToAsync(originalBodyStream);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An unhandled exception occurred while processing the request: {request}");
                    throw;
                }
                finally
                {
                    context.Response.Body = originalBodyStream;
                }
            }
        }

        private async Task<string> FormatRequest(HttpRequest request)
        {
            request.EnableBuffering();
            
            using (var reader = new StreamReader(
                request.Body,
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                
                var headers = new StringBuilder();
                foreach (var header in request.Headers)
                {
                    headers.AppendLine($"{header.Key}: {header.Value}");
                }
                
                return $"\n{request.Scheme} {request.Host}{request.Path} {request.QueryString}\n" +
                       $"Headers:\n{headers}\n" +
                       $"Body: {body}";
            }
        }

        private async Task<string> FormatResponse(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            
            string text = await new StreamReader(response.Body, Encoding.UTF8).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            
            var headers = new StringBuilder();
            foreach (var header in response.Headers)
            {
                headers.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            
            return $"Status Code: {response.StatusCode}\n" +
                   $"Headers:\n{headers}" +
                   $"Body: {text}";
        }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
