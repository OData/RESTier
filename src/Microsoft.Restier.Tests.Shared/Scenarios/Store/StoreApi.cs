// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.EntityFramework;

namespace Microsoft.Restier.Tests.Shared
{

    internal class StoreApi : EntityFrameworkApi<DbContext>
    {
        public StoreApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        /// Limits the results of <see cref="Customer" /> queries by a pre-determined set of criteria.
        /// </summary>
        protected internal IQueryable<Customer> OnFilterCustomers(IQueryable<Customer> entitySet)
        {
            // not filtering at this level
            return entitySet;
        }

        /// <summary>
        /// Limits the results of <see cref="Product" /> queries by a pre-determined set of criteria.
        /// </summary>
        protected internal IQueryable<Product> OnFilterProducts(IQueryable<Product> entitySet)
        {
            return entitySet.Where(c => c.IsActive);
        }

    }

}