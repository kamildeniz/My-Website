namespace PortfolioApp.Options
{
    public class RateLimitingOptions
    {
        public bool Enabled { get; set; } = true;
        public int RequestLimit { get; set; } = 100; // Number of requests
        public int WindowInSeconds { get; set; } = 60; // Time window in seconds
        public string[] WhitelistedPaths { get; set; } = new string[0];
    }
}
