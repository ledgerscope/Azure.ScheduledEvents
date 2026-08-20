using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Azure.ScheduledEvents
{
    public static class ServiceExtensions
    {
        public static T AddScheduledEventsClient<T>(this T services) where T : IServiceCollection
        {
            services.AddHttpClient();
            services.AddLogging();

            services.AddSingleton<SourceGenerationContext>();
            services.AddSingleton<ScheduledEventsCoordinator>();
            services.AddSingleton<ScheduledEventsClient>();
            services.AddSingleton<ScheduledEventsCancellationTokenSource>();

            return services;
        }
    }
}
