// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Restier.Tests.Shared
{
    internal class Customer
    {
        public short Id { get; set; }

        public List<Product> FavoriteProducts { get; set; }
    }
}
