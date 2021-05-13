namespace DICOMAnonymizer.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using DICOMAnonymizer.Tools;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SpecTests
    {
        private const string cProfPath = @"SpecXML\part15.xml";
        private const string serClassSpecPath = @"SpecXML\part04.xml";

        // We need { } around the namespace or else ':' can not be parsed as a XName
        private const string ns = @"{http://docbook.org/ns/docbook}";
        private readonly XDocument cProfXML = XDocument.Load(cProfPath);
        private readonly XDocument serClassSpecXML = XDocument.Load(serClassSpecPath);

        [TestMethod]
        public void SpecExists()
        {
            Assert.AreEqual(16022, cProfXML.Descendants().Count());
            Assert.AreEqual(32531, serClassSpecXML.Descendants().Count());
        }

        [TestMethod]
        public void CheckSpecXMLNamespace()
        {
            // We need { } around the name space or else ':' can not be parsed as a XName
            Assert.AreEqual(ns, "{" + cProfXML.Root.GetDefaultNamespace().NamespaceName + "}");
            Assert.AreEqual(ns, "{" + serClassSpecXML.Root.GetDefaultNamespace().NamespaceName + "}");
        }

        /// <summary>
        /// Check if our local Spec version is the same with the latest remote.
        /// If this test fails, updating: 1) the DICOM Spec file and 2) ConfidentialityProfile
        /// Tags is required.
        /// To check if we are consistent with the latest version, we use the remote
        /// ETag to avoid downloading the whole Spec.
        /// If the remote ETag is different, the test is marked as inconclusive and
        /// we download the whole remote file to check it against the local. If the
        /// files are the same, then our ETag needs to be updated. Else, the
        /// test should fail.
        /// </summary>
        [Ignore]
        [TestMethod]
        public void CheckConfidentialityProfileWithLatestOnline()
        {
            var specUrl = new Uri(@"http://dicom.nema.org/medical/dicom/current/source/docbook/part15/part15.xml", UriKind.Absolute);

            var request = WebRequest.Create(specUrl);
            request.Method = "HEAD";
            var etag = request.GetResponse().Headers["etag"].Replace(':', '-');

            if (!etag.Equals("\"7732d244af7d21-0\"", StringComparison.Ordinal))
            {
                using (var client = new WebClient())
                using (var reader = new StreamReader(client.OpenRead(specUrl)))
                {
                    var onlineSpec = reader.ReadToEnd();

                    var localSpec = File.ReadAllText(cProfPath);
                    Assert.AreEqual(localSpec, onlineSpec);
                }

                Assert.Inconclusive();
            }
        }

        [Ignore]
        [TestMethod]
        public void CheckServiceClassWithLatestOnline()
        {
            var specUrl = new Uri(@"http://dicom.nema.org/medical/dicom/current/source/docbook/part04/part04.xml", UriKind.Absolute);

            var request = WebRequest.Create(specUrl);
            request.Method = "HEAD";
            var etag = request.GetResponse().Headers["etag"].Replace(':', '-');

            if (!etag.Equals("\"bf03da46f7d21-0\"", StringComparison.Ordinal))
            {
                using (var client = new WebClient())
                using (var reader = new StreamReader(client.OpenRead(specUrl)))
                {
                    var onlineSpec = reader.ReadToEnd();

                    var localSpec = File.ReadAllText(serClassSpecPath);
                    Assert.AreEqual(localSpec, onlineSpec);
                }

                Assert.Inconclusive();
            }
        }

        [TestMethod]
        public void DefaultConfidentialityProfile()
        {
            Func<XAttribute, bool> byID = y => y.Name.LocalName.Equals("id", StringComparison.Ordinal);
            Func<XElement, string> tableName = y => y.Attributes().Where(byID).First().Value;

            var table = cProfXML.Descendants()
                .Where(x => x.Name.LocalName.Equals("table", StringComparison.Ordinal))
                .Where(x => tableName(x).Equals("table_E.1-1", StringComparison.Ordinal))
                .First();
            // Make sure our table has 14 columns
            Assert.AreEqual(14, table.Element(ns + "thead").Descendants().Where(x => x.Name.LocalName.Equals("para", StringComparison.Ordinal)).Count());

            var rows = table.Element(ns + "tbody").Elements(ns + "tr");
            Assert.AreEqual(278, rows.Count());

            var tagProf = new List<string>(276);
            var regProf = new List<string>(4);
            foreach (var row in rows)
            {
                var isTag = false;
                var sb = new StringBuilder();
                var clms = row.Elements().ToList();

                var parenthesis = new[] { '(', ')' };
                var tag = clms[1].Value.Trim(parenthesis);

                // tags might be dirty or might need to be converted into regex
                if (tag.Equals("50xx,xxxx", StringComparison.Ordinal))
                {
                    tag = "50[0-9A-F]{2},[0-9A-F]{4}";
                }
                else if (tag.Equals("60xx,4000", StringComparison.Ordinal))
                {
                    tag = "60[0-9A-F]{2},4000";
                }
                else if (tag.Equals("60xx,3000", StringComparison.Ordinal))
                {
                    tag = "60[0-9A-F]{2},3000";
                }
                else if (tag.Equals("gggg,eeee) where gggg is odd", StringComparison.Ordinal))
                {
                    tag = "[0-9A-F]{3}[13579BDF],[0-9A-F]{4}";
                }
                else
                {
                    if (tag.Contains(" "))
                    {
                        tag = tag.Replace(" ", string.Empty);
                    }

                    var r = new Regex("[0-9A-F]{4},[0-9A-F]{4}");
                    Assert.IsTrue(r.IsMatch(tag));
                    isTag = true;
                }
                sb.Append(tag);

                for (var i = 4; i < clms.Count; i++)
                {
                    sb.Append(";" + clms[i].Value);
                }

                (isTag ? tagProf : regProf).Add(sb.ToString());
            }

            tagProf.Sort();
            regProf.Sort();

            var rtagProf = new List<string>(ConfidentialityProfile.TagProfile);
            rtagProf.Sort();
            var rregProf = new List<string>(ConfidentialityProfile.RegexProfile);
            rregProf.Sort();

            // Check our Confidentiality Profile is in sync with the newly parsed Spec
            Assert.IsTrue(Enumerable.SequenceEqual(tagProf, rtagProf));
            Assert.IsTrue(Enumerable.SequenceEqual(regProf, rregProf));
        }

        [TestMethod]
        public void SOPClasses()
        {
            Func<XAttribute, bool> byID = y => y.Name.LocalName.Equals("id", StringComparison.Ordinal);
            Func<XElement, string> tableName = y => y.Attributes().Where(byID).First().Value;

            var table = serClassSpecXML.Descendants()
                .Where(x => x.Name.LocalName.Equals("table", StringComparison.Ordinal))
                .Where(x => tableName(x).Equals("table_B.5-1", StringComparison.Ordinal))
                .First();
            // Make sure our table has 3 columns
            Assert.AreEqual(3, table.Element(ns + "thead").Descendants().Where(x => x.Name.LocalName.Equals("para", StringComparison.Ordinal)).Count());

            var rows = table.Element(ns + "tbody").Elements(ns + "tr");
            Assert.AreEqual(129, rows.Count());

            var enumNames = Enum.GetValues(typeof(SOPClass)).Cast<SOPClass>().Select(e => e.ToString()).ToList();

            var specSOPNames = new List<string>(129);
            var specSOPCodes = new List<string>(129);
            foreach (var row in rows)
            {
                var name = row.Elements().ElementAt(0);
                var pad = "";
                if (name.Value.Length > 0 && char.IsDigit(name.Value[0]))
                {
                    pad = "_";
                }
                var cleanName = name.Value.Replace(" ", string.Empty).Replace("-", string.Empty).Replace("/", string.Empty);
                specSOPNames.Add(pad + cleanName);

                var code = row.Elements().ElementAt(1);
                specSOPCodes.Add(code.Value);
            }
            Assert.IsTrue(Enumerable.SequenceEqual(specSOPNames, enumNames));

            // Check conversions from name -> code, and vice versa
            for (var i = 0; i < specSOPNames.Count; i++)
            {
                var name = SOPClassFinder.SOPClassName(specSOPCodes[i]);
                Assert.AreEqual(specSOPNames[i], name.ToString());

                var classname = (SOPClass)Enum.Parse(typeof(SOPClass), specSOPNames[i]);
                var code = SOPClassFinder.SOPClassCode(classname);
                Assert.AreEqual(specSOPCodes[i], code);
            }

            // Check for unknown values
            var o1 = SOPClassFinder.SOPClassName("test");
            Assert.IsNull(o1);
            var o2 = SOPClassFinder.SOPClassCode((SOPClass)254);
            Assert.IsNull(o2);
        }
    }
}
