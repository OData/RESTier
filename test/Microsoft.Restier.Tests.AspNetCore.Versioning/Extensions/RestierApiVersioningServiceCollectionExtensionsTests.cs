// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Extensions
{

    [TestClass]
    public class RestierApiVersioningServiceCollectionExtensionsTests
    {

        [TestMethod]
        public void AddRestierApiVersioning_RegistersRegistryAsSingleton()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b => { });

            services.Should().Contain(d =>
                d.ServiceType == typeof(IRestierApiVersionRegistry) && d.Lifetime == ServiceLifetime.Singleton);
            services.Should().Contain(d =>
                d.ServiceType == typeof(RestierApiVersionRegistry) && d.Lifetime == ServiceLifetime.Singleton);
        }

        [TestMethod]
        public void AddRestierApiVersioning_RegistersBuilderAsSingletonInstance()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b => { });

            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RestierApiVersioningBuilder));
            descriptor.Should().NotBeNull();
            descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
            descriptor.ImplementationInstance.Should().NotBeNull();
        }

        [TestMethod]
        public void AddRestierApiVersioning_CalledTwice_AppendsToSameBuilder()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), deprecated: false, "api", _ => { }));

            services.AddRestierApiVersioning(b =>
                b.AddVersion<SampleApi>(new ApiVersion(2, 0), deprecated: false, "api", _ => { }));

            services.Where(d => d.ServiceType == typeof(RestierApiVersioningBuilder)).Should().HaveCount(1);

            var builder = (RestierApiVersioningBuilder)services
                .Single(d => d.ServiceType == typeof(RestierApiVersioningBuilder)).ImplementationInstance;
            builder.PendingRegistrations.Should().HaveCount(2);
            builder.PendingRegistrations.Should().Contain(p => p.ApiVersion == new ApiVersion(1, 0));
            builder.PendingRegistrations.Should().Contain(p => p.ApiVersion == new ApiVersion(2, 0));
        }

        [TestMethod]
        public void AddRestierApiVersioning_RegistersConfigureOptions()
        {
            var services = new ServiceCollection();

            services.AddRestierApiVersioning(b => { });

            services.Should().Contain(d =>
                d.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.OData.ODataOptions>)
                && d.ImplementationType == typeof(RestierApiVersioningOptionsConfigurator));
        }

        [TestMethod]
        public void AddRestierApiVersioning_ReplacesAnyPriorIApiVersionDescriptionProviderWithComposite()
        {
            var services = new ServiceCollection();
            var priorProvider = NSubstitute.Substitute.For<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
            services.AddSingleton(priorProvider);

            services.AddRestierApiVersioning(b => { });

            var providerDescriptors = services
                .Where(d => d.ServiceType == typeof(Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider))
                .ToArray();
            providerDescriptors.Should().HaveCount(1);
            providerDescriptors[0].ImplementationFactory.Should().NotBeNull(
                "the composite is registered via factory so it can capture and inject the prior provider");
        }

        [TestMethod]
        public void AddRestierApiVersioning_CalledTwice_DoesNotDoubleReplaceProvider()
        {
            var services = new ServiceCollection();
            var priorProvider = NSubstitute.Substitute.For<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
            services.AddSingleton(priorProvider);

            services.AddRestierApiVersioning(b => { });
            services.AddRestierApiVersioning(b => { });

            services
                .Where(d => d.ServiceType == typeof(Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider))
                .Should().HaveCount(1);
        }

        private class SampleApi : ApiBase
        {
            public SampleApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

    }

}
