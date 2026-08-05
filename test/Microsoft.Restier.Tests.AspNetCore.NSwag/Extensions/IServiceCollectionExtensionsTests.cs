// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Extensions
{

    [TestClass]
    public class IServiceCollectionExtensionsTests
    {

        [TestMethod]
        public void AddRestierNSwag_NoSettingsAction_RegistersAtLeastOneService()
        {
            var collection = new ServiceCollection();
            collection.AddRestierNSwag();
            collection.Should().NotBeEmpty();
        }

        [TestMethod]
        public void AddRestierNSwag_WithSettingsAction_RegistersConfiguratorAsSingleton()
        {
            var collection = new ServiceCollection();
            collection.AddRestierNSwag(settings => settings.AddAlternateKeyPaths = true);

            var provider = collection.BuildServiceProvider();
            var configurator = provider.GetService<Action<Microsoft.OpenApi.OData.OpenApiConvertSettings>>();
            configurator.Should().NotBeNull("the settings action must be retrievable as a singleton service");
        }

        [TestMethod]
        public void AddRestierNSwag_RegistersApiExplorerConvention_OnMvcOptions()
        {
            var collection = new ServiceCollection();
            collection.AddOptions();
            collection.AddRestierNSwag();

            var provider = collection.BuildServiceProvider();
            var mvcOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Mvc.MvcOptions>>().Value;

            mvcOptions.Conventions
                .OfType<Microsoft.Restier.AspNetCore.NSwag.RestierControllerApiExplorerConvention>()
                .Should().HaveCount(1, "AddRestierNSwag must register the convention exactly once");
        }

    }

}
