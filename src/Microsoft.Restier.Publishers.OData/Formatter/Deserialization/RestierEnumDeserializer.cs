// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData;
using System.Web.OData.Formatter.Deserialization;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The serializer for enum result.
    /// </summary>
    internal class RestierEnumDeserializer : ODataEnumDeserializer
    {
        private ODataEnumDeserializer enumDeserializer = new ODataEnumDeserializer();

        /// <inheritdoc />
        public override object Read(
            ODataMessageReader messageReader,
            Type type,
            ODataDeserializerContext readContext)
        {
            return enumDeserializer.Read(messageReader, type, readContext);
        }

        /// <inheritdoc />
        public override object ReadInline(
            object item,
            IEdmTypeReference edmType,
            ODataDeserializerContext readContext)
        {
            var result = enumDeserializer.ReadInline(item, edmType, readContext);

            var edmEnumObject = result as EdmEnumObject;
            if (edmEnumObject != null)
            {
                return edmEnumObject.Value;
            }

            return result;
        }
    }
}
