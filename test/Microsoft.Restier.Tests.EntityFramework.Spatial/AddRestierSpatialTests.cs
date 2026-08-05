// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFramework.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFramework.Spatial
{
    [TestClass]
    public class AddRestierSpatialTests
    {
        [TestMethod]
        public void AddRestierSpatial_registers_converter_and_provider()
        {
            var services = new ServiceCollection();
            services.AddRestierSpatial();

            var sp = services.BuildServiceProvider();

            sp.GetRequiredService<ISpatialTypeConverter>().Should().BeOfType<DbSpatialConverter>();
            sp.GetRequiredService<ISpatialModelMetadataProvider>().Should().BeOfType<DbSpatialModelMetadataProvider>();
        }

        [TestMethod]
        public void AddRestierSpatial_is_idempotent()
        {
            var services = new ServiceCollection();
            services.AddRestierSpatial();
            services.AddRestierSpatial();

            var sp = services.BuildServiceProvider();
            var converters = sp.GetServices<ISpatialTypeConverter>();
            converters.Should().ContainSingle();
        }
    }
}
