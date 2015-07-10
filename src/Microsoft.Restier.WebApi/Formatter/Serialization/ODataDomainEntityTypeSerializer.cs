// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    /// <summary>
    /// The serializer for entity result.
    /// </summary>
    public class ODataDomainEntityTypeSerializer : ODataEntityTypeSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataDomainEntityTypeSerializer" /> class.
        /// </summary>
        /// <param name="provider">The serializer provider.</param>
        public ODataDomainEntityTypeSerializer(ODataSerializerProvider provider)
            : base(provider)
        {
        }

        /// <summary>
        /// Writes the entity result to the response message.
        /// </summary>
        /// <param name="graph">The entity result to write.</param>
        /// <param name="type">The type of the entity.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        public override void WriteObject(object graph, Type type, ODataMessageWriter messageWriter, ODataSerializerContext writeContext)
        {
            EntityResult entityResult = graph as EntityResult;
            if (entityResult != null)
            {
                graph = entityResult.Result;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }

        /// <summary>
        /// Creates ETag for the entity instance.
        /// </summary>
        /// <param name="entityInstanceContext">The context that contains the entity instance.</param>
        /// <returns>The ETag created.</returns>
        public override string CreateETag(EntityInstanceContext entityInstanceContext)
        {
            Ensure.NotNull(entityInstanceContext);
            string etag = null;
            object etagGetterObject;
            if (entityInstanceContext.Request.Properties.TryGetValue("ETagGetter", out etagGetterObject))
            {
                Func<object, string> etagGetter = etagGetterObject as Func<object, string>;
                if (etagGetter != null)
                {
                    etag = etagGetter(entityInstanceContext.EntityInstance);
                }
            }

            if (etag == null)
            {
                etag = base.CreateETag(entityInstanceContext);
            }

            return etag;
        }
    }
}
