// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System.Globalization;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Thread safe hashing function
    /// </summary>
    public static class HashingFunctions
    {
        /// <summary>
        /// Hashes the identifier. NOTE SHA512Managed is NOT THREAD SAFE DO NOT MAKE IT STATIC
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static string HashID(string input, int length = 64)
        {
            using (var hashAlgorithm = new SHA512Managed())
            {
                var hashData = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
                var stringBuilder = new StringBuilder();

                // This is to to create hashes that are valid UIs, w/o leading zeros. Potential future improvements - different hashing algorithms depending on VR
                stringBuilder.Append('1');

                foreach (var hashedByte in hashData)
                {
                    stringBuilder.Append(hashedByte.ToString("d2", CultureInfo.InvariantCulture));
                }

                return stringBuilder.ToString().Substring(0, length);
            }
        }
    }
}
