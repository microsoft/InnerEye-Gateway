namespace Microsoft.InnerEye.Azure.Segmentation.Client
{
    using System.Collections.Generic;

    using Microsoft.InnerEye.Azure.Segmentation.API.Common;

    /// <summary>
    /// The segmentation model.
    /// </summary>
    public class SegmentationModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentationModel"/> class.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="channelData">The channel data.</param>
        /// <param name="tagReplacements">The tag replacements.</param>
        public SegmentationModel(string modelId, IEnumerable<ChannelData> channelData, IEnumerable<TagReplacement> tagReplacements)
        {
            ModelId = modelId;
            ChannelData = channelData;
            TagReplacements = tagReplacements;
        }

        /// <summary>
        /// Gets the model identifier.
        /// </summary>
        /// <value>
        /// The model identifier.
        /// </value>
        public string ModelId { get; }

        /// <summary>
        /// Gets the channel data.
        /// </summary>
        /// <value>
        /// The channel data.
        /// </value>
        public IEnumerable<ChannelData> ChannelData { get; }

        /// <summary>
        /// Gets the tag replacements.
        /// </summary>
        /// <value>
        /// The tag replacements.
        /// </value>
        public IEnumerable<TagReplacement> TagReplacements { get; }
    }
}