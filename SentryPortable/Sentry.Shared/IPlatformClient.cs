using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sentry
{
    public interface IPlatformClient
    {
        Task<IDictionary<string, string>> AppendPlatformTagsAsync(IDictionary<string, string> tags);

        IDictionary<string, object> AppendPlatformExtra(IDictionary<string, object> extra);

        string GetPlatformUserAgent();

        string PlatformTag { get; }
    }
}
