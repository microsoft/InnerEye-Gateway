namespace Microsoft.InnerEye.Listener.Tests.Helpers
{
    using System;
    using System.Runtime.InteropServices;

    public partial class MSMQHelpers
    {
        [DllImport("kernel32")]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Checks if MSMQ is installed. If the function returns false, please run the following command in an elevated PowerShell command prompt:
        /// Enable-WindowsOptionalFeature -Online -FeatureName MSMQ-Server -All
        /// </summary>
        /// <returns>If MSMQ installed.</returns>
        public static bool IsMSMQInstalled()
        {
            try
            {
                var handle = LoadLibrary("Mqrt.dll");

                if (handle != IntPtr.Zero && handle.ToInt32() != 0)
                {
                    FreeLibrary(handle);
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }
    }
}