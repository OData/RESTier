// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.Tests.Shared
{
    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool IsActive { get; set; }

        [Required]
        public Address Addr { get; set; }

        public Address Addr2 { get; set; }

        public Address Addr3 { get; set; }
    }
}
