namespace Microsoft.InnerEye.Listener.Tests.Models
{
    using System;

    /// <summary>
    /// Mock returning configuration, but sometimes throwing an exception.
    /// </summary>
    /// <typeparam name="T">Configuration type.</typeparam>
    public class MockConfigurationProvider<T>
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly T _configuration;

        /// <summary>
        /// TestException to throw on GetConfiguration to mock failure.
        /// </summary>
        public Exception TestException { get; set; }

        /// <summary>
        /// Initialize a new instance of the.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public MockConfigurationProvider(T configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Return configuration or throw an exception.
        /// </summary>
        /// <returns>Configuration.</returns>
        public T Configuration()
        {
            if (TestException != null)
            {
                throw TestException;
            }

            return _configuration;
        }
    }
}
