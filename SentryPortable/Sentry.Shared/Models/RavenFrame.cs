using Newtonsoft.Json;

namespace Sentry.Models
{
    public class RavenFrame
    {
        [JsonProperty("filename", NullValueHandling = NullValueHandling.Ignore)]
        public string Filename { get; set; }

        [JsonProperty("function", NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }

        [JsonProperty("lineno", NullValueHandling = NullValueHandling.Ignore)]
        public int Line { get; set; }
    }
}
