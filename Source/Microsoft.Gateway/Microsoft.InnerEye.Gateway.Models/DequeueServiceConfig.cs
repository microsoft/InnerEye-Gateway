// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;

    /// <summary>
    /// The configuration for services based on dequeue message queues.
    /// </summary>
    public class DequeueServiceConfig : IEquatable<DequeueServiceConfig>
    {
        /// <summary>
        /// The default value for the maxium time a queue message will be attempted to be reprocessed.
        /// 7 days * 24 hours * 60 minutes * 60 seconds
        /// </summary>
        private const int DefaultMaximumQueueMessageAgeSeconds = 7 * 24 * 60 * 60;

        /// <summary>
        /// The default value for the maxium time a dead letter will sit on the dead letter queue.
        /// 30 minutes * 60 seconds
        /// </summary>
        private const int DefaultDeadLetterMoveFrequencySeconds = 30 * 60;

        /// <summary>
        /// The maximum number of times we will dequeue an item before it is removed from the queue
        /// and moved to the dead letter queue.
        /// </summary>
        public const int MaxDequeueCount = 1;

        /// <summary>
        /// The time the service base will wait when a queue is empty to continue code execution.
        /// If we want to stop the service in a reasonable time, we cannot block the main execution thread.
        /// However, we also do not want to keep attempting reads from the queue in a tight loop.
        /// </summary>
        public static readonly TimeSpan DequeueTimeout = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The dead letter queue path format.
        /// </summary>
        public const string DeadLetterQueuePathFormat = "{0}DeadLetter";

        /// <summary>
        /// Create a queue path for the dead letter queue from a dequeue queue path.
        /// </summary>
        /// <param name="dequeueQueuePath">Dequeue queue path.</param>
        /// <returns>Dead letter queue path.</returns>
        public static string DeadLetterQueuePath(string dequeueQueuePath) =>
            string.Format(CultureInfo.InvariantCulture, DeadLetterQueuePathFormat, dequeueQueuePath);

        /// <summary>
        /// MaximumQueueMessageAge in seconds.
        /// </summary>
        public double MaximumQueueMessageAgeSeconds => MaximumQueueMessageAge.TotalSeconds;

        /// <summary>
        /// The maximum age of a queue message. This is the longest amount of the time we will attempt to process the
        /// message until it is removed from all queues (including dead letter queues).
        [JsonIgnore]
        public TimeSpan MaximumQueueMessageAge { get; }

        /// <summary>
        /// DeadLetterMoveFrequency in seconds.
        /// </summary>
        public double DeadLetterMoveFrequencySeconds => DeadLetterMoveFrequency.TotalSeconds;

        /// <summary>
        /// The frequency dead letter messages will be moved from the dead letter queue to the dequeue queue.
        /// </summary>
        [JsonIgnore]
        public TimeSpan DeadLetterMoveFrequency { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DequeueServiceConfig"/> class.
        /// </summary>
        /// <param name="maximumQueueMessageAgeSeconds">The maximum age of a message in the queue before it is deleted and never re-processed in seconds.</param>
        /// <param name="deadLetterMoveFrequencySeconds">The frequency a dead letter message should be moved back to its original queue and re-processed in seconds.</param>
        public DequeueServiceConfig(
            double? maximumQueueMessageAgeSeconds,
            double? deadLetterMoveFrequencySeconds)
        {
            MaximumQueueMessageAge = TimeSpan.FromSeconds(maximumQueueMessageAgeSeconds ?? DefaultMaximumQueueMessageAgeSeconds);
            DeadLetterMoveFrequency = TimeSpan.FromSeconds(deadLetterMoveFrequencySeconds ?? DefaultDeadLetterMoveFrequencySeconds);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as DequeueServiceConfig);
        }

        /// <inheritdoc/>
        public bool Equals(DequeueServiceConfig other)
        {
            return other != null &&
                   MaximumQueueMessageAge.Equals(other.MaximumQueueMessageAge) &&
                   DeadLetterMoveFrequency.Equals(other.DeadLetterMoveFrequency);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 2144852902;
            hashCode = hashCode * -1521134295 + MaximumQueueMessageAge.GetHashCode();
            hashCode = hashCode * -1521134295 + DeadLetterMoveFrequency.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(DequeueServiceConfig left, DequeueServiceConfig right)
        {
            return EqualityComparer<DequeueServiceConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(DequeueServiceConfig left, DequeueServiceConfig right)
        {
            return !(left == right);
        }
    }
}
