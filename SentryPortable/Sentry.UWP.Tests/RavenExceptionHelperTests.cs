using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Sentry.Helpers;
using Sentry.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sentry.Tests
{
    [TestClass]
    public class RavenExceptionHelperTests
    {
        [TestMethod]
        public void Test_Stacktrace_Parser()
        {
            string stacktrace = @"at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
at System.Runtime.CompilerServices.TaskAwaiter`1.GetResult()";

            IEnumerable<RavenFrame> frames = stacktrace.ParseStacktraceString();

            Assert.AreEqual(3, frames.Count());
            Assert.AreEqual("GetResult()", frames.Last().Method);
            Assert.AreEqual("System.Runtime.CompilerServices.TaskAwaiter`1", frames.Last().Filename);
        }

        public void Test_Exception_Enumerator()
        {
            InvalidOperationException innerEx = new InvalidOperationException("This is an inner exception");
            Exception ex = new Exception("This is an outer exception.", innerEx);

            IEnumerable<RavenException> exceptions = ex.EnumerateAllExceptions();

            Assert.AreEqual(2, exceptions.Count());
            Assert.AreEqual(typeof(InvalidOperationException), exceptions.Last());
        }
    }
}
