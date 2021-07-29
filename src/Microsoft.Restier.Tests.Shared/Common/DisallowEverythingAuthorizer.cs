// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.Tests.Shared
{

    /// <summary>
    /// An <see cref="IQueryExpressionAuthorizer"/> implementation that always returns <see langword="false"/>.
    /// </summary>
    public class DisallowEverythingAuthorizer : IQueryExpressionAuthorizer
    {
        public bool Authorize(QueryExpressionContext context) => false;
    }

}