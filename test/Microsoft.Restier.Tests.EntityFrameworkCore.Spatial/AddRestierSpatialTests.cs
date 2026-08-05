// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Spatial;
using Microsoft.Restier.EntityFrameworkCore.Spatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.EntityFrameworkCore.Spatial
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

            sp.GetRequiredService<ISpatialTypeConverter>().Should().BeOfType<NtsSpatialConverter>();
            sp.GetRequiredService<ISpatialModelMetadataProvider>().Should().BeOfType<NtsSpatialModelMetadataProvider>();
        }

        [TestMethod]
        public void AddRestierSpatial_is_idempotent()
        {
            var services = new ServiceCollection();
            services.AddRestierSpatial();
            services.AddRestierSpatial();

            var sp = services.BuildServiceProvider();
            sp.GetServices<ISpatialTypeConverter>().Should().ContainSingle();
        }
    }
}
