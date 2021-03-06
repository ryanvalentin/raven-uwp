﻿using Newtonsoft.Json;
using Sentry.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;

[assembly: InternalsVisibleTo("Sentry.UWP.Tests")]

namespace Sentry.Storage
{
    /// <summary>
    /// Stores and retrieves exceptions from local cache
    /// </summary>
    public class RavenStorageClient : IRavenStorageClient
    {
        private const string _ravenFolderName = "raven";

        private StorageFolder _temporaryStorage
        {
            get { return ApplicationData.Current.TemporaryFolder; }
        }

        /// <summary>
        /// Gets a list of exceptions currently stored locally.
        /// </summary>
        /// <returns><see cref="List{RavenJsonPayload}"/> waiting to be sent.</returns>
        public async Task<List<RavenPayload>> ListStoredExceptionsAsync()
        {
            StorageFolder folder = await GetRavenFolderAsync();

            List<RavenPayload> exceptions = new List<RavenPayload>();
            List<StorageFile> invalidFiles = new List<StorageFile>();

            foreach (StorageFile file in await folder.GetFilesAsync())
            {
                try
                {
                    string fileText = await FileIO.ReadTextAsync(file);

                    RavenPayload payload = JsonConvert.DeserializeObject<RavenPayload>(fileText);

                    exceptions.Add(payload);
                }
                catch (JsonException)
                {
                    invalidFiles.Add(file);
                }
                catch (FileNotFoundException)
                {
                    continue;
                }
            }

            // Make sure we clean up any files here that don't match the current
            // JSON schema so we don't continue to try and read them.
            foreach (var file in invalidFiles)
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);

            return exceptions;
        }

        public async Task StoreExceptionAsync(RavenPayload payload)
        {
            if (payload == null)
                return;

            try
            {
                StorageFolder folder = await GetRavenFolderAsync();

                StorageFile file = await folder.CreateFileAsync(payload.EventID, CreationCollisionOption.ReplaceExisting);

                await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(payload));
            }
            catch (FileNotFoundException) { }
        }

        public async Task<RavenPayload> GetPayloadByIdAsync(string eventId)
        {
            try
            {
                StorageFolder folder = await GetRavenFolderAsync();

                StorageFile file = await folder.GetFileAsync(eventId);

                string fileText = await FileIO.ReadTextAsync(file);

                RavenPayload payload = JsonConvert.DeserializeObject<RavenPayload>(fileText);

                return payload;
            }
            catch (FileNotFoundException) { }

            return null;
        }

        public async Task DeleteStoredExceptionAsync(string eventId)
        {
            try
            {
                StorageFolder folder = await GetRavenFolderAsync();

                StorageFile file = await folder.GetFileAsync(eventId);

                await file?.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (FileNotFoundException) { }
        }

        private async Task<StorageFolder> GetRavenFolderAsync()
        {
            return await _temporaryStorage.CreateFolderAsync(_ravenFolderName, CreationCollisionOption.OpenIfExists);
        }
    }
}
