// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    /// <summary>
    /// The serializer for primitive result.
    /// </summary>
    internal class RestierPrimitiveSerializer : ODataPrimitiveSerializer
    {
        /// <summary>
        /// Writes the entity result to the response message.
        /// </summary>
        /// <param name="graph">The entity result to write.</param>
        /// <param name="type">The type of the entity.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        public override void WriteObject(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            PrimitiveResult primitiveResult = graph as PrimitiveResult;
            if (primitiveResult != null)
            {
                graph = primitiveResult.Result;
            }

            if (writeContext != null)
            {
                graph = ConvertToPayloadValue(graph, writeContext);
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }

        /// <summary>
        /// Creates an <see cref="ODataPrimitiveValue"/> for the object represented by <paramref name="graph"/>.
        /// </summary>
        /// <param name="graph">The primitive value.</param>
        /// <param name="primitiveType">The EDM primitive type of the value.</param>
        /// <param name="writeContext">The serializer write context.</param>
        /// <returns>The created <see cref="ODataPrimitiveValue"/>.</returns>
        public override ODataPrimitiveValue CreateODataPrimitiveValue(
            object graph,
            IEdmPrimitiveTypeReference primitiveType,
            ODataSerializerContext writeContext)
        {
            // The EDM type of the "graph" would override the EDM type of the property when
            // OData Web API infers the primitiveType. Thus for "graph" of System.DateTime,
            // the primitiveType is always Edm.DateTimeOffset.
            //
            // In EF, System.DateTime is used for SqlDate, SqlDateTime and SqlDateTime2.
            // All of them have no time zone information thus it is safe to clear the time
            // zone when converting the "graph" to a DateTimeOffset.
            if (primitiveType != null && primitiveType.IsDateTimeOffset() && graph is DateTime)
            {
                graph = new DateTimeOffset((DateTime)graph, TimeSpan.Zero);
            }

            return base.CreateODataPrimitiveValue(graph, primitiveType, writeContext);
        }

        internal static object ConvertToPayloadValue(object value, ODataSerializerContext writeContext)
        {
            Ensure.NotNull(writeContext, "writeContext");

            IEdmTypeReference edmTypeReference = null;
            if (writeContext.Path != null)
            {
                // Try to get the EDM type of the value from the path.
                var edmType = writeContext.Path.EdmType as IEdmPrimitiveType;
                if (edmType != null)
                {
                    // Just created to call the payload value converter.
                    edmTypeReference = new EdmPrimitiveTypeReference(edmType, true /*isNullable*/);
                }
            }

            var payloadValueConverter = writeContext.Model.GetPayloadValueConverter();
            return payloadValueConverter.ConvertToPayloadValue(value, edmTypeReference);
        }
    }
}
