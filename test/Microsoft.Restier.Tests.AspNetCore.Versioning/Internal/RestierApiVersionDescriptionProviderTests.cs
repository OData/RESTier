// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentAssertions;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    [TestClass]
    public class RestierApiVersionDescriptionProviderTests
    {

        [TestMethod]
        public void ApiVersionDescriptions_TouchesIOptionsValueBeforeReadingRegistry()
        {
            var registry = new RestierApiVersionRegistry();
            var optionsAccessed = false;
            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(_ =>
            {
                optionsAccessed = true;
                registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), false, "v1", null);
                return new ODataOptions();
            });

            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner: null);

            var descriptions = provider.ApiVersionDescriptions;

            optionsAccessed.Should().BeTrue("the provider must read IOptions<ODataOptions>.Value before reading the registry");
            descriptions.Should().HaveCount(1);
            descriptions[0].ApiVersion.Should().Be(new ApiVersion(1, 0));
            descriptions[0].GroupName.Should().Be("v1");
            descriptions[0].IsDeprecated.Should().BeFalse();
        }

        [TestMethod]
        public void ApiVersionDescriptions_PopulatesGroupNameAndDeprecatedFlagFromDescriptor()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), isDeprecated: true, "v1", null);
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), isDeprecated: false, "v2", null);

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner: null);

            provider.ApiVersionDescriptions.Should().HaveCount(2);
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.ApiVersion == new ApiVersion(1, 0) && d.IsDeprecated && d.GroupName == "v1");
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.ApiVersion == new ApiVersion(2, 0) && !d.IsDeprecated && d.GroupName == "v2");
        }

        [TestMethod]
        public void ApiVersionDescriptions_WhenInnerProviderPresent_MergesInnerAndRestierDescriptions()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), false, "v2", null);

            var inner = Substitute.For<IApiVersionDescriptionProvider>();
            inner.ApiVersionDescriptions.Returns(new[]
            {
                new ApiVersionDescription(new ApiVersion(1, 0), "controllers-v1", deprecated: false),
            });

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner);

            provider.ApiVersionDescriptions.Should().HaveCount(2);
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.GroupName == "controllers-v1");
            provider.ApiVersionDescriptions.Should().ContainSingle(d => d.GroupName == "v2");
        }

        [TestMethod]
        public void IsDeprecated_ReturnsTrueOnlyWhenAllRestierDescriptorsAreDeprecated()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(1, 0), "api", "api/v1", typeof(SampleApi), isDeprecated: true, "v1", null);
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), isDeprecated: false, "v2", null);

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner: null);

            provider.IsDeprecated(new ApiVersion(1, 0)).Should().BeTrue();
            provider.IsDeprecated(new ApiVersion(2, 0)).Should().BeFalse();
            provider.IsDeprecated(new ApiVersion(99, 0)).Should().BeFalse();
        }

        [TestMethod]
        public void IsDeprecated_DelegatesToInnerForVersionsNotInRegistry()
        {
            var registry = new RestierApiVersionRegistry();
            registry.Add(new ApiVersion(2, 0), "api", "api/v2", typeof(SampleApi), false, "v2", null);

            var inner = Substitute.For<IApiVersionDescriptionProvider>();
            // v1.0 is only in the inner provider (not in the Restier registry), and is deprecated there.
            inner.ApiVersionDescriptions.Returns(new[]
            {
                new ApiVersionDescription(new ApiVersion(1, 0), "controllers-v1", deprecated: true),
            });

            var odataOptions = Substitute.For<IOptions<ODataOptions>>();
            odataOptions.Value.Returns(new ODataOptions());
            var provider = new RestierApiVersionDescriptionProvider(odataOptions, registry, inner);

            provider.IsDeprecated(new ApiVersion(1, 0)).Should().BeTrue("inner provider says so");
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
