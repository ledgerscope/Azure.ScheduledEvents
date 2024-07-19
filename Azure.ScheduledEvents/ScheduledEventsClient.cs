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
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Azure.ScheduledEvents
{
    public class ScheduledEventsClient
    {
        private readonly SourceGenerationContext sourceGenerationContext;

        private readonly IHttpClientFactory httpClientFactory;
        private readonly Uri scheduledEventsEndpoint = new Uri("http://169.254.169.254/metadata/scheduledevents?api-version=2020-07-01");

        public ScheduledEventsClient(IHttpClientFactory httpClientFactory, SourceGenerationContext sourceGenerationContext)
        {
            this.httpClientFactory = httpClientFactory;
            this.sourceGenerationContext = sourceGenerationContext;
        }

        /// <summary>
        /// Issues a get request to the scheduled events endpoint.
        ///
        /// For additional information on possible return status codes and headers,
        /// Please see here: "https://docs.microsoft.com/en-us/azure/virtual-machines/virtual-machines-scheduled-events"
        ///
        /// </summary>
        /// <returns>The Scheduled Events document</returns>
        public async Task<ScheduledEventsDocument?> GetScheduledEvents()
        {
            using var webClient = httpClientFactory.CreateClient();

            webClient.Timeout = TimeSpan.FromMinutes(5); //First request reckons it can take 2 minutes
            webClient.DefaultRequestHeaders.Add("Metadata", "true");

            using var response = await webClient.GetAsync(scheduledEventsEndpoint);

            response.EnsureSuccessStatusCode();
            
            using var content = response.Content;

            if (response.Content.Headers.ContentLength == 0)
            {
                return null;
            }
         
            var scheduledEventsDocument = await content.ReadFromJsonAsync(sourceGenerationContext.ScheduledEventsDocument);
            return scheduledEventsDocument;
        }

        /// <summary>
        /// Issues a post request to the scheduled events endpoint with the given json string
        ///
        /// For additional information on possible return status codes and headers,
        /// Please see here: "https://docs.microsoft.com/en-us/azure/virtual-machines/virtual-machines-scheduled-events"
        /// </summary>
        /// <param name="jsonPost">Json string with events to be approved</param>
        public async Task ExpediteScheduledEvents(string jsonPost)
        {
            using var webClient = httpClientFactory.CreateClient();

            webClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            await webClient.PostAsync(scheduledEventsEndpoint, new StringContent(jsonPost));
        }
    }
}
