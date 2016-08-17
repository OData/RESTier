// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The serializer for collection result.
    /// </summary>
    public class RestierCollectionSerializer : ODataCollectionSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierCollectionSerializer" /> class.
        /// </summary>
        /// <param name="provider">The serializer provider.</param>
        public RestierCollectionSerializer(ODataSerializerProvider provider)
            : base(provider)
        {
        }

        /// <summary>
        /// Writes the complex result to the response message.
        /// </summary>
        /// <param name="graph">The collection result to write.</param>
        /// <param name="type">The type of the collection.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        public override void WriteObject(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            NonResourceCollectionResult collectionResult = graph as NonResourceCollectionResult;
            if (collectionResult != null)
            {
                graph = collectionResult.Query;
                type = collectionResult.Type;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
    }
}
