using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PortfolioApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Web;
using PortfolioApp.Data;
using PortfolioApp.Data.Seeders;
using PortfolioApp.Filters;
using PortfolioApp.Middleware;
using PortfolioApp.Options;
using PortfolioApp.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/Login");
    options.Conventions.AllowAnonymousToPage("/Error");
})
.AddApplicationPart(typeof(Program).Assembly);

// Add memory cache
builder.Services.AddMemoryCache();

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Add memory cache
builder.Services.AddMemoryCache();

// Add session with configuration
var sessionTimeoutMinutes = builder.Configuration.GetValue<int>("Security:SessionTimeoutInMinutes", 30);
var requireHttps = builder.Configuration.GetValue<bool>("Security:RequireHttps", false);
var cookieName = builder.Configuration["Security:CookieName"] ?? ".AspNet.SharedCookie";

// Add session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);
    options.Cookie.Name = ".Portfolio.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = requireHttps 
        ? CookieSecurePolicy.Always 
        : CookieSecurePolicy.None;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add authentication with detailed cookie settings
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Cookie settings
        options.Cookie.Name = cookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = requireHttps 
            ? CookieSecurePolicy.Always 
            : CookieSecurePolicy.None;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.IsEssential = true;
        options.Cookie.MaxAge = TimeSpan.FromMinutes(sessionTimeoutMinutes);
        
        // Authentication paths
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.AccessDeniedPath = "/Error?statusCode=403";
        options.ReturnUrlParameter = "returnUrl";
        
        // Session settings
        options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionTimeoutMinutes);
        options.SlidingExpiration = true;
        
        // Security settings
        options.ClaimsIssuer = "PortfolioApp";
        
        // Event handlers
        options.Events = new CookieAuthenticationEvents
        {
            OnSigningIn = context =>
            {
                var userName = context.Principal?.Identity?.Name ?? "[unknown]";
                Console.WriteLine($"User {userName} is signing in.");
                return Task.CompletedTask;
            },
            OnSignedIn = context =>
            {
                var userName = context.Principal?.Identity?.Name ?? "[unknown]";
                Console.WriteLine($"User {userName} has successfully signed in.");
                return Task.CompletedTask;
            },
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.ContentType?.ToLower().Contains("application/json") == true)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api") ||
                    context.Request.ContentType?.ToLower().Contains("application/json") == true)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

// Add authorization
builder.Services.AddAuthorization();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<PortfolioApp.Services.HealthCheckService>("self", tags: new[] { "ready" })
    .AddCheck("live", () => HealthCheckResult.Healthy("Application is live"), tags: new[] { "live" });

// Configure request timing options
builder.Services.Configure<RequestTimingOptions>(builder.Configuration.GetSection("RequestTiming"));

// Configure rate limiting options
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PortfolioApp.Services.HealthCheckService>();

// Add API controllers
builder.Services.AddControllers()
    .AddApplicationPart(Assembly.GetExecutingAssembly());

// Add configuration
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();



// Add CORS if needed
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

// Add DbContext with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<AuthService>();

// Configure JSON options for health checks
builder.Services.Configure<JsonSerializerOptions>(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.WriteIndented = true;
});

// Register health check formatter
builder.Services.AddSingleton<HealthCheckJsonFormatter>();

// Configure rate limiting
builder.Services.Configure<RateLimitingOptions>(options =>
{
    options.Enabled = true;
    options.RequestLimit = 100; // Allow 100 requests
    options.WindowInSeconds = 60; // Per 60 seconds
    options.WhitelistedPaths = new[] 
    { 
        "/health", 
        "/healthz",
        "/_framework",
        "/_blazor",
        "/favicon.ico"
    };
});



var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        DatabaseSeeder.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        // Log the error
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(exception, "Global exception caught: {Message}", exception?.Message);
        
        // Set the status code
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "text/html";
        
        // Redirect to error page
        var errorPath = $"/Error?statusCode={context.Response.StatusCode}";
        if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            errorPath = $"/Error?statusCode=401";
        }
        
        context.Response.Redirect(errorPath);
    });
});

// Status code pages
app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");

// Development error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "connect-src 'self'; " +
        "frame-ancestors 'self';");
    
    await next();
});

// Configure forwarded headers for proxy/load balancer
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();

app.UseRouting();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Add session
app.UseSession();

// Add rate limiting middleware (after auth to allow authenticated requests to bypass)
app.UseMiddleware<RateLimitingMiddleware>();

// Add request timing middleware (after auth to include auth time)
app.UseMiddleware<RequestTimingMiddleware>();

// Add request logging middleware (after timing to include timing info)
app.UseMiddleware<RequestLoggingMiddleware>();

// Custom error handling middleware
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var request = context.HttpContext.Request;
    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    
    if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
    {
        logger.LogWarning("Unauthorized access to {Path}", request.Path);
        response.Redirect($"/Admin/Login?returnUrl={Uri.EscapeDataString(request.Path + request.QueryString)}");
        return;
    }
    
    if (response.StatusCode >= 400)
    {
        logger.LogWarning("Error {StatusCode} occurred for {Path}", response.StatusCode, request.Path);
        response.Redirect($"/Error?statusCode={response.StatusCode}");
    }
});

// Handle 404 errors
app.Use(async (context, next) =>
{
    await next();
    
    if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("404 Not Found: {Path}", context.Request.Path);
        context.Request.Path = "/Error";
        context.Response.StatusCode = 404;
        await next();
    }
});

// Map API controllers
app.MapControllers();

app.MapRazorPages();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    AllowCachingResponses = false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("live"),
    AllowCachingResponses = false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Legacy health check endpoint for backward compatibility
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// Handle favicon.ico
app.MapGet("/favicon.ico", async context =>
{
    context.Response.StatusCode = 404;
    await context.Response.CompleteAsync();
});

// Initialize database and process markdown files
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        
        var blogService = services.GetRequiredService<BlogService>();
        await blogService.ProcessMarkdownFilesAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database or processing markdown files.");
    }
}

app.Run();
