namespace Microsoft.InnerEye.Listener.DataProvider.Models
{
    using System.Net;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The Dicom application entity
    /// </summary>
    public static class ApplicationEntityValidationHelpers
    {
        /// <summary>
        /// The maximum valid port number (inclusive).
        /// </summary>
        public const int MaximumPortNumber = 65535;

        /// <summary>
        /// The minimum valid port number (inclusive).
        /// </summary>
        public const int MinimumPortNumber = 3;

        /// <summary>
        /// The valid hostname regex.
        /// </summary>
        private const string ValidHostnameRegex = @"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$";

        /// <summary>
        /// Validates the application entity tile. The title must have a length of at least 1 and less than 17 characters long.
        /// </summary>
        /// <returns>If the application entity title is valid.</returns>
        public static bool ValidateTitle(string title)
        {
            return !string.IsNullOrWhiteSpace(title) && title.Length >= 1 && title.Length <= 16;
        }

        /// <summary>
        /// Validates the application entity port is in the correct range.
        /// </summary>
        /// <returns>If the port is within the correct range.</returns>
        public static bool ValidatePort(int port)
        {
            return (port >= MinimumPortNumber) && (port <= MaximumPortNumber);
        }

        /// <summary>
        /// Validates the IP address is correct and parses correctly.
        /// </summary>
        /// <param name="address">The IP address.</param>
        /// <returns>If the IP address is correct.</returns>
        public static bool ValidateIPAddress(string address)
        {
            // Check if this is a valid host name
            if (Regex.IsMatch(address, ValidHostnameRegex))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(address) || !IPAddress.TryParse(address, out var parsedAddress))
            {
                return false;
            }

            switch (parsedAddress.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    return true;
                case AddressFamily.InterNetworkV6:
                    // we have IPv6
                    return true;
                default:
                    // something unknown.
                    return false;
            }
        }
    }
}