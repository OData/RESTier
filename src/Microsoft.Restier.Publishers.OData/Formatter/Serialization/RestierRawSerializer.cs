// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The serializer for raw result.
    /// </summary>
    public class RestierRawSerializer : ODataRawValueSerializer
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
            RawResult rawResult = graph as RawResult;
            if (rawResult != null)
            {
                graph = rawResult.Result;
                type = rawResult.Type;
            }

            if (writeContext != null)
            {
                graph = RestierPrimitiveSerializer.ConvertToPayloadValue(graph, writeContext);
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
