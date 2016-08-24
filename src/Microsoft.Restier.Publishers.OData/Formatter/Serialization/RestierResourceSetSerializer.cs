// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Web.OData.Formatter.Serialization;
using System.Web.OData.Query.Expressions;
using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The serializer for resource set result.
    /// </summary>
    public class RestierResourceSetSerializer : ODataResourceSetSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierResourceSetSerializer" /> class.
        /// </summary>
        /// <param name="provider">The serializer provider.</param>
        public RestierResourceSetSerializer(ODataSerializerProvider provider)
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
            ResourceSetResult collectionResult = graph as ResourceSetResult;
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
            IEdmTypeReference resourceSetType)
        {
            if (typeof(IEnumerable<DynamicTypeWrapper>).IsAssignableFrom(type))
            {
                IEdmTypeReference elementType = resourceSetType.AsCollection().ElementType();
                if (elementType.IsEntity())
                {
                    var entitySet = writeContext.NavigationSource as IEdmEntitySetBase;
                    var entityType = elementType.AsEntity();
                    var writer = messageWriter.CreateODataResourceSetWriter(entitySet, entityType.EntityDefinition());
                    WriteObjectInline(graph, resourceSetType, writer, writeContext);
                    return true;
                }
            }

            return false;
        }
    }
}
