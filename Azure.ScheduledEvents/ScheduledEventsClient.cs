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

using Newtonsoft.Json;
using System;
using System.Net;

namespace Azure.ScheduledEvents
{
    public class ScheduledEventsClient
    {
        private readonly Uri scheduledEventsEndpoint;

        public ScheduledEventsClient(Uri scheduledEventsEndpoint)
        {
            this.scheduledEventsEndpoint = scheduledEventsEndpoint;
        }

        public ScheduledEventsClient() : this(new Uri("http://169.254.169.254/metadata/scheduledevents?api-version=2019-01-01"))
        {

        }

        /// <summary>
        /// Issues a get request to the scheduled events endpoint.
        ///
        /// For additional information on possible return status codes and headers,
        /// Please see here: "https://docs.microsoft.com/en-us/azure/virtual-machines/virtual-machines-scheduled-events"
        ///
        /// </summary>
        /// <returns>The Scheduled Events document</returns>
        public ScheduledEventsDocument GetScheduledEvents()
        {
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("Metadata", "true");
                var json = webClient.DownloadString(scheduledEventsEndpoint);

                var scheduledEventsDocument = JsonConvert.DeserializeObject<ScheduledEventsDocument>(json);

                return scheduledEventsDocument;
            }
        }

        /// <summary>
        /// Issues a post request to the scheduled events endpoint with the given json string
        ///
        /// For additional information on possible return status codes and headers,
        /// Please see here: "https://docs.microsoft.com/en-us/azure/virtual-machines/virtual-machines-scheduled-events"
        /// </summary>
        /// <param name="jsonPost">Json string with events to be approved</param>
        public void ExpediteScheduledEvents(string jsonPost)
        {
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("Content-Type", "application/json");
                webClient.UploadString(scheduledEventsEndpoint, jsonPost);
            }
        }
    }
}
