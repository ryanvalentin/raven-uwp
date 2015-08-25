using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Sentry.Models;
using Sentry.Storage;
using System;
using System.Threading.Tasks;

namespace Sentry.Tests
{
    [TestClass]
    public class RavenStorageTests
    {
        private static RavenStorageClient _storageClient { get; set; }

        [ClassInitialize]
        public static void RavenStorageTestsInit(TestContext context)
        {
            Dsn dsn = new Dsn(RavenConfig.DSN);
            RavenClient.InitializeAsync(dsn);

            _storageClient = new RavenStorageClient();
        }
        
        private async Task<RavenPayload> CreateAndStoreExceptionAsync()
        {
            Exception ex = new Exception("This is a test exception");
            var payload = await RavenClient.Instance.GeneratePayloadAsync(ex, RavenLogLevel.Error, null, null);
            await _storageClient.StoreExceptionAsync(payload);

            return payload;
        }

        [TestMethod]
        public async Task Test_Store_Exception()
        {
            var payload = await CreateAndStoreExceptionAsync();

            var storedException = await _storageClient.GetPayloadByIdAsync(payload.EventID);

            Assert.IsNotNull(storedException);
            Assert.AreEqual(payload.EventID, storedException.EventID);
        }

        [TestMethod]
        public async Task Test_List_Exceptions()
        {
            var payload1 = await CreateAndStoreExceptionAsync();
            var payload2 = await CreateAndStoreExceptionAsync();

            var storedExceptions = await _storageClient.ListStoredExceptionsAsync();

            Assert.IsTrue(storedExceptions.Count > 1);
            Assert.IsTrue(storedExceptions.Contains(payload1));
            Assert.IsTrue(storedExceptions.Contains(payload2));
        }

        [TestMethod]
        public async Task Test_Delete_Exception()
        {
            var payload = await CreateAndStoreExceptionAsync();

            await _storageClient.DeleteStoredExceptionAsync(payload.EventID);

            Assert.IsNull(await _storageClient.GetPayloadByIdAsync(payload.EventID));
        }
    }
}
