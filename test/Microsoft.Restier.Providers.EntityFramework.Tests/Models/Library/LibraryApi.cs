// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using System.Web.OData.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Provider.EntityFramework.Tests.Models.Library
{
    class LibraryApi : EntityFrameworkApi<LibraryContext>
    {

        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            base.ConfigureApi(services);
            services.AddService<IModelBuilder, ModelBuilder>();

            return services;
        }

        private class ModelBuilder : IModelBuilder
        {
            public async Task<IEdmModel> GetModelAsync(ModelContext context, CancellationToken cancellationToken)
            {
                var builder = new ODataConventionModelBuilder();
                builder.EntitySet<Person>("Readers");
                var model = builder.GetEdmModel();
                return model;
            }
        }
    }
}
