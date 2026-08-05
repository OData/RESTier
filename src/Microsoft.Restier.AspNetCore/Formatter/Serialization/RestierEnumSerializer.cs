// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OData.Formatter.Serialization;
using Microsoft.OData;

namespace Microsoft.Restier.AspNetCore.Formatter
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
        /// Returns a tuple containing the correct object and type from the <see cref="EnumResult"/>.
        /// </summary>
        /// <param name="result">The object passed into the Serializer.</param>
        /// <param name="type">The type passed into the Serializer.</param>
        /// <returns>A tuple containing the correct object and type from the <see cref="EnumResult"/>.</returns>
        internal static (object Graph, Type Type) UnpackResult(object result, Type type)
        {
            return result is EnumResult enumResult ? (enumResult.Result, enumResult.Type) : (result, type);
        }

    }

}
