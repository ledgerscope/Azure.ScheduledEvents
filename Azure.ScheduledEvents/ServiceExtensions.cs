using Microsoft.Extensions.DependencyInjection;

namespace Azure.ScheduledEvents
{
    public static class ServiceExtensions
    {
        public static T AddScheduledEventsClient<T>(this T services) where T : IServiceCollection
        {
            services.AddHttpClient();

            services.AddSingleton<SourceGenerationContext>();
            services.AddSingleton<ScheduledEventsClient>();
            services.AddSingleton<ScheduledEventsCancellationTokenSource>();

            return services;
        }
    }
}
