// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    /// <summary>
    /// The serializer for entity collection result.
    /// </summary>
    public class RestierFeedSerializer : ODataFeedSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierFeedSerializer" /> class.
        /// </summary>
        /// <param name="provider">The serializer provider.</param>
        public RestierFeedSerializer(ODataSerializerProvider provider)
            : base(provider)
        {
        }

        /// <summary>
        /// Writes the entity collection results to the response message.
        /// </summary>
        /// <param name="graph">The entity collection results.</param>
        /// <param name="type">The type of the entities.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        public override void WriteObject(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            EntityCollectionResult collectionResult = graph as EntityCollectionResult;
            if (collectionResult != null)
            {
                graph = collectionResult.Query;
                type = collectionResult.Type;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
    }
}
