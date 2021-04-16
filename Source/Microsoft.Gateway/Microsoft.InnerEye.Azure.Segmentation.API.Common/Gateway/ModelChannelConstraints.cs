namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using Microsoft.InnerEye.DicomConstraints;

    /// <summary>
    /// Encodes a set of constraints for filtering and accepting a set of dicom datasets as appropriate
    /// for use with a particular channel in an ML model.
    /// </summary>
    /// <remarks>
    /// Series data is selected for a model channel using the following process:
    /// 1) A subset of images within the series that pass the ImageFilter constraints is selected.
    /// 2) The number of filtered images must  greater or equal to MinChannelImages and less than or equal to MaxChannelImages
    /// 3) All filtered images must pass the ChannelConstraints
    /// </remarks>
    public class ModelChannelConstraints : IEquatable<ModelChannelConstraints>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelChannelConstraints"/> class.
        /// </summary>
        /// <param name="channelID">The channel identifier.</param>
        /// <param name="imageFilter">The image filter constraints.</param>
        /// <param name="channelConstraints">The channel constraints.</param>
        /// <param name="minChannelImages">The minimum number of channel files required.</param>
        /// <param name="maxChannelImages">The maximum number of channel files allowed.</param>
        /// <exception cref="ArgumentNullException">
        /// channelID
        /// or
        /// imageFilter
        /// or
        /// channelConstraints
        /// </exception>
        public ModelChannelConstraints(
            string channelID, GroupConstraint imageFilter, GroupConstraint channelConstraints, int minChannelImages, int maxChannelImages)
        {
            ChannelID = channelID ?? throw new ArgumentNullException(nameof(channelID));
            ImageFilter = imageFilter ?? throw new ArgumentNullException(nameof(imageFilter));
            ChannelConstraints = channelConstraints ?? throw new ArgumentNullException(nameof(channelConstraints));
            MinChannelImages = minChannelImages;
            MaxChannelImages = maxChannelImages;
        }

        /// <summary>
        /// The channelID of the model we are constraining
        /// </summary>
        [Required]
        public string ChannelID { get; }

        /// <summary>
        /// Filter in DICOM files before applying the constraints. This selects a subset of images from the same series by
        /// filtering unwanted data e.g. extraneous sop classes.
        /// </summary>
        [Required]
        public GroupConstraint ImageFilter { get; }

        /// <summary>
        /// A set of filtered images must pass all constraints to be appropriate for this channel
        /// </summary>
        [Required]
        public GroupConstraint ChannelConstraints { get; }

        /// <summary>
        /// The minimum number of files required for this channel. Use 0 or less to impose no constraint. This is the inclusive
        /// lower bound for the number of filtered images.
        /// </summary>
        public int MinChannelImages { get; }

        /// <summary>
        /// The maximum number of files allowed for this channel. Use 0 or less to impose no maximum constraint. This is the
        /// inclusive upper bound for the number of filtered images.
        /// </summary>
        public int MaxChannelImages { get; }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as ModelChannelConstraints);
        }

        /// <inheritdoc/>
        public bool Equals(ModelChannelConstraints other)
        {
            return other != null &&
                   ChannelID == other.ChannelID &&
                   EqualityComparer<GroupConstraint>.Default.Equals(ImageFilter, other.ImageFilter) &&
                   EqualityComparer<GroupConstraint>.Default.Equals(ChannelConstraints, other.ChannelConstraints) &&
                   MinChannelImages == other.MinChannelImages &&
                   MaxChannelImages == other.MaxChannelImages;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 1238115311;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ChannelID);
            hashCode = hashCode * -1521134295 + EqualityComparer<GroupConstraint>.Default.GetHashCode(ImageFilter);
            hashCode = hashCode * -1521134295 + EqualityComparer<GroupConstraint>.Default.GetHashCode(ChannelConstraints);
            hashCode = hashCode * -1521134295 + MinChannelImages.GetHashCode();
            hashCode = hashCode * -1521134295 + MaxChannelImages.GetHashCode();
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(ModelChannelConstraints left, ModelChannelConstraints right)
        {
            return EqualityComparer<ModelChannelConstraints>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ModelChannelConstraints left, ModelChannelConstraints right)
        {
            return !(left == right);
        }
    }
}
