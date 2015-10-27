// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.OData.Formatter.Serialization;
using Microsoft.OData.Core;
using Microsoft.Restier.WebApi.Results;

namespace Microsoft.Restier.WebApi.Formatter.Serialization
{
    /// <summary>
    /// The serializer for enum result.
    /// </summary>
    internal class RestierEnumSerializer : ODataEnumSerializer
    {
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
            }

            base.WriteObject(graph, type, messageWriter, writeContext);
        }
    }
}
