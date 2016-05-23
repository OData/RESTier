// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Provider.EntityFramework;

namespace Microsoft.Restier.Publisher.OData.Test.Model
{
    class LibraryApi : EntityFrameworkApi<LibraryContext>
    {
        // Need to register publisher services as MapRestierRoute is not called
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            base.ConfigureApi(services);
            services.AddODataServices<LibraryApi>();
            return services;
        }
    }
}
