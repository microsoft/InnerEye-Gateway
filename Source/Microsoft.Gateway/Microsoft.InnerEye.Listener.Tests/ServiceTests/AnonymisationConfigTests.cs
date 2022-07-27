// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Tests.ServiceTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Dicom;
    using Microsoft.InnerEye.Azure.Segmentation.Client;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AnonymisationConfigTests
    {
        private static readonly Uri testUri = new Uri("https://exampleuri.com");
        private const string testLicenseKey = "AAAAAAAAAAAAAAAAA";

        [TestCategory("AnonymisationConfig")]
        [Description("Test if the InnerEyeSegmentationClient parses a valid anonymisation config properly.")]
        [Timeout(60 * 100)]
        [TestMethod]
        public void ValidAnonymisationSettingsParseTest()
        {
            var validProtocolConfig = new Dictionary<string, IEnumerable<string>>
            {
                { "Keep", new [] {"Rows", "Columns"} },
                { "Hash", new [] {"SeriesInstanceUID"} }
            };

            using (var testClient = new InnerEyeSegmentationClient(testUri, validProtocolConfig, testLicenseKey))
            {
                var parsedDicomTags = testClient.GetSegmentationAnonymisationProtocol();
                Assert.AreEqual(Enumerable.Count(parsedDicomTags), 3);
            }

        }

        [TestCategory("AnonymisationConfig")]
        [Description("Test if the InnerEyeSegmentationClient fails properly when passed empty data.")]
        [Timeout(60 * 100)]
        [TestMethod]
        public void EmptyAnonymisationSettingsParseTest()
        {

            var emptyProtocolConfig = new Dictionary<string, IEnumerable<string>> { };

            using (var testClient = new InnerEyeSegmentationClient(testUri, emptyProtocolConfig, testLicenseKey))
            {

                Assert.ThrowsException<DicomDataException>(() => testClient.GetSegmentationAnonymisationProtocol());
            }
        }

        [TestCategory("AnonymisationConfig")]
        [Description("Test if the InnerEyeSegmentationClient fails properly when passed invalid tags.")]
        [Timeout(60 * 100)]
        [TestMethod]
        public void InvalidTagAnonymisationSettingsParseTest()
        {
            var invalidTagProtocolConfig = new Dictionary<string, IEnumerable<string>>
            {
                {"Keep", new [] {"THISISABADTAG", "SOISTHIS"} }
            };

            using (var testClient = new InnerEyeSegmentationClient(testUri, invalidTagProtocolConfig, testLicenseKey))
            {

                Assert.ThrowsException<KeyNotFoundException>(() => testClient.GetSegmentationAnonymisationProtocol());
            }
        }


        [TestCategory("AnonymisationConfig")]
        [Description("Test if the InnerEyeSegmentationClient fails properly when passed invalid anonymisation methods.")]
        [Timeout(60 * 100)]
        [TestMethod]
        public void InvalidMethodAnonymisationSettingsParseTest()
        {
            var invalidMethodProtocolConfig = new Dictionary<string, IEnumerable<string>>
            {
                {"Keep", new [] {"Columns", "Rows"} },
                {"Bad_Method", new [] {"Columns"} }
            };

            using (var testClient = new InnerEyeSegmentationClient(testUri, invalidMethodProtocolConfig, testLicenseKey))
            {

                Assert.ThrowsException<ArgumentException>(() => testClient.GetSegmentationAnonymisationProtocol());
            }
        }
    }
}
