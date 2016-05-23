// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Publishers.OData.Query
{
    internal class RestierQueryExecutorOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the total
        /// number of items should be retrieved when the
        /// result has been filtered using paging operators.
        /// </summary>
        /// <remarks>
        /// Setting this to <c>true</c> may have a performance impact as
        /// the data provider may need to execute two independent queries.
        /// </remarks>
        public bool IncludeTotalCount { get; set; }

        public Action<long> SetTotalCount { get; set; }
    }
}
