// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using Microsoft.OData.Service.Sample.TrippinInMemory.Api;
using Microsoft.Restier.Publishers.OData;
using Microsoft.Restier.Publishers.OData.Batch;

namespace Microsoft.OData.Service.Sample.TrippinInMemory
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.SetUseVerboseErrors(true);
            config.MessageHandlers.Add(new ETagMessageHandler());
            config.SetUrlKeyDelimiter(ODataUrlKeyDelimiter.Slash);
            RegisterTrippin(config, GlobalConfiguration.DefaultServer);
        }

        public static async void RegisterTrippin(
            HttpConfiguration config, HttpServer server)
        {
            // enable query options for all properties
            config.Filter().Expand().Select().OrderBy().MaxTop(null).Count();
            config.SetTimeZoneInfo(TimeZoneInfo.Utc);
            await config.MapRestierRoute<TrippinApi>(
                "TrippinApi",
                "api/Trippin",
                new TrippinBatchHandler(server));
        }
    }
}