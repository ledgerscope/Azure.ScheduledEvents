using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Azure.ScheduledEvents
{
    public partial class ScheduledEventsCancellationTokenSource : IDisposable
    {
        private readonly Task _t;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationTokenSource _externalCancellationTokenSource = new CancellationTokenSource();
        private readonly ScheduledEventsClient _client;
        private readonly ILogger<ScheduledEventsCancellationTokenSource> logger;
        private readonly TimeSpan _noticePeriod = TimeSpan.FromMinutes(5);

        public ScheduledEventsCancellationTokenSource(ScheduledEventsClient client, ILogger<ScheduledEventsCancellationTokenSource> logger)
        {
            _client = client;
            this.logger = logger;
            _t = Timer();
        }

        public void Dispose()
        {
            _externalCancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource.Token;
        }

        private async Task Timer()
        {
            using var pt = new PeriodicTimer(TimeSpan.FromSeconds(10));
            using var tokens = CancellationTokenSource.CreateLinkedTokenSource(_externalCancellationTokenSource.Token, _cancellationTokenSource.Token);

            while (await pt.WaitForNextTickAsync(tokens.Token))
            {
                try
                {
                    var document = await _client.GetScheduledEvents();

                    if (document?.Events != null)
                    {
                        foreach (var item in document.Events)
                        {
                            if (item.NotBefore.HasValue)
                            {
                                if (item.NotBefore.Value.Add(_noticePeriod) < DateTime.UtcNow)
                                {
                                    _cancellationTokenSource.Cancel();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogErrorRetrievingScheduledEvents(ex);
                    await Task.Delay(TimeSpan.FromSeconds(Random.Shared.Next(10, 20)));
                }
            }
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Error retrieving ScheduledEvents")]
        public partial void LogErrorRetrievingScheduledEvents(Exception ex);
    }
}
