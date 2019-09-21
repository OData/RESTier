using System;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.Shared
{
    internal class StoreModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(ModelContext context, string name, out Type relevantType)
        {
            if (name == "Products")
            {
                relevantType = typeof(Product);
            }
            else if (name == "Customers")
            {
                relevantType = typeof(Customer);
            }
            else if (name == "Stores")
            {
                relevantType = typeof(Store);
            }
            else
            {
                relevantType = null;
            }
            
            return true;
        }

        public bool TryGetRelevantType(ModelContext context, string namespaceName, string name, out Type relevantType)
        {
            relevantType = typeof(Product);
            return true;
        }
    }
}
