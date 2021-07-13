// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.OData;
using Microsoft.OData.Edm;

#if NETCOREAPP3_1_OR_GREATER
namespace Microsoft.Restier.AspNetCore.Formatter
#else
namespace Microsoft.Restier.AspNet.Formatter
#endif
{
    /// <summary>
    /// The serializer for primitive result.
    /// </summary>
    public class RestierPrimitiveSerializer : ODataPrimitiveSerializer
    {
        private readonly ODataPayloadValueConverter payloadValueConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierPrimitiveSerializer"/> class.
        /// </summary>
        /// <param name="payloadValueConverter">The <see cref="ODataPayloadValueConverter"/> to use.</param>
        public RestierPrimitiveSerializer(ODataPayloadValueConverter payloadValueConverter)
        {
            Ensure.NotNull(payloadValueConverter, nameof(payloadValueConverter));
            this.payloadValueConverter = payloadValueConverter;
        }

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
            if (graph is PrimitiveResult primitiveResult)
            {
                graph = primitiveResult.Result;
                type = primitiveResult.Type;
            }

            if (writeContext != null)
            {
                graph = ConvertToPayloadValue(graph, writeContext, payloadValueConverter);
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }

        /// <summary>
        /// Writes the entity result to the response message asynchronously.
        /// </summary>
        /// <param name="graph">The entity result to write.</param>
        /// <param name="type">The type of the entity.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override Task WriteObjectAsync(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            if (graph is PrimitiveResult primitiveResult)
            {
                graph = primitiveResult.Result;
                type = primitiveResult.Type;
            }

            if (writeContext != null)
            {
                graph = ConvertToPayloadValue(graph, writeContext, payloadValueConverter);
            }

            return base.WriteObjectAsync(graph, type, messageWriter, writeContext);
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
                // If DateTime.Kind equals Local, offset should equal the offset of the system's local time zone
                if (((DateTime)graph).Kind == DateTimeKind.Local)
                {
                    graph = new DateTimeOffset((DateTime)graph, TimeZoneInfo.Local.GetUtcOffset((DateTime)graph));
                }
                else
                {
                    graph = new DateTimeOffset((DateTime)graph, TimeSpan.Zero);
                }
            }

            return base.CreateODataPrimitiveValue(graph, primitiveType, writeContext);
        }

        /// <summary>
        /// Converts the object to a Payload value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="writeContext">The <see cref="ODataSerializerContext"/> to use.</param>
        /// <param name="payloadValueConverter">The <see cref="ODataPayloadValueConverter"/> to use.</param>
        /// <returns>The converted value.</returns>
        internal static object ConvertToPayloadValue(object value, ODataSerializerContext writeContext, ODataPayloadValueConverter payloadValueConverter)
        {
            Ensure.NotNull(writeContext, nameof(writeContext));

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

            return payloadValueConverter.ConvertToPayloadValue(value, edmTypeReference);
        }
    }
}