/* Copyright 2014 Microsoft Corporation
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
#
*/

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Azure.ScheduledEvents
{
    /// <summary>
    /// Coordinates scheduled events retrieval across multiple processes using file-based caching
    /// and Named Mutex for coordination to reduce HTTP polling and stay within Azure rate limits
    /// </summary>
    public partial class ScheduledEventsCoordinator : IDisposable
    {
        private const string MutexName = "Azure.ScheduledEvents.GlobalLock";
        private static readonly TimeSpan LockCheckInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan HttpPollInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(30);
        
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SourceGenerationContext _sourceGenerationContext;
        private readonly ILogger<ScheduledEventsCoordinator> _logger;
        private readonly Uri _scheduledEventsEndpoint = new Uri("http://169.254.169.254/metadata/scheduledevents?api-version=2020-07-01");
        private readonly string _cacheFilePath;
        
        private Mutex _globalMutex;
        private bool _hasLock;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _coordinatorTask;

        public ScheduledEventsCoordinator(
            IHttpClientFactory httpClientFactory, 
            SourceGenerationContext sourceGenerationContext,
            ILogger<ScheduledEventsCoordinator> logger)
        {
            _httpClientFactory = httpClientFactory;
            _sourceGenerationContext = sourceGenerationContext;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
            _cacheFilePath = Path.Combine(Path.GetTempPath(), "azure-scheduled-events-cache.json");
            
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _globalMutex = new Mutex(false, MutexName);
                _hasLock = _globalMutex.WaitOne(TimeSpan.Zero);
                
                if (_hasLock)
                {
                    LogAcquiredGlobalLock();
                    _coordinatorTask = RunAsCoordinator(_cancellationTokenSource.Token);
                }
                else
                {
                    LogStartingAsClient();
                    _coordinatorTask = RunAsClient(_cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                LogInitializationError(ex);
                throw;
            }
        }

        public async Task<ScheduledEventsDocument?> GetScheduledEventsAsync()
        {
            if (_hasLock)
            {
                // We're the coordinator, we should have the latest data
                return await GetScheduledEventsDirectly();
            }
            else
            {
                // We're a client, try to read from cache first
                var cachedDocument = ReadFromCache();
                if (cachedDocument != null)
                {
                    return cachedDocument;
                }
                
                // Cache miss or expired, fallback to direct HTTP call
                LogCacheMissFallback();
                return await GetScheduledEventsDirectly();
            }
        }

        private ScheduledEventsDocument? ReadFromCache()
        {
            try
            {
                if (!File.Exists(_cacheFilePath))
                    return null;
                
                var fileInfo = new FileInfo(_cacheFilePath);
                if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc > CacheExpiry)
                {
                    LogCacheExpired();
                    return null;
                }
                
                var json = File.ReadAllText(_cacheFilePath);
                if (string.IsNullOrEmpty(json))
                    return null;
                    
                return JsonSerializer.Deserialize<ScheduledEventsDocument>(json, _sourceGenerationContext.ScheduledEventsDocument);
            }
            catch (Exception ex)
            {
                LogCacheReadError(ex);
                return null;
            }
        }

        private void WriteToCache(ScheduledEventsDocument? document)
        {
            try
            {
                var json = document != null 
                    ? JsonSerializer.Serialize(document, _sourceGenerationContext.ScheduledEventsDocument)
                    : "";
                    
                File.WriteAllText(_cacheFilePath, json);
            }
            catch (Exception ex)
            {
                LogCacheWriteError(ex);
            }
        }

        private async Task<ScheduledEventsDocument?> GetScheduledEventsDirectly()
        {
            using var webClient = _httpClientFactory.CreateClient();
            webClient.Timeout = TimeSpan.FromMinutes(5);
            webClient.DefaultRequestHeaders.Add("Metadata", "true");

            using var response = await webClient.GetAsync(_scheduledEventsEndpoint);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength == 0)
                return null;

            return await response.Content.ReadFromJsonAsync(_sourceGenerationContext.ScheduledEventsDocument);
        }

        private async Task RunAsCoordinator(CancellationToken cancellationToken)
        {
            LogStartingCoordinatorLoop();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Poll HTTP endpoint and update cache
                    var document = await GetScheduledEventsDirectly();
                    WriteToCache(document);
                    
                    await Task.Delay(HttpPollInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogCoordinatorError(ex);
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            
            LogCoordinatorLoopEnded();
        }

        private async Task RunAsClient(CancellationToken cancellationToken)
        {
            LogStartingClientLoop();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Periodically try to acquire the lock
                    if (_globalMutex.WaitOne(TimeSpan.Zero))
                    {
                        _hasLock = true;
                        LogPromotedToCoordinator();
                        
                        // Switch to coordinator mode
                        _coordinatorTask = RunAsCoordinator(cancellationToken);
                        return;
                    }
                    
                    await Task.Delay(LockCheckInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogClientError(ex);
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            
            LogClientLoopEnded();
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            
            try
            {
                _coordinatorTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            if (_hasLock)
            {
                try
                {
                    _globalMutex?.ReleaseMutex();
                }
                catch
                {
                    // Ignore mutex release errors
                }
            }
            
            _globalMutex?.Dispose();
            _cancellationTokenSource?.Dispose();
            
            // Clean up cache file on coordinator shutdown
            if (_hasLock)
            {
                try
                {
                    if (File.Exists(_cacheFilePath))
                        File.Delete(_cacheFilePath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "Acquired global lock - running as coordinator")]
        private partial void LogAcquiredGlobalLock();

        [LoggerMessage(Level = LogLevel.Information, Message = "Starting as client - will read from cache")]
        private partial void LogStartingAsClient();

        [LoggerMessage(Level = LogLevel.Information, Message = "Promoted from client to coordinator")]
        private partial void LogPromotedToCoordinator();

        [LoggerMessage(Level = LogLevel.Information, Message = "Starting coordinator polling loop")]
        private partial void LogStartingCoordinatorLoop();

        [LoggerMessage(Level = LogLevel.Information, Message = "Coordinator polling loop ended")]
        private partial void LogCoordinatorLoopEnded();

        [LoggerMessage(Level = LogLevel.Information, Message = "Starting client monitoring loop")]
        private partial void LogStartingClientLoop();

        [LoggerMessage(Level = LogLevel.Information, Message = "Client monitoring loop ended")]
        private partial void LogClientLoopEnded();

        [LoggerMessage(Level = LogLevel.Warning, Message = "Cache miss or expired, falling back to direct HTTP call")]
        private partial void LogCacheMissFallback();

        [LoggerMessage(Level = LogLevel.Debug, Message = "Cache expired")]
        private partial void LogCacheExpired();

        [LoggerMessage(Level = LogLevel.Error, Message = "Error during initialization")]
        private partial void LogInitializationError(Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error in coordinator loop")]
        private partial void LogCoordinatorError(Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error in client loop")]
        private partial void LogClientError(Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error reading from cache")]
        private partial void LogCacheReadError(Exception ex);

        [LoggerMessage(Level = LogLevel.Error, Message = "Error writing to cache")]
        private partial void LogCacheWriteError(Exception ex);
    }
}