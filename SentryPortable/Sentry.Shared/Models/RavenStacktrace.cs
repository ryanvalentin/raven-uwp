using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sentry.Models
{
    public class RavenStacktrace
    {
        [JsonProperty("frames")]
        public List<RavenFrame> Frames { get; set; }
    }
}
