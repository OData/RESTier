// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Formatter.Deserialization;
using Microsoft.OData;
using Microsoft.OData.Edm;

#if NETCOREAPP
namespace Microsoft.Restier.AspNetCore.Formatter
#else
namespace Microsoft.Restier.AspNet.Formatter
#endif
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
            if (edmEnumObject != null)
            {
                return edmEnumObject.Value;
            }

            return result;
        }
    }
}
