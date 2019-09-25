using System;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ODataServiceLifetime = Microsoft.OData.ServiceLifetime;

namespace Microsoft.Restier.Tests.Core
{

    /// <summary>
    /// Tests methods of the Core ServiceCOllectionExtensions.
    /// </summary>
    [TestClass]
    public class RestierContainerBuilderTests : RestierTestBase
    {

        [TestMethod]
        public void Constructor_CreatesServiceCollection()
        {
            var container = new RestierContainerBuilder(typeof(TestableEmptyApi));
            container.Should().NotBeNull();
            container.Services.Should().NotBeNull().And.BeEmpty();
        }

        [TestMethod]
        public void AddService_Single_ServiceType_NullShouldThrow()
        {
            var container = new RestierContainerBuilder(typeof(TestableEmptyApi));
            Action addService = () => { container.AddService(ODataServiceLifetime.Scoped, null, typeof(DefaultSubmitHandler)); };
            addService.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AddService_Single_ImplementationType_NullShouldThrow()
        {
            var container = new RestierContainerBuilder(typeof(TestableEmptyApi));
            Action addService = () => { container.AddService(ODataServiceLifetime.Scoped, typeof(DefaultSubmitHandler), implementationType: null); };
            addService.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AddService_Factory_ServiceType_NullShouldThrow()
        {
            var container = new RestierContainerBuilder(typeof(TestableEmptyApi));
            Action addService = () => { container.AddService(ODataServiceLifetime.Scoped, null, (sp) => new DefaultSubmitExecutor()); };
            addService.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AddService_Factory_ImplementationFactory_NullShouldThrow()
        {
            var container = new RestierContainerBuilder(typeof(TestableEmptyApi));
            Action addService = () => { container.AddService(ODataServiceLifetime.Scoped, typeof(DefaultSubmitHandler), implementationFactory: null); };
            addService.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void BuildContainer_HasServices()
        {
            var container = new RestierContainerBuilder(typeof(TestableEmptyApi));
            container.BuildContainer();
            container.Services.Should().HaveCount(1);
        }

    }

}