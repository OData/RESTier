namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Represents a hook point that profiles other hook points.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose instances
    /// are used in the original order of registration.
    /// </remarks>
    public interface IDomainProfiler
    {
        /// <summary>
        /// Profiles a hook point.
        /// </summary>
        /// <typeparam name="T">
        /// The type of a hook point.
        /// </typeparam>
        /// <param name="instance">
        /// An instance of the hook point.
        /// </param>
        /// <returns>
        /// A profiled instance of the hook point.
        /// </returns>
        T Profile<T>(T instance);
    }
}
