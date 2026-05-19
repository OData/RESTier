// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library.EFCore.Views
{
    [Keyless]
    public partial class BooksByPublisher
    {
        // Publisher.Id is a string in the shared Library fixture (e.g. "Publisher1").
        public string PublisherId { get; set; }
        public string BookName { get; set; }
        public int BookCount { get; set; }
    }
}
