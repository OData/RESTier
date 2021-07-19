#if NETCOREAPP3_1_OR_GREATER

using Microsoft.Restier.AspNetCore.Abstractions;
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

        [Operation]
        public bool AccessorIsNotNull()
        {
            return ServiceProvider.GetService<IClaimsPrincipalAccessor>() != null;
        }

        [Operation]
        public bool AccessorClaimsPrincipalIsNotNull()
        {
            return ServiceProvider.GetService<IClaimsPrincipalAccessor>()?.ClaimsPrincipal != null;
        }

        [Operation]
        public bool ClaimsPrincipalCurrentIsNotNull()
        {
            return ClaimsPrincipal.Current != null;
        }

    }

}

#endif