// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Web.Http;
using Microsoft.Restier.WebApi.Batch;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Domain;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin
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
            await config.MapODataDomainRoute<TrippinDomain>(
                "TrippinApi", "api/Trippin",
                new ODataDomainBatchHandler(server));
        }
    }
}
