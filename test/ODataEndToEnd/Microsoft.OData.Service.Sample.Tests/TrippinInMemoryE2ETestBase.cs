// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Default;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public abstract class TrippinInMemoryE2ETestBase : E2ETestBase<TrippinInMemoryDataServiceContext>,
        IClassFixture<TrippinServiceFixture>
    {
        protected TrippinInMemoryE2ETestBase()
            : base(new Uri("http://localhost:21248/api/Trippin/"))
        {
        }
    }
}