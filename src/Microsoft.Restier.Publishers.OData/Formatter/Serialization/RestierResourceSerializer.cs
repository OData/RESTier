// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The serializer for resource result, and now for complex only,
    /// for entity type, WebApi OData resource serializer will be used.
    /// </summary>
    public class RestierResourceSerializer : ODataResourceSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierResourceSerializer" /> class.
        /// </summary>
        /// <param name="provider">The serializer provider.</param>
        public RestierResourceSerializer(ODataSerializerProvider provider)
             : base(provider)
        {
        }

        /// <summary>
        /// Writes the complex result to the response message.
        /// </summary>
        /// <param name="graph">The complex result to write.</param>
        /// <param name="type">The type of the complex.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        public override void WriteObject(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            ComplexResult complexResult = graph as ComplexResult;
            if (complexResult != null)
            {
                graph = complexResult.Result;
                type = complexResult.Type;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
    }
}
