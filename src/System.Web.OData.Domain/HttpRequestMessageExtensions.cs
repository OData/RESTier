// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Net.Http;
using Microsoft.Restier.WebApi.Batch;

namespace Microsoft.Restier.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpRequestMessageExtensions
    {
        private const string ChangeSetKey = "Microsoft.Restier.Submit.ChangeSet";

        public static ODataDomainChangeSetProperty GetChangeSet(this HttpRequestMessage request)
        {
            Ensure.NotNull(request, "request");

            ODataDomainChangeSetProperty changeSetProperty;
            object value;
            if (request.Properties.TryGetValue(ChangeSetKey, out value))
            {
                changeSetProperty = value as ODataDomainChangeSetProperty;
                Contract.Assert(changeSetProperty != null);
            }
            else
            {
                changeSetProperty = null;
            }

            return changeSetProperty;
        }
    }
}
