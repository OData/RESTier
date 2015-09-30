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
        private const string ApiFactoryKey = "Microsoft.Restier.Core.ApiFactory";

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
        /// Gets the API factory from the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The API factory.</returns>
        internal static Func<IApi> GetApiFactory(this HttpRequestMessage request)
        {
            Ensure.NotNull(request, "request");

            object value;
            if (request.Properties.TryGetValue(ApiFactoryKey, out value))
            {
                return value as Func<IApi>;
            }

            return null;
        }

        /// <summary>
        /// Sets the API factory to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="apiFactory">The API factory.</param>
        internal static void SetApiFactory(this HttpRequestMessage request, Func<IApi> apiFactory)
        {
            Ensure.NotNull(request, "request");
            Ensure.NotNull(apiFactory, "apiFactory");

            request.Properties[ApiFactoryKey] = apiFactory;
        }
    }
}
