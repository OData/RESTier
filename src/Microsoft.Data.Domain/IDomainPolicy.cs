namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Represents a policy applicable to a domain that can be
    /// activated during a domain flow then later deactivated.
    /// </summary>
    public interface IDomainPolicy
    {
        /// <summary>
        /// Activates this domain policy.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        void Activate(InvocationContext context);

        /// <summary>
        /// Deactivates this domain policy.
        /// </summary>
        /// <param name="context">
        /// An invocation context.
        /// </param>
        void Deactivate(InvocationContext context);
    }
}
