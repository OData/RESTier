// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Providers.EntityFramework;

namespace Microsoft.Restier.Publishers.OData.Test.Model
{
    class LibraryApi : EntityFrameworkApi<LibraryContext>
    {
        // Need to register publisher services as MapRestierRoute is not called
        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            EntityFrameworkApi<LibraryContext>.ConfigureApi(apiType, services);
            services.AddODataServices<LibraryApi>();
            return services;
        }

        public LibraryApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }
}
