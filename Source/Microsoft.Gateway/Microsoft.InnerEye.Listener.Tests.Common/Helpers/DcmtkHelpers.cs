namespace Microsoft.InnerEye.Listener.Tests.Common.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public enum ScuProfile
    {
        LEImplicitCT,
        LEExplicitCT,
        MixedUnsupportedMultipleCT,
        BEExplicitCT,
        AllSupportedMultipleCT,
        AllSupportedMultipleRTCT,
        RLECT,
        JPEGLosslessCT,
        JPEGLosslessNonHierarchical14CT,
        JPEGLSLosslessCT,
        MixedStandardSingleCT,
        LEImplicitRTCT,
        LEExplicitRTCT,
        BEExplicitRTCT,
    }

    public static class DcmtkHelpers
    {
        private static string StoreSCUPath { get; } = "Assets\\storescu.exe";

        /// <summary>
        /// Sends an entire folder using the DCMTK Store SCU function.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="port">The port number of the receiver.</param>
        /// <param name="scuProfile">The SCU profile.</param>
        /// <param name="testContext">The tesxt context for logging information.</param>
        /// <param name="scanDirectories">If we should scan sub-directories for files.</param>
        /// <param name="waitForExit">If we should wait for the send to complete.</param>
        /// <param name="abort">If we should abort.</param>
        /// <param name="applicationEntityTitle">The application entity title.</param>
        /// <param name="calledAETitle">The called application entity title.</param>
        /// <param name="hostIP">The host IP.</param>
        /// <returns>The result for the Store SCU function. This string will be empty if it completed succesfully.</returns>
        public static string SendFolderUsingDCMTK(string path, int port, ScuProfile scuProfile, TestContext testContext, bool scanDirectories = true, bool waitForExit = true, bool abort = false, string applicationEntityTitle = "STORESCU", string calledAETitle = "ANY-SCP", string hostIP = "127.0.0.1")
        {
            var directoryPath = path;

            if (!directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                directoryPath += Path.DirectorySeparatorChar;
            }

            // Make sure the directory path ends with double / so it is not escaped
            directoryPath += Path.DirectorySeparatorChar;

            Assert.IsTrue(new DirectoryInfo(directoryPath).Exists, $"[DCMTK StoreSCU] Directory {directoryPath} does not exist");

            return SendUsingDCMTK(directoryPath, port, scuProfile, testContext, scanDirectories, waitForExit, abort, applicationEntityTitle, calledAETitle, hostIP);
        }

        /// <summary>
        /// Sends a Dicom file using the DCMTK Store SCU function.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="port">The port number of the receiver.</param>
        /// <param name="scuProfile">The SCU profile.</param>
        /// <param name="testContext">The tesxt context for logging information.</param>
        /// <param name="waitForExit">If we should wait for the send to complete.</param>
        /// <param name="abort">If we should abort.</param>
        /// <param name="applicationEntityTitle">The application entity title.</param>
        /// <param name="calledAETitle">The called application entity title.</param>
        /// <param name="hostIP">The host IP.</param>
        /// <returns>The result for the Store SCU function. This string will be empty if it completed succesfully.</returns>
        public static string SendFileUsingDCMTK(string path, int port, ScuProfile scuProfile, TestContext testContext, bool waitForExit = true, bool abort = false, string applicationEntityTitle = "STORESCU", string calledAETitle = "ANY-SCP", string hostIP = "127.0.0.1")
        {
            var filePath = path;

            if (filePath.StartsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                var currentExecutionLocation = (new DirectoryInfo(Assembly.GetExecutingAssembly().Location)).Parent.FullName;
                filePath = currentExecutionLocation + filePath;
            }

            Assert.IsTrue(new FileInfo(filePath).Exists, $"[DCMTK StoreSCU] File {filePath} does not exist");

            return SendUsingDCMTK(filePath, port, scuProfile, testContext, false, waitForExit, abort, applicationEntityTitle, calledAETitle, hostIP);
        }

        private static string SendUsingDCMTK(string path, int port, ScuProfile scuProfile, TestContext testContext, bool scanDirectories, bool waitForExit, bool abort, string applicationEntityTitle, string calledAETitle, string hostIP)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(path));

            var currentExecutionLocation = (new DirectoryInfo(Assembly.GetExecutingAssembly().Location)).Parent.FullName;

            Assert.IsNotNull(StoreSCUPath, "storescu.exe not found on system PATH");
            var fileName = StoreSCUPath;

            testContext?.WriteLine($"Launching DCMTK StoreSCU from: {fileName}");

            var logLevel = "-ll error";
            var sd = scanDirectories ? " +sd +r +sp \"*.dcm\"" : "";
            var abortS = abort ? " --abort" : "";
            var configPath = Path.Combine(currentExecutionLocation, "Assets\\SCU.cfg");
            var scuProfileString = scuProfile.ToString();

            Assert.IsTrue(new FileInfo(configPath).Exists, $"[DCMTK StoreSCU] The config path is incorrect: {configPath}");

            testContext?.WriteLine($"Sending file via DCMTK StoreSCU from: {path}");

            var process = new Process();

            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = $"{logLevel}{abortS}{sd} -aec {calledAETitle} -aet {applicationEntityTitle} -xf \"{configPath}\" {scuProfileString} {hostIP} {port} \"{path}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            testContext?.WriteLine($"DCMTK StoreSCU start arguments: {process.StartInfo.Arguments}");

            process.Start();

            if (waitForExit)
            {
                var stdOut = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return stdOut;
            }

            return string.Empty;
        }
    }
}