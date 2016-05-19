// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Restier.WebApi.Test.Scenario;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public abstract class TrippinE2ETestBase : E2ETestBase<TrippinModel>, IClassFixture<TrippinServiceFixture>
    {
        protected TrippinE2ETestBase()
            : base(new Uri("http://localhost:18384/api/Trippin/"))
        {
            this.ResetDataSource();
        }
    }
}
