// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Models
{
    public class Airline
    {
        [Key]
        public string AirlineCode { get; set; }

        public string Name { get; set; }

        [Timestamp]
        public Byte[] TimeStampValue { get; set; }
    }
}
