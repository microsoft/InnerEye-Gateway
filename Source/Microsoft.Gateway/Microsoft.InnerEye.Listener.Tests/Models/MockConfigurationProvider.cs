namespace Microsoft.InnerEye.Listener.Tests.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Mock implementation of IConfigurationProvider<T>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MockConfigurationProvider<T>
    {
        /// <summary>
        /// Previous configuration, if there was a queue.
        /// </summary>
        private T _previousConfiguration;

        /// <summary>
        /// TestException to throw on GetConfiguration to mock failure.
        /// </summary>
        public Exception TestException { get; set; }

        /// <summary>
        /// Mock queue of configurations.
        /// </summary>
        public Queue<T> ConfigurationQueue { get; } = new Queue<T>();

        /// <inheritdoc/>
        public T GetConfiguration()
        {
            if (TestException != null)
            {
                throw TestException;
            }

            if (ConfigurationQueue.Count > 0)
            {
                _previousConfiguration = ConfigurationQueue.Dequeue();
            }

            return _previousConfiguration;
        }
    }
}
