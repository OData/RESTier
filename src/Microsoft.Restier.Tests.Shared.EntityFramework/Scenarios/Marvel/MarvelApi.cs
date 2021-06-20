// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.EntityFramework;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Marvel
{

    /// <summary>
    /// A testable API that implements an Entity Framework model and has secondary operations
    /// against a SQL 2017 LocalDB database.
    /// </summary>
    public class MarvelApi : EntityFrameworkApi<MarvelContext>
    {

        public MarvelApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

    }

}