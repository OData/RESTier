// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Tests.Shared
{
    public class StoreModelProducer : IModelBuilder
    {
        private readonly EdmModel model;

        public StoreModelProducer(EdmModel model)
        {
            this.model = model;
        }

        public IEdmModel GetModel(ModelContext context)
        {
            return model;
        }
    }
}
