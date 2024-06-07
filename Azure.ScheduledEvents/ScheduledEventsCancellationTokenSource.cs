using System;
using System.Threading;
using Microsoft.Extensions.Logging;

using Timer = System.Timers.Timer;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;

namespace Azure.ScheduledEvents
{
    public partial class ScheduledEventsCancellationTokenSource : IDisposable
    {
        private readonly Timer _t = new Timer(TimeSpan.FromSeconds(2).TotalMilliseconds);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ScheduledEventsClient _client;
        private readonly ILogger<ScheduledEventsCancellationTokenSource> logger;
        private readonly TimeSpan _noticePeriod = TimeSpan.FromMinutes(5);

        public ScheduledEventsCancellationTokenSource(ScheduledEventsClient client, ILogger<ScheduledEventsCancellationTokenSource> logger)
        {
            _client = client;
            this.logger = logger;
            _t.Elapsed += t_Elapsed;
            _t.Start();
        }

        public void Dispose()
        {
            _t?.Dispose();
        }

        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource.Token;
        }

        private async void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                _t.Stop();

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
            }
            finally
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _t.Start();
                }
            }
        }

        [LoggerMessage(Level = LogLevel.Error, Message = "Error retrieving ScheduledEvents")]
        public partial void LogErrorRetrievingScheduledEvents(Exception ex);
    }
}
