// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core.Contexts;
using System;

namespace Microsoft.AspNetCore.Http
{

    /// <summary>
    /// 
    /// </summary>
    internal static class Restier_GraphQL_HttpContextExtensions
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static QueryContext ToQueryContext(this HttpContext httpContext)
        {
            var queryContext = new QueryContext
            {
                IncomingUrl = new Uri(httpContext.Request.Path.Value)
            };
            return queryContext;
        }

    }

}
