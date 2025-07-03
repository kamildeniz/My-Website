using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// NLog: Setup NLog for Dependency injection
builder.Logging.ClearProviders();
builder.Host.UseNLog();

var logger = NLog.LogManager.GetCurrentClassLogger();
logger.Debug("init main");

try
{
    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/Admin");
        options.Conventions.AllowAnonymousToPage("/Admin/Login");
        options.Conventions.AllowAnonymousToPage("/Error");
    });


    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "DataProtection-Keys")))
        .SetApplicationName("PortfolioApp");

    builder.Services.AddMemoryCache();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Security:SessionTimeoutInMinutes", 30));
        options.Cookie.Name = ".Portfolio.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = builder.Configuration.GetValue<bool>("Security:RequireHttps", false)
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.None;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = builder.Configuration["Security:CookieName"] ?? ".AspNet.SharedCookie";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = builder.Configuration.GetValue<bool>("Security:RequireHttps", false)
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.None;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.IsEssential = true;
            options.Cookie.MaxAge = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Security:SessionTimeoutInMinutes", 30));
            options.LoginPath = "/Admin/Login";
            options.LogoutPath = "/Admin/Logout";
            options.AccessDeniedPath = "/Error?statusCode=403";
            options.ReturnUrlParameter = "returnUrl";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("Security:SessionTimeoutInMinutes", 30));
            options.SlidingExpiration = true;
            options.ClaimsIssuer = "PortfolioApp";
            options.Events = new CookieAuthenticationEvents
            {
                OnSigningIn = context =>
                {
                    Console.WriteLine($"User {context.Principal?.Identity?.Name ?? "[unknown]"} is signing in.");
                    return Task.CompletedTask;
                },
                OnSignedIn = context =>
                {
                    Console.WriteLine($"User {context.Principal?.Identity?.Name ?? "[unknown]"} has successfully signed in.");
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

    builder.Services.AddAuthorization();
    builder.Services.AddHealthChecks()
        .AddCheck<PortfolioApp.Services.HealthCheckService>("self", tags: new[] { "ready" })
        .AddCheck("live", () => HealthCheckResult.Healthy("Application is live"), tags: new[] { "live" });

    builder.Services.Configure<RequestTimingOptions>(builder.Configuration.GetSection("RequestTiming"));
    builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<PortfolioApp.Services.HealthCheckService>();
    builder.Services.AddScoped<BlogService>();
    builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly());

    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

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

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
    builder.Services.Configure<JsonSerializerOptions>(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.WriteIndented = true;
    });
    builder.Services.AddSingleton<HealthCheckJsonFormatter>();
    builder.Services.Configure<RateLimitingOptions>(options =>
    {
        options.Enabled = true;
        options.RequestLimit = 100;
        options.WindowInSeconds = 60;
        options.WhitelistedPaths = new[] { "/health", "/healthz", "/_framework", "/_blazor", "/favicon.ico" };
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            DatabaseSeeder.Initialize(services);
        }
        catch (Exception ex)
        {
            var scopeLogger = services.GetRequiredService<ILogger<Program>>();
            scopeLogger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;
            var errorLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            errorLogger.LogError(exception, "Global exception caught: {Message}", exception?.Message);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/html";
            var errorPath = $"/Error?statusCode={context.Response.StatusCode}";
            if (exception is UnauthorizedAccessException)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                errorPath = "/Error?statusCode=401";
            }
            context.Response.Redirect(errorPath);
        });
    });

    app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseHsts();
    }

    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; img-src 'self' data: https:; font-src 'self' https://cdn.jsdelivr.net; connect-src 'self'; frame-ancestors 'self';");
        await next();
    });

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseMiddleware<RequestTimingMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<RateLimitingMiddleware>();

    app.UseSession();

    app.UseAuthentication();
    app.UseAuthorization();

    // app.UseStatusCodePages(async context =>
    // {
    //     var response = context.HttpContext.Response;
    //     var request = context.HttpContext.Request;
    //     var statusCodeLogger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

    //     if (response.StatusCode == (int)HttpStatusCode.Unauthorized)
    //     {
    //         statusCodeLogger.LogWarning("Unauthorized access to {Path}", request.Path);
    //         response.Redirect($"/Admin/Login?returnUrl={Uri.EscapeDataString(request.Path + request.QueryString)}");
    //         return;
    //     }
    //     if (response.StatusCode >= 400)
    //     {
    //         statusCodeLogger.LogWarning("Error {StatusCode} occurred for {Path}", response.StatusCode, request.Path);
    //         response.Redirect($"/Error?statusCode={response.StatusCode}");
    //     }
    // });



    app.MapControllers();
    app.MapRazorPages();
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

    app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
    app.MapGet("/favicon.ico", async context =>
    {
        context.Response.StatusCode = 404;
        await context.Response.CompleteAsync();
    });

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
            var initLogger = services.GetRequiredService<ILogger<Program>>();
            initLogger.LogError(ex, "An error occurred while initializing the database or processing markdown files.");
        }
    }

    app.MapGet("/generatehash", (string password) =>
    {
        var (hash, salt) = HashPassword(password);
        return Results.Ok(new { Hash = hash, Salt = salt });
    });

    static (string hash, string salt) HashPassword(string password)
    {
        byte[] salt = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}
