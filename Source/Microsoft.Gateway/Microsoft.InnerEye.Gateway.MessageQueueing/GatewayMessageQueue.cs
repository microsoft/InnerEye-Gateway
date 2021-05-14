// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Gateway.MessageQueueing
{
    using Microsoft.InnerEye.Gateway.MessageQueueing.Sqlite;

    /// <summary>
    /// Message queue constants and creator.
    /// </summary>
    public static class GatewayMessageQueue
    {
        /// <summary>
        /// The upload queue path.
        /// </summary>
        public const string UploadQueuePath = @".\Private$\ListenerUploadQueue";

        /// <summary>
        /// The download queue path.
        /// </summary>
        public const string DownloadQueuePath = @".\Private$\ListenerDownloadQueue";

        /// <summary>
        /// The push queue path.
        /// </summary>
        public const string PushQueuePath = @".\Private$\ListenerPushQueue";

        /// <summary>
        /// The delete queue path
        /// </summary>
        public const string DeleteQueuePath = @".\Private$\DeleteQueue";

        /// <summary>
        /// Gets a new message queue instance.
        /// </summary>
        /// <param name="path">The message queue path.</param>
        /// <returns>The message queue interface.</returns>
        public static IMessageQueue Get(string path) => new SqliteMessageQueue(path);
    }
}