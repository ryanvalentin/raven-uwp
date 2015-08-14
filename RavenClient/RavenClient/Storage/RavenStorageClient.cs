using Newtonsoft.Json;
using RavenClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace RavenClient.Storage
{
    /// <summary>
    /// Stores and retrieves exceptions from local cache
    /// </summary>
    public class RavenStorageClient
    {
        public RavenStorageClient()
        {

        }

        private StorageFolder _temporaryStorage
        {
            get { return ApplicationData.Current.TemporaryFolder; }
        }

        public async Task StoreExceptionAsync(RavenJsonPayload payload)
        {
            StorageFolder folder = await GetRavenFolderAsync();

            StorageFile file = await folder.CreateFileAsync(payload.EventID, CreationCollisionOption.FailIfExists);

            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(payload));
        }

        public async Task DeleteStoredExceptionAsync(string eventId)
        {
            StorageFolder folder = await GetRavenFolderAsync();

            StorageFile file = await folder.GetFileAsync(eventId);

            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }

        public async Task<List<RavenJsonPayload>> ListStoredExceptionsAsync()
        {
            StorageFolder folder = await GetRavenFolderAsync();

            List<RavenJsonPayload> exceptions = new List<RavenJsonPayload>();
            List<StorageFile> invalidFiles = new List<StorageFile>();

            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                try
                {
                    string fileText = await FileIO.ReadTextAsync(file);

                    exceptions.Add(JsonConvert.DeserializeObject<RavenJsonPayload>(fileText));
                }
                catch (JsonException)
                {
                    invalidFiles.Add(file);
                }
            }

            foreach (var file in invalidFiles)
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);

            return exceptions;
        }

        private async Task<StorageFolder> GetRavenFolderAsync()
        {
            return await _temporaryStorage.CreateFolderAsync("raven", CreationCollisionOption.OpenIfExists);
        }
    }
}
