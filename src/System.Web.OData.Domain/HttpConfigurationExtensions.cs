// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.ComponentModel;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData.Domain.Batch;
using System.Web.OData.Domain.Routing;
using System.Web.OData.Extensions;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.Data.Domain;

namespace System.Web.OData.Domain
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpConfigurationExtensions
    {
        // TODO: this can't be async; model should be loaded later on demand
        public static async Task<ODataRoute> MapODataDomainRoute<TController>(
            this HttpConfiguration config, string routeName, string routePrefix,
            ODataDomainBatchHandler batchHandler = null)
            where TController : ODataDomainController, new()
        {
            using (TController controller = new TController())
            {
                var model = await controller.Domain.GetModelAsync();
                var conventions = ODataRoutingConventions.CreateDefault();
                conventions.Insert(0, new DefaultODataRoutingConvention(typeof(TController).Name));
                conventions.Insert(0, new AttributeRoutingConvention(model, config));

                if (batchHandler != null && batchHandler.ContextFactory == null)
                {
                    batchHandler.ContextFactory = () => new TController().Domain.Context;
                }

                return config.MapODataServiceRoute(
                    routeName,
                    routePrefix,
                    model,
                    new DefaultODataPathHandler(),
                    conventions,
                    batchHandler);
            }
        }
    }
}
