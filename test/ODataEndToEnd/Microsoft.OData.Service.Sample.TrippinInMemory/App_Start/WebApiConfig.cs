// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using System.Web.OData;
using Microsoft.OData.Service.Sample.TrippinInMemory.Api;
using Microsoft.Restier.Publishers.OData;
using Microsoft.Restier.Publishers.OData.Batch;

namespace Microsoft.OData.Service.Sample.TrippinInMemory
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            RegisterTrippin(config, GlobalConfiguration.DefaultServer);
            config.SetUseVerboseErrors(true);
            config.MessageHandlers.Add(new ETagMessageHandler());
        }

        public static async void RegisterTrippin(
            HttpConfiguration config, HttpServer server)
        {
            await config.MapRestierRoute<TrippinApi>(
                "TrippinApi",
                 "api/Trippin",
                new RestierBatchHandler(server));
        }
    }
}