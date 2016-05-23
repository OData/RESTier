// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Providers.EntityFramework.Tests.Models.Library
{
    class Person
    {
        public Guid Id { get; set; }

        public string FullName { get; set; }

        public Address Addr { get; set; }

        public Universe Universe { get; set; }
    }
}
