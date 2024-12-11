using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;
using System.Text.Json;

namespace Azure.ScheduledEvents.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var httpClientFactory = new ServiceCollection().AddHttpClient().BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
            var client = new ScheduledEventsClient(httpClientFactory, new SourceGenerationContext());

            var doc = await client.GetScheduledEvents();
        }

        [TestMethod]
        public async Task TestMethod2()
        {
            var httpClientFactory = new ServiceCollection().AddHttpClient().BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
            ScheduledEventsClient client = new ScheduledEventsClient(httpClientFactory, new SourceGenerationContext());

            using (var cts = new ScheduledEventsCancellationTokenSource(client, NullLogger<ScheduledEventsCancellationTokenSource>.Instance))
            {
                var token = cts.GetCancellationToken();
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000);
                }
            }
        }

        [TestMethod]
        public void TestString()
        {
            var json = """
                                {
                    "DocumentIncarnation":  2,
                    "Events":  [
                                   {
                                       "EventId":  "C7061BAC-AFDC-4513-B24B-AA5F13A16123",
                                       "EventStatus":  "Scheduled",
                                       "EventType":  "Freeze",
                                       "ResourceType":  "VirtualMachine",
                                       "Resources":  [
                                                         "WestNO_0",
                                                         "WestNO_1"
                                                     ],
                                       "NotBefore":  "Mon, 11 Apr 2022 22:26:58 GMT",
                                       "Description":  "Virtual machine is being paused because of a memory-preserving Live Migration operation.",
                                       "EventSource":  "Platform",
                                       "DurationInSeconds":  5
                                   }
                               ]
                }
                """;

            var sgc = new SourceGenerationContext();
            var doc = JsonSerializer.Deserialize<ScheduledEventsDocument>(json, sgc.ScheduledEventsDocument);
        }
    }
}