using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.Swagger.Extensions
{

    [TestClass]
    public class IServiceCollectionExtensionsTests
    {

        [TestMethod]
        public void AddRestierSwagger_NoSettingsAction()
        {
            var collection = new ServiceCollection();
            collection.AddRestierSwagger();
            collection.Should().ContainSingle();
        }

        [TestMethod]
        public void AddRestierSwagger_SettingsAction()
        {
            var collection = new ServiceCollection();
            collection.AddRestierSwagger(settings => settings.AddAlternateKeyPaths = true);
            collection.Should().HaveCount(2);
        }

    }

}
