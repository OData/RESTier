// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

#if NET6_0_OR_GREATER

using Microsoft.Restier.EntityFrameworkCore;
using System;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.Views
{

    /// <summary>
    /// 
    /// </summary>
    public class LibraryWithViewsApi : EntityFrameworkApi<LibraryWithViewsContext>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public LibraryWithViewsApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

    }

}

#endif