using System.Collections;
using System.Linq;
using Microsoft.Data.Domain;
using Microsoft.OData.Edm;

namespace System.Web.OData.Domain.Results
{
    /// <summary>
    /// Represents a single entity instance being returned from an action.
    /// </summary>
    public class EntityResult : EntityQueryResult
    {
        public EntityResult(IQueryable query, IEdmTypeReference edmType, DomainContext context)
            : base(edmType)
        {
            Ensure.NotNull(query, "query");
            Ensure.NotNull(context, "context");

            this.Context = context;

            this.Result = query.SingleOrDefault();
        }

        public object Result { get; private set; }

        public DomainContext Context { get; private set; }
    }
}
