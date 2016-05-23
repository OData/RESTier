// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.Query.Expressions;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.Restier.Publishers.OData.Results;

namespace Microsoft.Restier.Publishers.OData.Formatter.Serialization
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
                if (TryWriteAggregationResult(graph, type, messageWriter, writeContext, collectionResult.EdmType))
                {
                    return;
                }
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
        
        private bool TryWriteAggregationResult(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext,
            IEdmTypeReference feedType)
        {
            if (typeof(IEnumerable<DynamicTypeWrapper>).IsAssignableFrom(type))
            {
                IEdmTypeReference elementType = feedType.AsCollection().ElementType();
                if (elementType.IsEntity())
                {
                    IEdmEntitySetBase entitySet = writeContext.NavigationSource as IEdmEntitySetBase;
                    IEdmEntityTypeReference entityType = elementType.AsEntity();
                    ODataWriter writer = messageWriter.CreateODataFeedWriter(entitySet, entityType.EntityDefinition());
                    base.WriteObjectInline(graph, feedType, writer, writeContext);
                    return true;
                }
            }

            return false;
        }
    }
}
