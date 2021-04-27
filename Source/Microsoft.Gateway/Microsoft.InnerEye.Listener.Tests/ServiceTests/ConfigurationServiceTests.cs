namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.Security.Authentication;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ConfigurationServiceTests : BaseTestClass
    {
        [TestCategory("ConfigurationService")]
        [Timeout(100 * 1000)]
        [Description("Starts the configuration service and throws an exception on start. Tests it stops correctly.")]
        [TestMethod]
        public void TestBadStart()
        {
            using (var configurationService = CreateConfigurationService(
                null,
                () =>
                {
                    throw new NotImplementedException();
                },
                CreateDownloadService(),
                CreateUploadService()))
            {
                var cancelRequested = false;

                configurationService.StopRequested += (s, e) =>
                {
                    configurationService.OnStop();
                    cancelRequested = true;
                };

                configurationService.Start();

                SpinWait.SpinUntil(() => cancelRequested, TimeSpan.FromSeconds(10));

                Assert.IsFalse(configurationService.IsExecutionThreadRunning);
            }
        }

        [TestCategory("ConfigurationService")]
        [Timeout(100 * 1000)]
        [Description("Starts the configuration service and throws an authentication exception on start. Tests it stops correctly.")]
        [TestMethod]
        public void TestBadProductKeyStart()
        {
            var processorSettings = TestGatewayProcessorConfigProvider.ProcessorSettings();
            var existingLicenseKey = processorSettings.LicenseKey;

            try
            {
                // Note that Visual Studio must be running as Administrator in order to set the environment variable,
                // otherwise there will be a SecurityException.
                TestGatewayProcessorConfigProvider.SetProcessorSettings(licenseKey: Guid.NewGuid().ToString());

                using (var configurationService = CreateConfigurationService(
                    null,
                    null,
                    CreateDownloadService(),
                    CreateUploadService()))
                {
                    configurationService.Start();

                    SpinWait.SpinUntil(() => !configurationService.IsExecutionThreadRunning, TimeSpan.FromSeconds(10));

                    Assert.IsFalse(configurationService.IsExecutionThreadRunning);
                }
            }
            finally
            {
                TestGatewayProcessorConfigProvider.SetProcessorSettings(licenseKey: existingLicenseKey);
            }
        }

        [TestCategory("ConfigurationService")]
        [Timeout(100 * 1000)]
        [Description("Starts the configuration service and then makes the segmentation client throw an authentication failure after start. Checks all services stop.")]
        [TestMethod]
        public void TestBadProductKeyAfterStart()
        {
            var client = GetMockInnerEyeSegmentationClient();

            // Create the services with multiple instances.
            using (var downloadService = CreateDownloadService(instances: 3))
            using (var uploadService = CreateUploadService(instances: 3))
            using (var configurationService = CreateConfigurationService(
                client,
                null,
                downloadService,
                uploadService))
            {
                var cancelRequested = false;

                configurationService.StopRequested += (s, e) =>
                {
                    configurationService.OnStop();
                    cancelRequested = true;
                };

                configurationService.Start();

                SpinWait.SpinUntil(() => configurationService.IsExecutionThreadRunning, TimeSpan.FromSeconds(10));
                SpinWait.SpinUntil(() => downloadService.IsExecutionThreadRunning, TimeSpan.FromSeconds(10));
                SpinWait.SpinUntil(() => uploadService.IsExecutionThreadRunning, TimeSpan.FromSeconds(10));

                // Check all services have started.
                Assert.IsTrue(configurationService.IsExecutionThreadRunning);
                Assert.IsTrue(downloadService.IsExecutionThreadRunning);
                Assert.IsTrue(uploadService.IsExecutionThreadRunning);

                client.PingException = new AuthenticationException();

                SpinWait.SpinUntil(() => cancelRequested, TimeSpan.FromSeconds(10));
                SpinWait.SpinUntil(() => !downloadService.IsExecutionThreadRunning, TimeSpan.FromSeconds(10));
                SpinWait.SpinUntil(() => !uploadService.IsExecutionThreadRunning, TimeSpan.FromSeconds(10));

                Assert.IsTrue(cancelRequested);
                Assert.IsFalse(downloadService.IsExecutionThreadRunning);
                Assert.IsFalse(uploadService.IsExecutionThreadRunning);
            }
        }
    }
}
