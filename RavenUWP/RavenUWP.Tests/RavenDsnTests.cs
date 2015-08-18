using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;

namespace RavenUWP.Tests
{
    [TestClass]
    public class RavenDsnTests
    {
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
    }
}
