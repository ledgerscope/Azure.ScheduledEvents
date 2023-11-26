using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Azure.ScheduledEvents
{
    [JsonSourceGenerationOptions(WriteIndented = true)]

    [JsonSerializable(typeof(ScheduledEventsDocument))]
    [JsonSerializable(typeof(ScheduledEvent))]

    public partial class SourceGenerationContext : JsonSerializerContext
    { }
}
