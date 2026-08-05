// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Asp.Versioning;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Versioning;
using Microsoft.Restier.AspNetCore.Versioning.Internal;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Versioning.Internal
{

    [TestClass]
    public class RestierApiVersioningBuilderTests
    {

        [TestMethod]
        public void AddVersion_AttributeDriven_AppendsOneRegistrationPerApiVersionAttribute()
        {
            var builder = new RestierApiVersioningBuilder();

            builder.AddVersion<TwoVersionedApi>("api", _ => { });

            builder.PendingRegistrations.Should().HaveCount(2);
            builder.PendingRegistrations.Should().Contain(r =>
                r.ApiVersion == new ApiVersion(1, 0) && r.IsDeprecated && r.BasePrefix == "api");
            builder.PendingRegistrations.Should().Contain(r =>
                r.ApiVersion == new ApiVersion(2, 0) && !r.IsDeprecated && r.BasePrefix == "api");
        }

        [TestMethod]
        public void AddVersion_AttributeDriven_NoAttribute_Throws()
        {
            var builder = new RestierApiVersioningBuilder();

            Action act = () => builder.AddVersion<UnannotatedApi>("api", _ => { });

            act.Should().Throw<InvalidOperationException>().WithMessage($"*{typeof(UnannotatedApi).FullName}*");
        }

        [TestMethod]
        public void AddVersion_Imperative_AppendsRegistrationWithExplicitDeprecatedFlag()
        {
            var builder = new RestierApiVersioningBuilder();

            builder.AddVersion<UnannotatedApi>(new ApiVersion(3, 0), deprecated: true, "api", _ => { });

            builder.PendingRegistrations.Should().HaveCount(1);
            var registration = builder.PendingRegistrations[0];
            registration.ApiVersion.Should().Be(new ApiVersion(3, 0));
            registration.IsDeprecated.Should().BeTrue();
            registration.BasePrefix.Should().Be("api");
            registration.ApiType.Should().Be(typeof(UnannotatedApi));
        }

        [TestMethod]
        public void AddVersion_ReturnsSameBuilder_ForChaining()
        {
            var builder = new RestierApiVersioningBuilder();

            var returned = builder.AddVersion<TwoVersionedApi>("api", _ => { });

            returned.Should().BeSameAs(builder);
        }

        [TestMethod]
        public void AddVersion_ConfigureVersioning_RecordedOnRegistration()
        {
            var builder = new RestierApiVersioningBuilder();

            builder.AddVersion<TwoVersionedApi>(
                "api",
                _ => { },
                options => options.SegmentFormatter = ApiVersionSegmentFormatters.MajorMinor);

            builder.PendingRegistrations.Should().AllSatisfy(r =>
            {
                var opts = new RestierVersioningOptions();
                r.ApplyVersioningOptions?.Invoke(opts);
                opts.SegmentFormatter.Should().BeSameAs(ApiVersionSegmentFormatters.MajorMinor);
            });
        }

        [ApiVersion("1.0", Deprecated = true)]
        [ApiVersion("2.0")]
        private class TwoVersionedApi : ApiBase
        {
            public TwoVersionedApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class UnannotatedApi : ApiBase
        {
            public UnannotatedApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
                : base(model, queryHandler, submitHandler)
            {
            }
        }

    }

}
