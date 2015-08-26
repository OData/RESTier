// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
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
        private const string DomainFactoryKey = "Microsoft.Restier.Core.DomainFactory";

        /// <summary>
        /// Gets the <see cref="ODataDomainChangeSetProperty"/> from the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The <see cref="ODataDomainChangeSetProperty"/>.</returns>
        public static ODataDomainChangeSetProperty GetChangeSet(this HttpRequestMessage request)
        {
            Ensure.NotNull(request, "request");

            object value;
            if (request.Properties.TryGetValue(ChangeSetKey, out value))
            {
                return value as ODataDomainChangeSetProperty;
            }

            return null;
        }

        /// <summary>
        /// Gets the domain factory from the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <returns>The domain factory.</returns>
        internal static Func<IDomain> GetDomainFactory(this HttpRequestMessage request)
        {
            Ensure.NotNull(request, "request");

            object value;
            if (request.Properties.TryGetValue(DomainFactoryKey, out value))
            {
                return value as Func<IDomain>;
            }

            return null;
        }

        /// <summary>
        /// Sets the domain factory to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="request">The HTTP request.</param>
        /// <param name="domainFactory">The domain factory.</param>
        internal static void SetDomainFactory(this HttpRequestMessage request, Func<IDomain> domainFactory)
        {
            Ensure.NotNull(request, "request");
            Ensure.NotNull(domainFactory, "domainFactory");

            request.Properties[DomainFactoryKey] = domainFactory;
        }
    }
}
