// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.AspNet.Formatter
{
    /// <summary>
    /// The serializer for raw result.
    /// </summary>
    public class RestierRawSerializer : ODataRawValueSerializer
    {
        private readonly ODataPayloadValueConverter payloadValueConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierPrimitiveSerializer"/> class.
        /// </summary>
        /// <param name="payloadValueConverter"></param>
        public RestierRawSerializer(ODataPayloadValueConverter payloadValueConverter)
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
            RawResult rawResult = graph as RawResult;
            if (rawResult != null)
            {
                graph = rawResult.Result;
                type = rawResult.Type;
            }

            if (writeContext != null)
            {
                graph = RestierPrimitiveSerializer.ConvertToPayloadValue(graph, writeContext, payloadValueConverter);
            }

            if (graph == null)
            {
                // This is to make ODataRawValueSerializer happily serialize null value.
                graph = string.Empty;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
    }
}
