// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Runtime.Serialization;
using System.Web.OData.Domain.Results;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.OData.Edm;

namespace System.Web.OData.Domain.Formatter.Serialization
{
    public class ODataDomainFeedSerializer : ODataFeedSerializer
    {
        public ODataDomainFeedSerializer(ODataSerializerProvider provider)
            : base(provider)
        {
        }

        public override void WriteObject(object graph, Type type, ODataMessageWriter messageWriter, ODataSerializerContext writeContext)
        {
            Ensure.NotNull(messageWriter, "messageWriter");
            Ensure.NotNull(writeContext, "writeContext");

            IEdmEntitySetBase entitySet = writeContext.NavigationSource as IEdmEntitySetBase;
            if (entitySet == null)
            {
                throw new SerializationException("EntitySet is missing during serialization.");
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

            string message = string.Format("{0} cannot write an object of type '{1}'.", typeof(ODataDomainFeedSerializer).Name, feedType.FullName());
            throw new SerializationException(message);
        }
    }
}
