using Microsoft.OData.Edm;

namespace System.Web.OData.Domain.Results
{
    /// <summary>
    /// The result of an OData query.
    /// </summary>
    public abstract class EntityQueryResult
    {
        private IEdmTypeReference edmType;

        protected EntityQueryResult(IEdmTypeReference edmType)
        {
            Ensure.NotNull(edmType, "edmType");

            this.edmType = edmType;
        }

        public IEdmTypeReference EdmType
        {
            get
            {
                return this.edmType;
            }
        }
    }
}
