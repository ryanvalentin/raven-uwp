using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;

namespace RavenUWP.Tests
{
    [TestClass]
    public class RavenUnitTests
    {
        [ClassInitialize]
        public static void RavenTestsInit(TestContext context)
        {
            Dsn dsn = new Dsn(RavenConfig.DSN);
            RavenClient.InitializeAsync(dsn);
        }

        [TestMethod]
        public void TestCreateValidDsn()
        {
            try
            {
                Dsn dsn = new Dsn("http://PUBLICKEY:SECRETKEY@example.com/1");
            }
            catch (ArgumentException ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestCreateInvalidDsn()
        {
            Assert.ThrowsException<ArgumentException>(() => new Dsn("invalid_dsn"));
            Assert.ThrowsException<ArgumentNullException>(() => new Dsn(null));
        }

        [TestMethod]
        public async Task TestCreatePayload()
        {
            Exception ex = new Exception("This is a test exception");
            
            var payload = await RavenClient.Instance.GeneratePayloadAsync(ex, RavenLogLevel.Error, null, null);

            Assert.AreEqual("System.Exception: This is a test exception", payload.Message);
            Assert.AreEqual(RavenLogLevel.Error, payload.Level);

            // Test that some default tags have been set even though we aren't sending them
            Assert.IsNotNull(payload.Tags);

            // Test that some extra parameters have been set even though we aren't sending them
            Assert.IsNotNull(payload.Extra);
        }
    }
}
