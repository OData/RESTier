using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Tests.Shared
{

    /// <summary>
    /// An <see cref="IQueryExpressionAuthorizer"/> implementation that always returns <see cref="false"/>.
    /// </summary>
    internal class DisallowEverythingAuthorizer : IQueryExpressionAuthorizer
    {
        public bool Authorize(QueryExpressionContext context) => false;
    }

}