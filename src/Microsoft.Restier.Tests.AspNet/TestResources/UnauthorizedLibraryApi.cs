using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    /// <summary>
    /// A <see cref="LibraryApi"/> implementation that registers a <see cref="DisallowEverythingAuthorizer"/> with the DI container.
    /// </summary>
    internal class UnauthorizedLibraryApi : LibraryApi
    {
        public UnauthorizedLibraryApi(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            return LibraryApi.ConfigureApi(apiType, services)
                .AddSingleton<IQueryExpressionAuthorizer, DisallowEverythingAuthorizer>();
        }

    }

}