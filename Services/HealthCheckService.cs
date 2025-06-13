using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace PortfolioApp.Services
{
    public class HealthCheckService : IHealthCheck
    {
        private readonly ILogger<HealthCheckService> _logger;
        private static readonly Random _random = new Random();
        private bool _isHealthy = true;

        public HealthCheckService(ILogger<HealthCheckService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Simulate occasional failures for testing
            // In a real app, you'd check actual dependencies
            _ = SimulateOccasionalFailures();
        }

        public Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check application health here
                var memoryInfo = GC.GetGCMemoryInfo();
                var allocated = GC.GetTotalMemory(forceFullCollection: false);
                var memoryThreshold = 1024 * 1024 * 1024; // 1GB
                var status = _isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;
                
                var data = new Dictionary<string, object>
                {
                    { "allocated_bytes", allocated },
                    { "gen0_collection_count", GC.CollectionCount(0) },
                    { "gen1_collection_count", GC.CollectionCount(1) },
                    { "gen2_collection_count", GC.CollectionCount(2) },
                    { "timestamp", DateTime.UtcNow }
                };

                var result = _isHealthy 
                    ? HealthCheckResult.Healthy("Application is healthy", data) 
                    : HealthCheckResult.Unhealthy("Application is unhealthy", null, data);
                
                _logger.LogInformation("Health check executed. Status: {Status}", status);
                
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return Task.FromResult(
                    HealthCheckResult.Unhealthy("An exception was thrown when checking health", ex));
            }
        }
        
        private async Task SimulateOccasionalFailures()
        {
            while (true)
            {
                // Simulate a failure 10% of the time
                _isHealthy = _random.Next(1, 11) > 1;
                
                if (!_isHealthy)
                {
                    _logger.LogWarning("Simulated health check failure");
                }
                
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
            // ReSharper disable once FunctionNeverReturns
        }
    }
}
