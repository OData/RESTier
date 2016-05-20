// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using Microsoft.Restier.Publisher.OData.Batch;
using Microsoft.Restier.Publisher.OData.Routing;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Api;

namespace Microsoft.OData.Service.Sample.Trippin
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            RegisterTrippin(config, GlobalConfiguration.DefaultServer);
        }

        public static async void RegisterTrippin(
            HttpConfiguration config, HttpServer server)
        {
            await config.MapRestierRoute<TrippinApi>(
                "TrippinApi", "api/Trippin",
                new RestierBatchHandler(server));
        }
    }
}
