// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Web.OData.Domain.Batch;

namespace System.Web.OData.Domain
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpRequestMessageExtensions
    {
        private const string ChangeSetKey = "Microsoft.Data.Domain.Submit.ChangeSet";

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
