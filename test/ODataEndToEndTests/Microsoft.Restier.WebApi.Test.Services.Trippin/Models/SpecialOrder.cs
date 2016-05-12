// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Models
{
    public class SpecialOrder : Order
    {
        public string Tag { get; set; }
    }
}