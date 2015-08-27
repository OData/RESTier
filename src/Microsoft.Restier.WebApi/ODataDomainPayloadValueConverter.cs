﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Restier.WebApi
{
    /// <summary>
    /// The default payload value converter in RESTier.
    /// </summary>
    public class ODataDomainPayloadValueConverter : ODataPayloadValueConverter
    {
        internal static readonly ODataDomainPayloadValueConverter Default = new ODataDomainPayloadValueConverter();

        /// <summary>
        /// Converts the given primitive value defined in a type definition from the payload object.
        /// </summary>
        /// <param name="value">The given CLR value.</param>
        /// <param name="edmTypeReference">The expected type reference from model.</param>
        /// <returns>The converted payload value of the underlying type.</returns>
        /// <remarks>
        /// RESTier handles Edm.Date specially.
        /// System.DateTime values will be converted to System.DateTimeOffset in OData Web API
        /// before being passed into ODataLib for serialization. So we need to convert System.DateTimeOffset
        /// to Edm.Library.Date to avoid type validation failure in ODataLib.
        /// </remarks>
        public override object ConvertToPayloadValue(object value, IEdmTypeReference edmTypeReference)
        {
            if (edmTypeReference != null && edmTypeReference.IsDate() && value is DateTimeOffset)
            {
                var dateTimeOffsetValue = (DateTimeOffset)value;
                return new Date(dateTimeOffsetValue.Year, dateTimeOffsetValue.Month, dateTimeOffsetValue.Day);
            }

            return base.ConvertToPayloadValue(value, edmTypeReference);
        }
    }
}
