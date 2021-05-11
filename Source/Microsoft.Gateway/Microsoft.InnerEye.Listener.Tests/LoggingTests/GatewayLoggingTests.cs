﻿namespace Microsoft.InnerEye.Listener.Tests.LoggingTests
{
    using System.Diagnostics;
    using Microsoft.InnerEye.Listener.Common.Providers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GatewayLoggingTests : BaseTestClass
    {
        [Timeout(60 * 1000)]
        [TestCategory("LoggingTests")]
        [Description("Tests sending a trace event with random characters does not throw exceptions.")]
        [TestMethod]
        public void TestTraceLogging1()
        {
            Trace.TraceError($"[{GetType().Name}] #$^&*(^Funky {new ConfigurationException("Something went really wrong.")}");
        }
    }
}