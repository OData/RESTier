// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.OData.Service.Sample.TrippinInMemory.Models
{
    public class Airline
    {
        [Key]
        public string AirlineCode { get; set; }

        [ConcurrencyCheck]
        public string Name { get; set; }
    }
}