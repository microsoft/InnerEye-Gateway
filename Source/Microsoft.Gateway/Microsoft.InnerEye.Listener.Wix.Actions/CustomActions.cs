namespace Microsoft.InnerEye.Listener.Wix.Actions
{
    using System;
#if DEBUG
    using System.Diagnostics;
#endif
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.InnerEye.Gateway.Models;
    using Microsoft.InnerEye.Listener.Common.Providers;

    /// <summary>
    /// The collection of custom actions run by the WiX installer.
    /// </summary>
    public static class CustomActions
    {
        /// <summary>
        /// The command line argument to silent install.
        /// </summary>
        private const string UILevelCustomActionKey = "UILevel";

        /// <summary>
        /// Gets the install path.
        /// </summary>
        /// <value>
        /// The install path.
        /// </value>
        private static string InstallPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft InnerEye Gateway");

        /// <summary>
        /// Gets the config folder path.
        /// </summary>
        /// <value>
        /// The config folder path.
        /// </value>
        private static string ConfigInstallDirectory => Path.Combine(InstallPath, "Config");

        /// <summary>
        /// The pre-install custom action.
        /// Asks the user for a license-key and validates it before continuing with the install.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>The action result.</returns>
        [CustomAction]
        public static ActionResult ValidateProductKey(Session session)
        {
#if DEBUG
            Debugger.Launch();
#endif

            // Make sure that the applications are run as services.
            var gatewayProcessorConfigProvider = new GatewayProcessorConfigProvider(
                null,
                ConfigInstallDirectory);
            gatewayProcessorConfigProvider.SetRunAsConsole(false);

            var gatewayReceiveConfigProvider = new GatewayReceiveConfigProvider(
                null,
                ConfigInstallDirectory);
            gatewayReceiveConfigProvider.SetRunAsConsole(false);

            // Check if the installer is running unattended - lets skip the UI if true
            if (session.CustomActionData[UILevelCustomActionKey] == "2")
            {
                return ActionResult.Success;
            }


            var processorSettings = gatewayProcessorConfigProvider.ProcessorSettings();

#pragma warning disable CA5364 // Do Not Use Deprecated Security Protocols
#pragma warning disable CA5386 // Avoid hardcoding SecurityProtocolType value
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#pragma warning restore CA5386 // Avoid hardcoding SecurityProtocolType value
#pragma warning restore CA5364 // Do Not Use Deprecated Security Protocols

            // First time install so lets display a form to grab the license key.
            DialogResult licenseKeyDialogResult = DialogResult.No;
            // TODO FIX INSTALLER
            using (var form = new LicenseKeyForm(processorSettings))
            {
                licenseKeyDialogResult = form.ShowDialog();
            }

            switch (licenseKeyDialogResult)
            {
                case DialogResult.Cancel:
                    return ActionResult.UserExit;
                case DialogResult.No:
                    return ActionResult.NotExecuted;
                default:
                    return ActionResult.Success;
            }
        }

        /// <summary>
        /// Validates the license key using the InnerEye segmentation client.
        /// </summary>
        /// <param name="processorSettings">Processor settings.</param>
        /// <param name="licenseKey">The license key to validate.</param>
        /// <returns>If valid and text to display with the validation result.</returns>
        internal static async Task<(bool Result, string ValidationText)> ValidateLicenseKeyAsync(ProcessorSettings processorSettings, string licenseKey)
        {
            var validationText = string.Empty;
            var existingLicenseKey = Environment.GetEnvironmentVariable(processorSettings.LicenseKeyEnvVar, EnvironmentVariableTarget.Machine);

            try
            {
                // Update the settings for the Gateway.
                Environment.SetEnvironmentVariable(processorSettings.LicenseKeyEnvVar, licenseKey, EnvironmentVariableTarget.Machine);

                using (var segmentationClient = new InnerEyeSegmentationClient(processorSettings.InferenceUri, processorSettings.LicenseKeyEnvVar, null))
                {
                    await segmentationClient.PingAsync();
                }

                return (true, validationText);
            }
            catch (HttpRequestException)
            {
                validationText = "Failed to connect to the internet";
                // Restore the previous environment variable
                Environment.SetEnvironmentVariable(processorSettings.LicenseKeyEnvVar, existingLicenseKey, EnvironmentVariableTarget.Machine);
            }
            catch (Exception)
            {
                validationText = "Invalid product key";
                // Restore the previous environment variable
                Environment.SetEnvironmentVariable(processorSettings.LicenseKeyEnvVar, existingLicenseKey, EnvironmentVariableTarget.Machine);
            }

            return (false, validationText);
        }
    }
}
