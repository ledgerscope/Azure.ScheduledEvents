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
        private readonly TimeSpan _noticePeriod = TimeSpan.FromMinutes(5);

        public ScheduledEventsCancellationTokenSource(ScheduledEventsClient client)
        {
            _client = client;

            _t.Elapsed += t_Elapsed;
            _t.Start();
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

                if (document != null)
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
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _t.Start();
                }
            }
        }
    }
}
