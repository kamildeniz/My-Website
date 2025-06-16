using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PortfolioApp.Middleware;
using PortfolioApp.Models;

namespace PortfolioApp.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(
            RequestDelegate next, 
            ILogger<GlobalExceptionHandlerMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = context.Response;
            var errorResponse = new ErrorResponse();
            
            switch (exception)
            {
                case ApplicationException ex:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = ex.Message;
                    _logger.LogWarning(ex, "Application Exception");
                    break;
                case UnauthorizedAccessException ex:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "Unauthorized access";
                    _logger.LogWarning(ex, "Unauthorized Access");
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Message = "Internal server error";
                    _logger.LogError(exception, "Unhandled Exception");
                    break;
            }

            // Include stack trace in development
            if (_env.IsDevelopment())
            {
                errorResponse.StackTrace = exception.ToString();
                errorResponse.InnerException = exception.InnerException?.Message;
            }

            // Log the error
            _logger.LogError(exception, $"An error occurred: {exception.Message}");
            
            // If the request is for an API, return JSON response
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                var result = JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(result);
            }
            else
            {
                // For non-API requests, redirect to the error page
                context.Response.Redirect($"~/Error?statusCode={response.StatusCode}&message={Uri.EscapeDataString(errorResponse.Message)}");
            }
        }
    }

    public class ErrorResponse
    {
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
    }
}

// Extension method used to add the middleware to the HTTP request pipeline.
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
