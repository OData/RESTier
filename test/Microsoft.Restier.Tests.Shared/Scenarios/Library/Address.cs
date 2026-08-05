// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations.Schema;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{

    /// <summary>
    /// 
    /// </summary>
    [ComplexType]
    public class Address
    {

        /// <summary>
        /// 
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Zip { get; set; }

    }

}