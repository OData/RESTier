// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.AspNetCore.Formatter
{
    /// <summary>
    /// The serializer for raw result.
    /// </summary>
    public class RestierRawSerializer : ODataRawValueSerializer
    {
        private readonly ODataPayloadValueConverter payloadValueConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierRawSerializer"/> class.
        /// </summary>
        /// <param name="payloadValueConverter">The <see cref="ODataPayloadValueConverter"/> to use.</param>
        public RestierRawSerializer(ODataPayloadValueConverter payloadValueConverter)
        {
            Ensure.NotNull(payloadValueConverter, nameof(payloadValueConverter));
            this.payloadValueConverter = payloadValueConverter;
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
            RawResult rawResult = graph as RawResult;
            if (rawResult is not null)
            {
                graph = rawResult.Result;
                type = rawResult.Type;
            }

            if (writeContext is not null)
            {
                graph = RestierPrimitiveSerializer.ConvertToPayloadValue(graph, writeContext, payloadValueConverter);
            }

            if (graph is null)
            {
                // This is to make ODataRawValueSerializer happily serialize null value.
                graph = string.Empty;
            }

            return base.WriteObjectAsync(graph, type, messageWriter, writeContext);
        }
    }
}
