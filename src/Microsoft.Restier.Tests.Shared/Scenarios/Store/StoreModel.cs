// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNet.OData.Builder;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Tests.Shared
{
    internal static class StoreModel
    {
        public static EdmModel Model { get; private set; }

        public static IEdmEntityType Product { get; private set; }

        static StoreModel()
        {
            var builder = new ODataConventionModelBuilder
            {
                Namespace = "Microsoft.Restier.Tests.AspNet"
            };
            builder.EntitySet<Product>("Products");
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Store>("Stores");
            builder.Function("GetBestProduct").ReturnsFromEntitySet<Product>("Products");
            builder.Action("RemoveWorstProduct").ReturnsFromEntitySet<Product>("Products");
            Model = (EdmModel)builder.GetEdmModel();
            Product = (IEdmEntityType)Model.FindType("Microsoft.Restier.Tests.AspNet.Product");
        }

    }

}