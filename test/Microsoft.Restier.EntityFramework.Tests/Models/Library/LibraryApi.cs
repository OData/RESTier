// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core;

namespace Microsoft.Restier.EntityFramework.Tests.Models.Library
{
    class LibraryApi : DbApi<LibraryContext>
    {
        internal ApiContext Context
        {
            get { return this.ApiContext; }
        }
    }
}
