// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Net.Http;
using Microsoft.Restier.Core;
using Microsoft.Restier.WebApi.Batch;

namespace Microsoft.Restier.WebApi
{
    /// <summary>
    /// Offers a collection of extension methods to <see cref="HttpRequestMessage"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class HttpRequestMessageExtensions
    {
        private const string ChangeSetKey = "Microsoft.Restier.Submit.ChangeSet";
        private const string ApiInstanceKey = "Microsoft.Restier.Core.ApiInstance";

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

        /// <summary>
        /// Gets the API instance from the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The API instance.</returns>
        internal static ApiBase GetApiInstance(this HttpRequestMessage request)
        {
            Ensure.NotNull(request, "request");

            object value;
            if (request.Properties.TryGetValue(ApiInstanceKey, out value))
            {
                return value as ApiBase;
            }

            return null;
        }

        /// <summary>
        /// Sets the API instance to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="apiInstance">The API instance.</param>
        internal static void SetApiInstance(this HttpRequestMessage request, ApiBase apiInstance)
        {
            Ensure.NotNull(request, "request");
            Ensure.NotNull(apiInstance, "apiInstance");

            request.Properties[ApiInstanceKey] = apiInstance;
        }
    }
}
