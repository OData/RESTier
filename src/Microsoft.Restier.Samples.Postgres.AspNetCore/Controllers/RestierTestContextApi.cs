// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.EntityFrameworkCore;
using Microsoft.Restier.Samples.Postgres.AspNetCore.Models;

namespace Microsoft.Restier.Samples.Postgres.AspNetCore.Controllers
{
    public class RestierTestContextApi : EntityFrameworkApi<RestierTestContext>
    {
        public RestierTestContextApi(
            RestierTestContext dbContext,
            IEdmModel model,
            IQueryHandler queryHandler,
            ISubmitHandler submitHandler)
            : base(dbContext, model, queryHandler, submitHandler)
        {
        }

        /// <summary>
        /// Checks if the database is online.
        /// </summary>
        /// <returns>True if the database can connect; otherwise, false.</returns>
        [UnboundOperation]
        public bool IsOnline()
        {
            try
            {
                return DbContext.Database.CanConnect();
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Debug.WriteLine(ex);
                return false;
            }
        }
    }
}
