# Azure Scheduled Events Cross-Process Coordination

This library now includes cross-process coordination to reduce HTTP polling and stay within Azure's rate limits (5 requests/second per VM).

## How It Works

### Process Coordination
- **Global Lock**: Uses a Named Mutex (`Azure.ScheduledEvents.GlobalLock`) for cross-process coordination
- **Coordinator Process**: The first process to acquire the lock becomes responsible for HTTP polling
- **Client Processes**: Other processes read from a shared cache file
- **Automatic Failover**: If the coordinator process dies, another process will take over

### File-Based Caching
- **Cache Location**: Temporary file at `%TEMP%\azure-scheduled-events-cache.json`
- **Cache Expiry**: 30 seconds (configurable)
- **Fallback**: If cache is stale or missing, processes fall back to direct HTTP calls

### Benefits
- **Reduced HTTP Calls**: From N processes making HTTP calls to only 1 process per VM
- **Rate Limit Compliance**: Stays within Azure's 5 requests/second limit
- **Backward Compatibility**: Existing code requires no changes
- **Fault Tolerance**: Automatic failover when coordinator process terminates

## Usage

The API remains unchanged. Simply use the existing service registration:

```csharp
var services = new ServiceCollection()
    .AddScheduledEventsClient()
    .BuildServiceProvider();

var client = services.GetRequiredService<ScheduledEventsClient>();
var events = await client.GetScheduledEvents();
```

## Implementation Details

### ScheduledEventsCoordinator
- Manages global lock acquisition and release
- Handles HTTP polling when coordinator (every 10 seconds)
- Manages file-based cache read/write operations
- Provides automatic failover through periodic lock checking (every 30 seconds)

### File Cache Format
The cache file contains JSON-serialized `ScheduledEventsDocument` data with timestamps for expiry checking.

### Error Handling
- Comprehensive logging for debugging coordination issues
- Graceful fallback to direct HTTP calls when coordination fails
- Automatic cleanup of resources and cache files on shutdown

## Testing

The implementation includes comprehensive unit tests:
- Coordinator instantiation and cleanup
- Multiple coordinator instance coordination
- Cache file operations
- HTTP timeout handling
- Failover scenarios

Run tests with:
```bash
dotnet test --filter "CoordinatorTests|CoordinatorDemoTest"
```