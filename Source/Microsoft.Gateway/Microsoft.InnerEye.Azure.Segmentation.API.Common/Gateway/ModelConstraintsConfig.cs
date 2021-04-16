namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Newtonsoft.Json;

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
        [JsonConstructor]
        public ModelConstraintsConfig(
            string modelId,
            ModelChannelConstraints[] channelConstraints,
            TagReplacement[] tagReplacements)
        {
            ModelId = modelId;
            ChannelConstraints = channelConstraints ?? throw new ArgumentNullException(nameof(channelConstraints));
            TagReplacements = tagReplacements ?? throw new ArgumentNullException(nameof(tagReplacements));
        }

        /// <summary>
        /// The identifier of the model we are constraining
        /// </summary>
        [Required]
        public string ModelId { get; }

        /// <summary>
        /// Constraints for each channel in the model.
        /// </summary>
        [Required]
        public ModelChannelConstraints[] ChannelConstraints { get; }

        /// <summary>
        /// Gets or sets the TagReplacements
        /// </summary>
        /// <value>
        /// The result structure set tags.
        /// </value>
        [Required]
        public TagReplacement[] TagReplacements { get; }

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
            hashCode = hashCode * -1521134295 + EqualityComparer<ModelChannelConstraints[]>.Default.GetHashCode(ChannelConstraints);
            hashCode = hashCode * -1521134295 + EqualityComparer<TagReplacement[]>.Default.GetHashCode(TagReplacements);
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
