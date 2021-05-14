// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Configurable constraints placed on data accepted by a model.
    /// </summary>
    public class ModelConstraintsConfig : IEquatable<ModelConstraintsConfig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelConstraintsConfig"/> class.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="channelConstraints">The channel constraints.</param>
        /// <param name="tagReplacements">The tag replacements.</param>
        /// <exception cref="ArgumentNullException">
        /// channelConstraints
        /// or
        /// tagReplacements
        /// </exception>
        public ModelConstraintsConfig(
            string modelId,
            IReadOnlyList<ModelChannelConstraints> channelConstraints,
            IReadOnlyList<TagReplacement> tagReplacements)
        {
            ModelId = modelId;
            ChannelConstraints = channelConstraints ?? throw new ArgumentNullException(nameof(channelConstraints));
            TagReplacements = tagReplacements ?? throw new ArgumentNullException(nameof(tagReplacements));
        }

        /// <summary>
        /// The identifier of the model we are constraining
        /// </summary>
        public string ModelId { get; }

        /// <summary>
        /// Constraints for each channel in the model.
        /// </summary>
        public IReadOnlyList<ModelChannelConstraints> ChannelConstraints { get; }

        /// <summary>
        /// Gets or sets the TagReplacements
        /// </summary>
        /// <value>
        /// The result structure set tags.
        /// </value>
        public IReadOnlyList<TagReplacement> TagReplacements { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ModelConstraintsConfig);
        }

        /// <inheritdoc/>
        public bool Equals(ModelConstraintsConfig other)
        {
            return other != null &&
                   ModelId == other.ModelId &&
                   ChannelConstraints.SequenceEqual(other.ChannelConstraints) &&
                   TagReplacements.SequenceEqual(other.TagReplacements);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = -1007173847;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ModelId);
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyList<ModelChannelConstraints>>.Default.GetHashCode(ChannelConstraints);
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyList<TagReplacement>>.Default.GetHashCode(TagReplacements);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(ModelConstraintsConfig left, ModelConstraintsConfig right)
        {
            return EqualityComparer<ModelConstraintsConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ModelConstraintsConfig left, ModelConstraintsConfig right)
        {
            return !(left == right);
        }
    }
}
