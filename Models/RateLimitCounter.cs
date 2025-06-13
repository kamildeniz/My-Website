using System;

namespace PortfolioApp.Models
{
    public class RateLimitCounter
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int Count { get; set; } = 0;
    }
}
