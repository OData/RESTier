// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Formatter.Serialization;
using Microsoft.OData;

#if NETCOREAPP
namespace Microsoft.Restier.AspNetCore.Formatter
#else
namespace Microsoft.Restier.AspNet.Formatter
#endif
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

        /// <summary>
        /// Writes the complex result to the response message asynchronously.
        /// </summary>
        /// <param name="graph">The collection result to write.</param>
        /// <param name="type">The type of the collection.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override Task WriteObjectAsync(
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

            return base.WriteObjectAsync(graph, type, messageWriter, writeContext);
        }
    }
}
