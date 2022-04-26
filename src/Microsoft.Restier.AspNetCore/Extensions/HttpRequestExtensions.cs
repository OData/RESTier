// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Net;

namespace Microsoft.AspNetCore.Http
{

    /// <summary>
    /// Extensions for <see cref="HttpRequest"/>.
    /// </summary>
    public static class RestierHttpRequestExtensions
    {

        /// <summary>
        /// Determines whether or not the request is being made on the same machine as the server itself.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <remarks>Taken from: https://www.strathweb.com/2016/04/request-islocal-in-asp-net-core.</remarks>
        public static bool IsLocal(this HttpRequest req)
        {
            var connection = req.HttpContext.Connection;
            if (connection.RemoteIpAddress is not null)
            {
                if (connection.LocalIpAddress is not null)
                {
                    return connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
                }
                else
                {
                    return IPAddress.IsLoopback(connection.RemoteIpAddress);
                }
            }

            // for in memory TestServer or when dealing with default connection info
            if (connection.RemoteIpAddress is null && connection.LocalIpAddress is null)
            {
                return true;
            }

            return false;
        }

    }

}
