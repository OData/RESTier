// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Library
{
    [ComplexType]
    public class Universe
    {
        public byte[] BinaryProperty { get; set; }

        public bool BooleanProperty { get; set; }

        public byte ByteProperty { get; set; }

        //public Date DateProperty { get; set; }

        public DateTimeOffset DateTimeOffsetProperty { get; set; }

        public decimal DecimalProperty { get; set; }

        public double DoubleProperty { get; set; }

        public TimeSpan DurationProperty { get; set; }

        public Guid GuidProperty { get; set; }

        public short Int16Property { get; set; }

        public int Int32Property { get; set; }

        public long Int64Property { get; set; }

        // public sbyte SByteProperty { get; set; }

        public float SingleProperty { get; set; }

        // public FileStream StreamProperty { get; set; }

        public string StringProperty { get; set; }

        public TimeOfDay TimeOfDayProperty { get; set; }
    }
}