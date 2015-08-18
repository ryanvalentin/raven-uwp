using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        public void Test_Create_Valid_Dsn()
        {
            try
            {
                Dsn dsn = new Dsn("http://public:private@example.com/projectid");

                Assert.AreEqual("public", dsn.PublicKey);
                Assert.AreEqual("private", dsn.PrivateKey);
                Assert.AreEqual("projectid", dsn.ProjectID);
            }
            catch (ArgumentException ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void Test_Create_Invalid_Dsn()
        {
            Assert.ThrowsException<ArgumentException>(() => new Dsn("invalid_dsn"));
        }

        public void Test_Create_Null_Dsn()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new Dsn(null));
        }

        [TestMethod]
        public async Task Test_Create_Valid_Payload()
        {
            Exception ex = new Exception("This is a test exception");
            
            var payload = await RavenClient.Instance.GeneratePayloadAsync(ex, RavenLogLevel.Error, null, null);

            Assert.AreEqual("System.Exception: This is a test exception", payload.Message);
            Assert.AreEqual(RavenLogLevel.Error, payload.Level);
            
            // Test that some default tags have been set even though we aren't sending them
            Assert.IsNotNull(payload.Tags);
            foreach (var p in payload.Tags)
                Assert.IsFalse(String.IsNullOrWhiteSpace(p.Value), String.Format("{0}:{1}", p.Key, p.Value));            

            // Test that some extra parameters have been set even though we aren't sending them
            Assert.IsNotNull(payload.Extra);
            foreach (var e in payload.Extra)
                Assert.IsNotNull(e.Value, String.Format("{0}:{1}", e.Key, e.Value));
        }

        [TestMethod]
        public async Task Test_Create_Valid_Payload_With_Tags()
        {
            Exception ex = new Exception("This is a test exception");

            var tags = new Dictionary<string, string>()
            {
                { "test tag", "test value" }
            };

            var payload = await RavenClient.Instance.GeneratePayloadAsync(ex, RavenLogLevel.Error, tags, null);

            Assert.IsTrue(payload.Tags.ContainsKey("test tag"));
            Assert.AreEqual("test value", payload.Tags["test tag"]);
        }

        [TestMethod]
        public async Task Test_Create_Valid_Payload_With_Extra()
        {
            Exception ex = new Exception("This is a test exception");

            var extra = new Dictionary<string, object>()
            {
                { "test extra", true }
            };

            var payload = await RavenClient.Instance.GeneratePayloadAsync(ex, RavenLogLevel.Error, null, extra);

            Assert.IsTrue(payload.Extra.ContainsKey("test extra"));
            Assert.IsInstanceOfType(payload.Extra["test extra"], typeof(bool));
        }
    }
}
