// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core;
using System;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.Shared
{
    public class StoreModelMapper : IModelMapper
    {
        public bool TryGetRelevantType(InvocationContext context, string name, out Type relevantType)
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

        public bool TryGetRelevantType(InvocationContext context, string namespaceName, string name, out Type relevantType)
        {
            relevantType = typeof(Product);
            return true;
        }

        public IModelMapper Inner { get; set; }
    }
}
