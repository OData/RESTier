// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
