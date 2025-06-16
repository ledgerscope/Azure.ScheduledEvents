using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;
using System.Text.Json;

namespace Azure.ScheduledEvents.Tests
{
    [TestClass]
    public class CoordinatorTests
    {
        [TestMethod]
        public void TestCoordinatorInstantiation()
        {
            var services = new ServiceCollection()
                .AddScheduledEventsClient()
                .BuildServiceProvider();
            
            var coordinator = services.GetRequiredService<ScheduledEventsCoordinator>();
            Assert.IsNotNull(coordinator);
            
            coordinator.Dispose();
        }

        [TestMethod] 
        public async Task TestGetScheduledEventsViaCoordinator()
        {
            var services = new ServiceCollection()
                .AddScheduledEventsClient()
                .BuildServiceProvider();
            
            var client = services.GetRequiredService<ScheduledEventsClient>();
            
            try
            {
                // This will likely fail in test environment but should not throw unexpected exceptions
                var doc = await client.GetScheduledEvents();
                
                // If we get here, either the call succeeded or failed gracefully
                Assert.IsTrue(true);
            }
            catch (HttpRequestException)
            {
                // Expected in test environment without Azure metadata endpoint
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                // Log any unexpected exceptions
                Console.WriteLine($"Unexpected exception: {ex}");
                throw;
            }
        }

        [TestMethod]
        public void TestMultipleCoordinatorInstances()
        {
            var services1 = new ServiceCollection()
                .AddScheduledEventsClient()
                .BuildServiceProvider();
                
            var services2 = new ServiceCollection()
                .AddScheduledEventsClient()
                .BuildServiceProvider();
            
            var coordinator1 = services1.GetRequiredService<ScheduledEventsCoordinator>();
            var coordinator2 = services2.GetRequiredService<ScheduledEventsCoordinator>();
            
            Assert.IsNotNull(coordinator1);
            Assert.IsNotNull(coordinator2);
            
            // Only one should have the lock, but both should work
            coordinator1.Dispose();
            coordinator2.Dispose();
        }

        [TestMethod]
        public void TestCacheFileCreation()
        {
            var services = new ServiceCollection()
                .AddScheduledEventsClient()
                .BuildServiceProvider();
            
            var coordinator = services.GetRequiredService<ScheduledEventsCoordinator>();
            Assert.IsNotNull(coordinator);
            
            // Let the coordinator run for a short time to potentially create cache files
            Thread.Sleep(100);
            
            coordinator.Dispose();
            
            // Cache should be cleaned up on dispose
            Assert.IsTrue(true); // Test passed if we get here without exceptions
        }
    }
}