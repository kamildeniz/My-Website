using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PortfolioApp.Pages
{
    public class RateLimitTestModel : PageModel
    {
        private readonly ILogger<RateLimitTestModel> _logger;

        public RateLimitTestModel(ILogger<RateLimitTestModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Rate limit test page accessed");
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class RateLimitTestController : ControllerBase
{
    private static int _requestCount = 0;
    private static DateTime _lastResetTime = DateTime.UtcNow;
    private const int RateLimit = 10; // Lower limit for testing purposes
    private const int WindowInSeconds = 60;

    [HttpGet]
    public IActionResult Get()
    {
        var now = DateTime.UtcNow;
        
        // Reset counter if window has passed
        if ((now - _lastResetTime).TotalSeconds > WindowInSeconds)
        {
            _requestCount = 0;
            _lastResetTime = now;
        }

        _requestCount++;

        var remaining = Math.Max(0, RateLimit - _requestCount);
        var resetInSeconds = (int)(_lastResetTime.AddSeconds(WindowInSeconds) - now).TotalSeconds;

        Response.Headers.Add("X-RateLimit-Limit", RateLimit.ToString());
        Response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
        Response.Headers.Add("X-RateLimit-Reset", resetInSeconds.ToString());

        if (_requestCount > RateLimit)
        {
            Response.Headers.Add("Retry-After", resetInSeconds.ToString());
            return StatusCode(429, new 
            { 
                message = $"Rate limit exceeded. Try again in {resetInSeconds} seconds.",
                limit = RateLimit,
                remaining = 0,
                reset = resetInSeconds
            });
        }

        return Ok(new 
        { 
            message = "Request successful!",
            count = _requestCount,
            limit = RateLimit,
            remaining = remaining,
            reset = resetInSeconds
        });
    }
}
