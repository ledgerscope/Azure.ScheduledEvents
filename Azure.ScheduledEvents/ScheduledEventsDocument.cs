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
using System.Text.Json.Serialization;

namespace Azure.ScheduledEvents
{
    /// <summary>
    /// Represents the entire Scheduled Events document
    /// </summary>
    public class ScheduledEventsDocument
    {
        public int DocumentIncarnation;

        public ScheduledEvent[] Events { get; set; }
    }

    /// <summary>
    /// Represents an individual scheduled event
    /// See https://docs.microsoft.com/en-us/azure/virtual-machines/windows/scheduled-events for descriptions
    /// </summary>
    public class ScheduledEvent
    {
        public string EventId { get; set; }

        public string EventSource { get; set; }

        public string EventStatus { get; set; }

        public string EventType { get; set; }

        public string ResourceType { get; set; }

        public string[] Resources { get; set; }

        [JsonConverter(typeof(DateTimeConverterForCustomStandardFormatR))]
        public DateTime? NotBefore { get; set; }

        public int DurationInSeconds { get; set; }

        public string Description { get; set; }
    }
}
