// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    /// <summary>
    /// The serializer for primitive result.
    /// </summary>
    internal class ODataDomainPrimitiveSerializer : ODataPrimitiveSerializer
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
                graph = ConvertToPayloadValue(writeContext.Model, graph);
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }

        internal static object ConvertToPayloadValue(IEdmModel model, object value)
        {
            Ensure.NotNull(model, "model");

            var payloadValueConverter = model.GetPayloadValueConverter();
            return payloadValueConverter.ConvertToPayloadValue(value, null);
        }
    }
}
