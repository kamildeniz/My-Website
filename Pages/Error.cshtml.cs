using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Pages
{
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public ErrorModel(ILogger<ErrorModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public string? RequestId { get; set; } = string.Empty;
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public new int? StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? InnerExceptionMessage { get; set; }
        public string? StackTrace { get; set; }
        public string? Path { get; set; }
        public bool ShowDevelopmentInfo => _environment.IsDevelopment();

        public IActionResult OnGet(int? statusCode = null)
        {
            try
            {
                // Get the status code from the query string, route data or the response
                StatusCode = statusCode ?? 
                    (HttpContext.Response.StatusCode != 200 ? HttpContext.Response.StatusCode : 500);
                
                // Get the exception details if available
                var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
                var exception = exceptionHandlerPathFeature?.Error;

                // If we have a status code re-execute feature, we can get the original path
                if (statusCodeReExecuteFeature != null)
                {
                    Path = statusCodeReExecuteFeature.OriginalPath;
                }

                if (exception != null)
                {
                    // Log the exception
                    _logger.LogError(exception, "An unhandled exception occurred at {Path}", exceptionHandlerPathFeature?.Path);
                    
                    // Set error details for development environment
                    if (_environment.IsDevelopment())
                    {
                        ExceptionMessage = exception.Message;
                        InnerExceptionMessage = exception.InnerException?.Message;
                        StackTrace = exception.StackTrace;
                    }
                }
                else
                {
                    // Log the status code error
                    _logger.LogWarning("Status Code {StatusCode} occurred at {Path}", StatusCode, HttpContext.Request.Path);
                }

                // Set the request ID for correlation
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                
                // Set the path where the error occurred
                Path = exceptionHandlerPathFeature?.Path ?? HttpContext.Request.Path;
                
                // Set a user-friendly error message based on the status code
                SetErrorMessage();
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "An error occurred in the error page");
                return StatusCode(500, "A critical error occurred while processing your request.");
            }
        }

        public IActionResult OnPost()
        {
            // Handle POST requests the same way as GET
            return OnGet();
        }

        private void SetErrorMessage()
        {
            ErrorMessage = StatusCode switch
            {
                400 => "Geçersiz istek. Lütfen girdiğiniz bilgileri kontrol edip tekrar deneyin.",
                401 => "Bu sayfayı görüntülemek için giriş yapmalısınız.",
                403 => "Bu sayfaya erişim izniniz bulunmamaktadır.",
                404 => "Aradığınız sayfa bulunamadı.",
                500 => "Sunucuda bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
                _ => "Beklenmeyen bir hata oluştu."
            };
        }
    }
}

