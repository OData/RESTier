// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// A review for a book. Used for testing multi-level deep insert/update.
    /// </summary>
    public class Review
    {

        public Guid Id { get; set; }

        public string Content { get; set; }

        public int Rating { get; set; }

        public Guid BookId { get; set; }

        public Book Book { get; set; }

    }

}
