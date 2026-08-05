// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System.Security.Claims;

namespace Microsoft.Restier.Tests.AspNetCore.ClaimsPrincipalAccessor
{

    /// <summary>
    /// A test API that exposes an operation to verify ClaimsPrincipal.Current is accessible.
    /// </summary>
    public class ClaimsPrincipalApi : ApiBase
    {
        public ClaimsPrincipalApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        [UnboundOperation]
        public bool ClaimsPrincipalCurrentIsNotNull()
        {
            return ClaimsPrincipal.Current is not null;
        }
    }

}

#endif
