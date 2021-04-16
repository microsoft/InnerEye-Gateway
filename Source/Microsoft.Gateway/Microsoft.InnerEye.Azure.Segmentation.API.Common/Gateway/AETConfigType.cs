namespace Microsoft.InnerEye.Azure.Segmentation.API.Common
{
    /// <summary>
    /// Current list of types of Application Entity Titles available in the gateway configuration.
    /// </summary>
    public enum AETConfigType
    {
        /// <summary>
        /// The model cloud usage
        /// </summary>
        Model,

        /// <summary>
        /// The model dry run. It saves the anonymized image to local disk for inspection.
        /// </summary>
        ModelDryRun,

        /// <summary>
        /// The model with result dry run. It saves the anonymized image and result RT from server to local disk for inspection.
        /// </summary>
        ModelWithResultDryRun,
    }
}