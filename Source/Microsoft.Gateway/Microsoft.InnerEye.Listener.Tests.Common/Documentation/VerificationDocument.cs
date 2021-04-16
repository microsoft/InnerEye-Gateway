namespace Microsoft.InnerEye.Listener.Tests.Common.Documentation
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Creates a document from the current test context.
    /// </summary>
    public static class VerificationDocument
    {
        /// <summary>
        /// Appends the test result to the file path.
        /// </summary>
        /// <param name="path">The document file path.</param>
        /// <param name="testContext">The current test context.</param>
        public static void AppendTestResult(string path, TestContext testContext)
        {
            var testResult = CreateTestResult(testContext).ToString();

            if (!File.Exists(path))
            {
                TryWriteLines(path, 1, TestResult.VerificationDocumentHeaders.Concat(new[] { testResult }).ToArray());
                testContext.WriteLine($"Created verification document: {path}. Result: {testResult}");
            }
            else
            {
                TryWriteLines(path, 5, testResult);
                testContext.WriteLine($"Updated verification document: {path}. Result: {testResult}");
            }

            testContext.AddResultFile(path);
        }

        /// <summary>
        /// Tries to write the lines. Sometimes the file might still be use so we wait between retries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="retryCount">The number of times to retry.</param>
        /// <param name="lines">The lines.</param>
        private static void TryWriteLines(string path, int retryCount = 5, params string[] lines)
        {
            try
            {
                File.AppendAllLines(path, lines);
            }
            catch
            {
                if (retryCount >= 0)
                {
                    Thread.Sleep(1000);
                    TryWriteLines(path, retryCount - 1, lines);
                }
            }
        }

        /// <summary>
        /// Creates the test result from the current test context.
        /// </summary>
        /// <param name="testContext">The test context.</param>
        /// <returns>The test result.</returns>
        private static TestResult CreateTestResult(TestContext testContext)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                var classType = assembly.GetTypes().FirstOrDefault(x => x.FullName == testContext.FullyQualifiedTestClassName);

                if (classType != null)
                {
                    var method = classType.GetMethod(testContext.TestName);

                    var categories = method.GetCustomAttribute<TestCategoryAttribute>(true);
                    var description = method.GetCustomAttributes<DescriptionAttribute>(true);

                    return new TestResult(
                        testContext.TestName,
                        categories == null ? string.Empty : string.Join(",", categories.TestCategories.Select(x => x)),
                        description == null ? string.Empty : string.Join(",", description.Select(x => x.Description)),
                        testContext.CurrentTestOutcome.ToString(),
                        DateTime.UtcNow,
                        Environment.MachineName,
                        classType.Name);
                }
            }

            return null;
        }
    }
}