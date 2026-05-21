// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.DependencyInjection;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    [TestClass]
    public class RestierApiVersioningOptionsConfiguratorTests
    {

        [TestMethod]
        public void Configure_DefaultFormatter_ComposesPrefixAsBaseSlashVMajor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), deprecated: false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("api/v1");
            registry.Descriptors.Should().HaveCount(1);
            registry.Descriptors[0].RoutePrefix.Should().Be("api/v1");
            registry.Descriptors[0].BasePrefix.Should().Be("api");
            registry.Descriptors[0].GroupName.Should().Be("v1");
            registry.Descriptors[0].Version.Should().Be("1.0");
        }

        [TestMethod]
        public void Configure_EmptyBasePrefix_ComposesPrefixAsVMajor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(2, 0), deprecated: false, "", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("v2");
            registry.Descriptors[0].RoutePrefix.Should().Be("v2");
            registry.Descriptors[0].BasePrefix.Should().Be("");
            registry.Descriptors[0].GroupName.Should().Be("v2");
        }

        [TestMethod]
        public void Configure_MajorMinorFormatter_ComposesPrefixAsBaseSlashVMajorDotMinor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 5), deprecated: false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.SegmentFormatter = ApiVersionSegmentFormatters.MajorMinor));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("api/v1.5");
            registry.Descriptors[0].RoutePrefix.Should().Be("api/v1.5");
            registry.Descriptors[0].GroupName.Should().Be("v1.5");
        }

        [TestMethod]
        public void Configure_ExplicitRoutePrefix_UsedVerbatim_GroupNameStillFromFormatter()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 0), deprecated: false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.ExplicitRoutePrefix = "legacy/v1-old"));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("legacy/v1-old");
            registry.Descriptors[0].RoutePrefix.Should().Be("legacy/v1-old");
            registry.Descriptors[0].GroupName.Should().Be("v1");
        }

        [TestMethod]
        public void Configure_PassesSunsetDateThroughToDescriptor()
        {
            var sunset = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 0), deprecated: false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.SunsetDate = sunset));

            configurator.Configure(options);

            registry.Descriptors[0].SunsetDate.Should().Be(sunset);
        }

        [TestMethod]
        public void Configure_DuplicateApiVersionAndBasePrefix_Throws()
        {
            var (configurator, _, options) = BuildSubject(b =>
            {
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
                b.AddVersion<OtherApi>(new ApiVersion(1, 0), false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
            });

            Action act = () => configurator.Configure(options);

            act.Should().Throw<InvalidOperationException>().WithMessage("*1.0*api*");
        }

        [TestMethod]
        public void Configure_RunOnlyOnce_GuardsAgainstReEntry()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "api", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);
            configurator.Configure(options);

            registry.Descriptors.Should().HaveCount(1);
            options.RouteComponents.Where(kvp => kvp.Key == "api/v1").Should().HaveCount(1);
        }

        [TestMethod]
        public void Configure_NormalizesBasePrefix_TrailingSlashStrippedFromRouteAndDescriptor()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "api/", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>()));

            configurator.Configure(options);

            options.RouteComponents.Should().ContainKey("api/v1");
            registry.Descriptors[0].RoutePrefix.Should().Be("api/v1");
            registry.Descriptors[0].BasePrefix.Should().Be("api",
                "trailing slash on basePrefix must be normalized so it groups with non-slashed registrations");
        }

        [TestMethod]
        public void Configure_ExplicitGroupNameFormatter_OverridesDefault()
        {
            var (configurator, registry, options) = BuildSubject(b =>
                b.AddVersion<SampleApi>(
                    new ApiVersion(1, 0), false, "api",
                    svc => svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>(),
                    opts => opts.GroupNameFormatter = v => $"orders-v{v.MajorVersion}"));

            configurator.Configure(options);

            registry.Descriptors[0].GroupName.Should().Be("orders-v1");
        }

        [TestMethod]
        public void Configure_GroupNameCollisionAcrossBasePrefixes_Throws()
        {
            var (configurator, _, options) = BuildSubject(b =>
            {
                b.AddVersion<SampleApi>(new ApiVersion(1, 0), false, "orders", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
                b.AddVersion<OtherApi>(new ApiVersion(1, 0), false, "inventory", svc =>
                    svc.AddSingleton<IChainedService<IModelBuilder>, SampleModelBuilder>());
            });

            Action act = () => configurator.Configure(options);

            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*v1*orders*inventory*GroupNameFormatter*",
                    "the configurator must reject duplicate group names with guidance");
        }

        private static (RestierApiVersioningOptionsConfigurator configurator, RestierApiVersionRegistry registry, ODataOptions options) BuildSubject(
            Action<IRestierApiVersioningBuilder> configure)
        {
            var builder = new RestierApiVersioningBuilder();
            configure(builder);
            var registry = new RestierApiVersionRegistry();
            var configurator = new RestierApiVersioningOptionsConfigurator(builder, registry);
            return (configurator, registry, new ODataOptions());
        }

        private class SampleApi : ApiBase
        {
            public SampleApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class OtherApi : ApiBase
        {
            public OtherApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class SampleEntity
        {
            public int Id { get; set; }
        }

        private class SampleModelBuilder : IModelBuilder
        {
            public IModelBuilder Inner { get; set; }

            public IEdmModel GetEdmModel()
            {
                var b = new ODataConventionModelBuilder();
                b.EntitySet<SampleEntity>("Items");
                return b.GetEdmModel();
            }
        }

    }

}
