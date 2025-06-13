using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace PortfolioApp.Services
{
    public class HealthCheckJsonFormatter
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public HealthCheckJsonFormatter(IOptions<JsonSerializerOptions> jsonOptions)
        {
            _jsonOptions = jsonOptions?.Value ?? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        public Task WriteResponseAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var result = new
            {
                status = report.Status.ToString(),
                duration = report.TotalDuration,
                info = report.Entries.Select(e => new
                {
                    key = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    data = e.Value.Data,
                    duration = e.Value.Duration,
                    exception = e.Value.Exception?.Message,
                    tags = e.Value.Tags
                }),
                timestamp = DateTime.UtcNow
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(result, _jsonOptions));
        }

        public static Task WriteMinimalResponseAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            
            var result = new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow
            };
            
            return context.Response.WriteAsync(JsonSerializer.Serialize(result));
        }
    }
}
