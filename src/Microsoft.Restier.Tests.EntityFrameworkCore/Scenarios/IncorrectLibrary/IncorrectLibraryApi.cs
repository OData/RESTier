// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.EntityFrameworkCore;
using System;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Scenarios.IncorrectLibrary
{
    /// <summary>
    /// 
    /// </summary>
    public class IncorrectLibraryApi : EntityFrameworkApi<IncorrectLibraryContext>
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public IncorrectLibraryApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

    }

}
