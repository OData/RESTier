// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http.Routing;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using Microsoft.OData.UriParser;

namespace Microsoft.OData.Service.Sample.TrippinInMemory
{
    public static class Helpers
    {
        public static TKey GetKeyFromUri<TKey>(HttpRequestMessage request, Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            string serviceRoot = GetServiceRootUri(request);
            uri = RebuildUri(uri, serviceRoot);
            var pathHandler = (IODataPathHandler)request.GetRequestContainer().GetService(typeof(IODataPathHandler));
            var odataPath = pathHandler.Parse(serviceRoot, uri.LocalPath, request.GetRequestContainer());

            var keySegment = odataPath.Segments.OfType<KeySegment>().FirstOrDefault();
            if (keySegment == null)
            {
                throw new InvalidOperationException("The link does not contain a key.");
            }

            var value = keySegment.Keys.FirstOrDefault().Value;
            return (TKey)value;
        }

        public static string GetSessionIdFromString(string str)
        {
            var match = Regex.Match(str, @"/\(S\((\w+)\)\)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return default(string);
        }

        public static string GetServiceRootUri(HttpRequestMessage request)
        {
            var urlHelper = request.GetUrlHelper() ?? new UrlHelper(request);
            var pathHandler = (IODataPathHandler)request.GetRequestContainer().GetService(typeof(IODataPathHandler));
            string serviceRoot = urlHelper.CreateODataLink(
                request.ODataProperties().RouteName,
                pathHandler, new List<ODataPathSegment>());
            return serviceRoot;
        }

        public static Uri RebuildUri(Uri original, string serviceRoot)
        {
            var serviceRootSessionId = GetSessionIdFromString(serviceRoot);
            if (serviceRootSessionId == null)
            {
                throw new ArgumentNullException("Key in request URI is null.");
            }

            var originalSessionId = GetSessionIdFromString(original.ToString());
            if (originalSessionId == null)
            {
                var uri = default(Uri);
                var builder = new UriBuilder(original.Scheme, original.Host, original.Port,
                    HttpContext.Current.Request.ApplicationPath);
                var beforeSessionSegement = new Uri(builder.ToString(), UriKind.Absolute).AbsoluteUri;
                var afterSessionSegment = original.AbsoluteUri.Substring(beforeSessionSegement.Length);

                var sessionSegment = string.Format("(S({0}))", HttpContext.Current.Session.SessionID);
                var path = CombineUriPaths(beforeSessionSegement, sessionSegment);
                path = CombineUriPaths(path, afterSessionSegment);
                uri = new Uri(path);

                var uriBuilder = new UriBuilder(uri);
                var baseAddressUri = new Uri(serviceRoot, UriKind.Absolute);
                uriBuilder.Host = baseAddressUri.Host;
                uri = new Uri(uriBuilder.ToString());

                return uri;
            }

            if (originalSessionId.Equals(serviceRootSessionId))
            {
                return original;
            }

            throw new InvalidOperationException(
                String.Format(
                    CultureInfo.InvariantCulture,
                    "Key '{0}' in request is not the same with that '{1}' in service root URI.",
                    originalSessionId,
                    serviceRootSessionId));
        }

        private static string CombineUriPaths(string path1, string path2)
        {
            if (path1.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                if (path2.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    path2 = path2.Substring(1);
                }
            }
            else
            {
                if (!path2.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    path2 = "/" + path2;
                }
            }

            return path1 + path2;
        }
    }
}