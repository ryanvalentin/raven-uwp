using Sentry.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sentry.Storage
{
    public interface IRavenStorageClient
    {
        Task<List<RavenPayload>> ListStoredExceptionsAsync();

        Task StoreExceptionAsync(RavenPayload payload);

        Task<RavenPayload> GetPayloadByIdAsync(string eventId);

        Task DeleteStoredExceptionAsync(string eventId);
    }
}
