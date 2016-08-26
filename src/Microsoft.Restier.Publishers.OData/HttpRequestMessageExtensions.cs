// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Net.Http;
using Microsoft.Restier.Publishers.OData.Batch;

namespace Microsoft.Restier.Publishers.OData
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="HttpRequestMessage"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class HttpRequestMessageExtensions
    {
        private const string ChangeSetKey = "Microsoft.Restier.Submit.ChangeSet";

        /// <summary>
        /// Sets the <see cref="RestierChangeSetProperty"/> to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="changeSetProperty">The change set to be set.</param>
        public static void SetChangeSet(this HttpRequestMessage request, RestierChangeSetProperty changeSetProperty)
        {
            Ensure.NotNull(request, "request");
            request.Properties.Add(ChangeSetKey, changeSetProperty);
        }

        /// <summary>
        /// Gets the <see cref="RestierChangeSetProperty"/> from the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The <see cref="RestierChangeSetProperty"/>.</returns>
        public static RestierChangeSetProperty GetChangeSet(this HttpRequestMessage request)
        {
            Ensure.NotNull(request, "request");

            object value;
            if (request.Properties.TryGetValue(ChangeSetKey, out value))
            {
                return value as RestierChangeSetProperty;
            }

            return null;
        }
    }
}
