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
#if NETCOREAPP3_1_OR_GREATER
        <TApi>: RestierBreakdanceTestBase<TApi> where TApi : ApiBase
#endif
    {

        /// <summary>
        /// 
        /// </summary>
        public TestContext TestContext { get; set; }

    }

}