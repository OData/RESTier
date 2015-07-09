// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.EntityFramework.Tests.Models.Library
{
    class Book
    {
        public string Id { get; set; }

        public Publisher Publisher { get; set; }
    }
}
