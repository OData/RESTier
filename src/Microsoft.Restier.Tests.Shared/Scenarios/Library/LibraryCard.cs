// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// An object in the model that is supposed to remain empty for unit tests.
    /// </summary>
    public class LibraryCard
    {

        public Guid Id { get; set; }

        public DateTimeOffset DateRegistered { get; set; }

    }

}