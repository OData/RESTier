// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core.Extensions
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddRestierCoreServices_RegistersDefaultExpandCycleDetector()
        {
            var services = new ServiceCollection();

            // AddRestierCoreServices is internal; InternalsVisibleTo grants access from this test assembly.
            services.AddRestierCoreServices();

            using var provider = services.BuildServiceProvider();
            provider.GetService<IExpandCycleDetector>()
                .Should().NotBeNull()
                .And.BeOfType<DefaultExpandCycleDetector>();
        }
    }
}
