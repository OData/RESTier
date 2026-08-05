// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.AspNetCore.OData.Query.Wrapper;
using Microsoft.OData;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Restier.AspNetCore.Formatter
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
        /// Writes the entity collection results to the response message asynchronously.
        /// </summary>
        /// <param name="graph">The entity collection results.</param>
        /// <param name="type">The type of the entities.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public override async Task WriteObjectAsync(object graph, Type type, ODataMessageWriter messageWriter, ODataSerializerContext writeContext)
        {
            Ensure.NotNull(messageWriter, nameof(messageWriter));
            Ensure.NotNull(writeContext, nameof(writeContext));

            if (graph is ResourceSetResult collectionResult)
            {
                graph = collectionResult.Query;
                type = collectionResult.Type;

#pragma warning disable CA1062 // Validate public arguments
                if (await TryWriteAggregationResult(graph, type, messageWriter, writeContext, collectionResult.EdmType))
#pragma warning restore CA1062 // Validate public arguments

                {
                    return;
                }
            }

            await base.WriteObjectAsync(graph, type, messageWriter, writeContext).ConfigureAwait(false);
        }

        internal async Task<bool> TryWriteAggregationResult(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext,
            IEdmTypeReference resourceSetType)
        {
            if (typeof(IEnumerable<DynamicTypeWrapper>).IsAssignableFrom(type))
            {
                var elementType = resourceSetType.AsCollection().ElementType();
                if (elementType.IsEntity())
                {
                    var entitySet = writeContext.NavigationSource as IEdmEntitySetBase;
                    var entityType = elementType.AsEntity();
                    var writer = await messageWriter.CreateODataResourceSetWriterAsync(entitySet, entityType.EntityDefinition());
                    await WriteObjectInlineAsync(graph, resourceSetType, writer, writeContext);
                    return true;
                }
            }

            return false;
        }
    }
}
