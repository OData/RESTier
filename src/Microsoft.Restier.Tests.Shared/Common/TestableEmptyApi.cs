// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.Core;

namespace Microsoft.Restier.Tests.Shared
{

    /// <summary>
    /// An API that inherits from <see cref="ApiBase"/> and has no operations or methods.
    /// </summary>
    /// <remarks>
    /// Now that we've separated service registration from API instances, this class can be used many different ways in the tests.
    /// </remarks>
    public class TestableEmptyApi : ApiBase
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public TestableEmptyApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

    }

}