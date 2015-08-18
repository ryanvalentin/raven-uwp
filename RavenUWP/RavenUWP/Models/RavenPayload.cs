using Newtonsoft.Json;
using RavenUWP.Helpers;
using System;
using System.Collections.Generic;

namespace RavenUWP.Models
{
    public class RavenPayload
    {
        [JsonProperty("event_id")]
        public string EventID { get; set; }

        [JsonProperty("project")]
        public string Project { get; set; }

        [JsonProperty("level", Required = Required.Always)]
        [JsonConverter(typeof(RavenLogLevelJsonConverter))]
        public RavenLogLevel? Level { get; set; }

        [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Timestamp { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("logger", NullValueHandling = NullValueHandling.Ignore)]
        public string Logger { get; set; }        

        [JsonProperty("culprit", NullValueHandling = NullValueHandling.Ignore)]
        public string Culprit { get; set; }

        [JsonProperty("stacktrace", NullValueHandling = NullValueHandling.Ignore)]
        public RavenStacktrace Stacktrace { get; set; }

        [JsonProperty("exception", NullValueHandling = NullValueHandling.Ignore)]
        public string Exception { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public RavenUser User { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Tags { get; set; }

        [JsonProperty("extra", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> Extra { get; set; }
    }
}
