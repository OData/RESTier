// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.Publishers.OData.Formatter
{
    /// <summary>
    /// The serializer for enum result.
    /// </summary>
    public class RestierEnumSerializer : ODataEnumSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestierEnumSerializer" /> class.
        /// </summary>
        /// <param name="provider">The serializer provider.</param>
        public RestierEnumSerializer(ODataSerializerProvider provider) : base(provider)
        {
        }

        /// <summary>
        /// Writes the enum result to the response message.
        /// </summary>
        /// <param name="graph">The enum result to write.</param>
        /// <param name="type">The type of the enum.</param>
        /// <param name="messageWriter">The message writer.</param>
        /// <param name="writeContext">The writing context.</param>
        public override void WriteObject(
            object graph,
            Type type,
            ODataMessageWriter messageWriter,
            ODataSerializerContext writeContext)
        {
            EnumResult enumResult = graph as EnumResult;
            if (enumResult != null)
            {
                graph = enumResult.Result;
                type = enumResult.Type;
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
    }
}
