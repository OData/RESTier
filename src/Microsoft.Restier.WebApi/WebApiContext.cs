// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net.Http;

namespace Microsoft.Restier.WebApi
{
    internal class WebApiContext
    {
        public HttpRequestMessage Request { get; set; }

        public bool? QueryIncludeTotalCount { get; set; }
    }
}
