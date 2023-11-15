// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NETCOREAPP3_1_OR_GREATER

using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Microsoft.Restier.Tests.AspNetCore.ClaimsPrincipalAccessor
{

    /// <summary>
    /// 
    /// </summary>
    public class ClaimsPrincipalApi : ApiBase
    {

        #region Constructors

        public ClaimsPrincipalApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        #endregion

        [UnboundOperation]
        public bool ClaimsPrincipalCurrentIsNotNull()
        {
            return ClaimsPrincipal.Current is not null;
        }

    }

}

#endif