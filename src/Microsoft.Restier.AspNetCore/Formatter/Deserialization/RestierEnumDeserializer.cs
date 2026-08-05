// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Formatter.Deserialization;
using Microsoft.AspNetCore.OData.Formatter.Value;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.AspNetCore.Formatter
{

    /// <summary>
    /// The serializer for enum result.
    /// </summary>
    internal class RestierEnumDeserializer : ODataEnumDeserializer
    {
        /// <inheritdoc />
        public override object ReadInline(
            object item,
            IEdmTypeReference edmType,
            ODataDeserializerContext readContext)
        {
            var result = base.ReadInline(item, edmType, readContext);

            var edmEnumObject = result as EdmEnumObject;
            if (edmEnumObject is not null)
            {
                return edmEnumObject.Value;
            }

            return result;
        }

    }

}

