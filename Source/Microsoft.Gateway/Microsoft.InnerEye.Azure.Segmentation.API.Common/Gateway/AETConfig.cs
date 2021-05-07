namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// AET configuration
    /// </summary>
    public class AETConfig : IEquatable<AETConfig>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AETConfig"/> class.
        /// </summary>
        /// <param name="aetConfigType">Type of application entity configuration.</param>
        /// <param name="modelsConfig">The models configuration required if it is not feedback</param>
        /// <exception cref="ArgumentNullException">
        /// Model configuration null
        /// </exception>
        /// <exception cref="ArgumentException">
        /// calledAET
        /// or
        /// modelConfig
        /// </exception>
        public AETConfig(
            AETConfigType aetConfigType,
            IReadOnlyList<ModelConstraintsConfig> modelsConfig)
        {
            AETConfigType = aetConfigType;

            if (NeedsModelConfig(aetConfigType))
            {
                ModelsConfig = modelsConfig ?? throw new ArgumentNullException(nameof(modelsConfig));

                if (!ModelsConfig.Any())
                {
                    throw new ArgumentException("You must specify at least 1 ModelConstraintConfig", nameof(modelsConfig));
                }
            }

            // If this is not a model AET config type the models config should be null
            if (!NeedsModelConfig(aetConfigType) && modelsConfig != null)
            {
                throw new ArgumentException("This config type does not require a ModelConstraintsConfig", nameof(modelsConfig));
            }
        }

        /// <summary>
        /// Clone this into a new instance of the <see cref="AETConfig"/> class, optionally replacing some properties.
        /// </summary>
        /// <param name="aetConfigType">Optional new AETConfigType.</param>
        /// <param name="modelsConfig">Optional new ModelConstraintsConfig[].</param>
        /// <returns>New AETConfig.</returns>
        public AETConfig With(
            AETConfigType? aetConfigType = null,
            ModelConstraintsConfig[] modelsConfig = null) =>
                new AETConfig(
                    aetConfigType ?? AETConfigType,
                    modelsConfig ?? ModelsConfig);

        /// <summary>
        /// Gets the type of the aet configuration.
        /// </summary>
        /// <value>
        /// The type of the aet configuration.
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public AETConfigType AETConfigType { get; }

        /// <summary>
        /// If not IsFeedbackAET - then the models configured for this AE, or null otherwise
        /// </summary>
        public IReadOnlyList<ModelConstraintsConfig> ModelsConfig { get; }

        /// <summary>
        /// Needs a model configuration
        /// </summary>
        public static bool NeedsModelConfig(AETConfigType aetConfigType) =>
            aetConfigType == AETConfigType.Model || aetConfigType == AETConfigType.ModelWithResultDryRun;

        /// <summary>
        /// Configuration needs a destination endpoint
        /// </summary>
        public static bool NeedsEndpoint(AETConfigType aetConfigType) =>
            aetConfigType == AETConfigType.Model;

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as AETConfig);
        }

        /// <inheritdoc/>
        public bool Equals(AETConfig other)
        {
            return other != null &&
                   AETConfigType == other.AETConfigType &&
                   ((ModelsConfig == null && other.ModelsConfig == null) ||
                    (ModelsConfig != null && ModelsConfig.SequenceEqual(other.ModelsConfig)));
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hashCode = 602733048;
            hashCode = hashCode * -1521134295 + AETConfigType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<IReadOnlyList<ModelConstraintsConfig>>.Default.GetHashCode(ModelsConfig);
            return hashCode;
        }

        /// <inheritdoc/>
        public static bool operator ==(AETConfig left, AETConfig right)
        {
            return EqualityComparer<AETConfig>.Default.Equals(left, right);
        }

        /// <inheritdoc/>
        public static bool operator !=(AETConfig left, AETConfig right)
        {
            return !(left == right);
        }
    }
}
