namespace Microsoft.InnerEye.DicomConstraints
{
    /// <summary>
    /// An interface for initializing 1 type from another
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IInitializable<T>
    {
        /// <summary>
        /// Convert from an instance of T
        /// </summary>
        /// <param name="source"></param>
        void Init(T source);
    }
}
