// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Listener.Tests.Common.Documentation
{
    using System;

    /// <summary>
    /// The test result model.
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// The verification document header1
        /// </summary>
        public static readonly string[] VerificationDocumentHeaders = new[]
        {
            "# Microsoft Radiomcs: Gateway Test Results",
            string.Empty,
            "Test|Test Description|Test Result|Test Date Time|Machine Name",
            "------------|------------|------------|-------------|-------------",
        };

        /// <summary>
        /// The divider
        /// </summary>
        private const char Divider = '|';

        /// <summary>
        /// Initializes a new instance of the <see cref="Documentation.TestResult"/> class.
        /// </summary>
        /// <param name="name">Name of the test.</param>
        /// <param name="category">The test category.</param>
        /// <param name="description">The test description.</param>
        /// <param name="result">The test result.</param>
        /// <param name="resultDateTime">The test date time.</param>
        /// <param name="machineName">Name of the machine.</param>
        /// <param name="testClass">The test class.</param>
        public TestResult(
            string name,
            string category,
            string description,
            string result,
            DateTime resultDateTime,
            string machineName,
            string testClass)
        {
            Name = name;
            Category = category;
            Description = description;
            Result = result;
            ResultDateTime = resultDateTime;
            MachineName = machineName;
            TestClass = testClass;
        }

        /// <summary>
        /// Gets or sets the name of the test.
        /// </summary>
        /// <value>
        /// The name of the test.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the test category.
        /// </summary>
        /// <value>
        /// The test category.
        /// </value>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the test description.
        /// </summary>
        /// <value>
        /// The test description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the test result.
        /// </summary>
        /// <value>
        /// The test result.
        /// </value>
        public string Result { get; set; }

        /// <summary>
        /// Gets or sets the test date time.
        /// </summary>
        /// <value>
        /// The test date time.
        /// </value>
        public DateTime ResultDateTime { get; set; }

        /// <summary>
        /// Gets or sets the name of the machine.
        /// </summary>
        /// <value>
        /// The name of the machine.
        /// </value>
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the test class.
        /// </summary>
        /// <value>
        /// The test class.
        /// </value>
        public string TestClass { get; set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"Name: {Name}<br/>Class: {TestClass}<br/>Category: {Category}{Divider}{Description}{Divider}{Result}{Divider}{ResultDateTime}{Divider}{MachineName}";
        }
    }
}