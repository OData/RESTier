// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Runtime.Serialization;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;
using Microsoft.Restier.WebApi.Properties;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    /// <summary>
    /// The serializer for entity collection result.
    /// </summary>
    public class ODataDomainFeedSerializer : ODataFeedSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataDomainFeedSerializer" /> class.
        /// </summary>
        /// <param name="provider">The serializer provider.</param>
        public ODataDomainFeedSerializer(ODataSerializerProvider provider)
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
            Ensure.NotNull(messageWriter, "messageWriter");
            Ensure.NotNull(writeContext, "writeContext");

            IEdmEntitySetBase entitySet = writeContext.NavigationSource as IEdmEntitySetBase;
            if (entitySet == null)
            {
                throw new SerializationException(Resources.EntitySetMissingForSerialization);
            }

            EntityCollectionResult collectionResult = (EntityCollectionResult)graph;
            IEdmTypeReference feedType = collectionResult.EdmType;

            IEdmEntityTypeReference entityType = GetEntityType(feedType);
            ODataWriter writer = messageWriter.CreateODataFeedWriter(entitySet, entityType.EntityDefinition());
            this.WriteObjectInline(collectionResult.Query, feedType, writer, writeContext);
        }

        private static IEdmEntityTypeReference GetEntityType(IEdmTypeReference feedType)
        {
            if (feedType.IsCollection())
            {
                IEdmTypeReference elementType = feedType.AsCollection().ElementType();
                if (elementType.IsEntity())
                {
                    return elementType.AsEntity();
                }
            }

            string message = string.Format(
                CultureInfo.InvariantCulture,
                "{0} cannot write an object of type '{1}'.",
                typeof(ODataDomainFeedSerializer).Name,
                feedType.FullName());
            throw new SerializationException(message);
        }
    }
}
