using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Timer = System.Timers.Timer;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;
using System.Diagnostics;

namespace Azure.ScheduledEvents
{
    public class ScheduledEventsCancellationTokenSource// : IDisposable
    {
        private readonly Timer _t = new Timer(TimeSpan.FromSeconds(2).TotalMilliseconds);
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ScheduledEventsClient _client;
        private readonly TimeSpan _noticePeriod = TimeSpan.FromSeconds(30);

        public ScheduledEventsCancellationTokenSource(TimeSpan noticePeriod, ScheduledEventsClient client = null)
        {
            _noticePeriod = noticePeriod;
            _client = client ?? new ScheduledEventsClient();

            _t.Elapsed += t_Elapsed;
            _t.Start();
        }

        public CancellationToken GetCancellationToken()
        {
            return _cancellationTokenSource.Token;
        }

        private void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var document = _client.GetScheduledEvents();

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
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
