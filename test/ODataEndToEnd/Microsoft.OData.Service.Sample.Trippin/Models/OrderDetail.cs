// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.OData.Service.Sample.Trippin.Models
{
    /// <summary>
    ///  It will be a complex type to test complex property Computed and Immutable properties
    /// </summary>
    public class OrderDetail
    {
        public OrderDetail()
        {
            // Set the computed value in constructor, it could be done by database layer
            ComputedProperty = "OrderDetailComputedValue";
        }

        public string NormalProperty { get; set; }

        public string AnotherNormalProperty { get; set; }

        public string ComputedProperty { get; set; }

        public string ImmutableProperty { get; set; }
    }
}