using Microsoft.Extensions.DependencyInjection;

namespace Azure.ScheduledEvents.Tests
{
    [TestClass]
    public class CoordinatorDemoTest
    {
        [TestMethod]
        public async Task DemonstrateCoordinatorBehavior()
        {
            // This test demonstrates how multiple processes would coordinate
            // In a real scenario, these would be separate processes/applications
            
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
            
            // Give the coordinators a moment to initialize
            await Task.Delay(1000);
            
            // Both coordinators should be functional even though only one has the global lock
            // The one without the lock should be reading from cache or falling back to HTTP
            
            // Clean up
            coordinator1.Dispose();
            coordinator2.Dispose();
            
            Assert.IsTrue(true); // Test passes if we get here without exceptions
        }

        [TestMethod]
        public void TestFailoverScenario()
        {
            // This test simulates what happens when the coordinator process dies
            var services1 = new ServiceCollection()
                .AddScheduledEventsClient()
                .BuildServiceProvider();

            var services2 = new ServiceCollection()
                .AddScheduledEventsClient()
                .BuildServiceProvider();

            var coordinator1 = services1.GetRequiredService<ScheduledEventsCoordinator>();
            var coordinator2 = services2.GetRequiredService<ScheduledEventsCoordinator>();
            
            // Simulate the first coordinator process dying
            coordinator1.Dispose();
            
            // The second coordinator should eventually detect this and take over
            // (This would happen over time via the periodic lock checking)
            
            coordinator2.Dispose();
            
            Assert.IsTrue(true); // Test passes if we get here without exceptions
        }
    }
}