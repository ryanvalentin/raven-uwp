using Newtonsoft.Json;
using RavenClient.Helpers;
using System;
using System.Collections.Generic;

namespace RavenClient.Models
{
    public class RavenJsonPayload
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
        public RavenJsonStacktrace Stacktrace { get; set; }

        [JsonProperty("exception", NullValueHandling = NullValueHandling.Ignore)]
        public string Exception { get; set; }

        [JsonProperty("user", NullValueHandling = NullValueHandling.Ignore)]
        public string User { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, string> Tags { get; set; }

        [JsonProperty("extra", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, object> Extra { get; set; }
    }

    public class RavenJsonStacktrace
    {
        [JsonProperty("frames")]
        public List<RavenJsonFrame> Frames { get; set; }
    }

    public class RavenJsonFrame
    {
        [JsonProperty("filename", NullValueHandling = NullValueHandling.Ignore)]
        public string Filename { get; set; }

        [JsonProperty("function", NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }

        [JsonProperty("lineno", NullValueHandling = NullValueHandling.Ignore)]
        public int Line { get; set; }
    }
}
