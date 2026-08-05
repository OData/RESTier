// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.AspNetCore.Formatter
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
            var result = UnpackResult(graph, type);
            return base.WriteObjectAsync(result.Graph, result.Type, messageWriter, writeContext);
        }

        /// <summary>
        /// Returns a tuple containing the correct object and type from the <see cref="NonResourceCollectionResult"/>.
        /// </summary>
        /// <param name="result">The object passed into the Serializer.</param>
        /// <param name="type">The type passed into the Serializer.</param>
        /// <returns>A tuple containing the correct object and type from the <see cref="NonResourceCollectionResult"/>.</returns>
        internal static (object Graph, Type Type) UnpackResult(object result, Type type)
        {
            return result is NonResourceCollectionResult collectionResult ? (collectionResult.Query, collectionResult.Type) : (result, type);
        }

    }

}
