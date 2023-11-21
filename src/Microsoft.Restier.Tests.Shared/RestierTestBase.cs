// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Breakdance;
using Microsoft.Restier.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Shared
{

    /// <summary>
    /// 
    /// </summary>
    public class RestierTestBase
#if NET6_0_OR_GREATER
        <TApi>: RestierBreakdanceTestBase<TApi> where TApi : ApiBase
#endif
    {
#if NET6_0_OR_GREATER
        public RestierTestBase(bool useEndpointRouting = false) : base(useEndpointRouting)
        {
            
        }
#else

        ///<summary>Exists to provide compatibility for our ASP.NET Classic tests. Do not use.</summary>
        public bool UseEndpointRouting => false;

#endif

        /// <summary>
        /// 
        /// </summary>
        public TestContext TestContext { get; set; }

    }

}