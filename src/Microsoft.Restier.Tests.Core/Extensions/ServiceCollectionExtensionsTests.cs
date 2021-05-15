// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit test for the <see cref="Core.ServiceCollectionExtensions"/> static class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ServiceCollectionExtensionsTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;
        private readonly IServiceProvider serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceCollectionExtensionsTests"/> class.
        /// </summary>
        public ServiceCollectionExtensionsTests()
        {
            serviceProviderFixture = new ServiceProviderMock();
            serviceProvider = serviceProviderFixture.ServiceProvider.Object;
        }

        /// <summary>
        /// Can Call HasService.
        /// </summary>
        [TestMethod]
        public void CanCallHasService()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new Mock<IQueryExecutor>().Object);

            var result = services.HasService<IQueryExecutor>();
            result.Should().BeTrue("IQueryExecutor should be there.");

            result = services.HasService<ServiceCollectionExtensionsTests>();
            result.Should().BeFalse("ServiceCollectionExtensionsTests should not be there.");
        }

        /// <summary>
        /// Cannot call HasService with a null first argument.
        /// </summary>
        [TestMethod]
        public void CannotCallHasServiceWithNullServices()
        {
            Action act = () => default(IServiceCollection).HasService<IQueryExecutor>();
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call HasServiceCount.
        /// </summary>
        [TestMethod]
        public void CanCallHasServiceCount()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new Mock<IQueryExecutor>().Object);
            services.AddSingleton(new Mock<IQueryExecutor>().Object);

            var result = services.HasServiceCount<IQueryExecutor>();

            result.Should().Be(2);
        }

        /// <summary>
        /// Cannot call HasServiceCount with a null first argument.
        /// </summary>
        [TestMethod]
        public void CannotCallHasServiceCountWithNullServices()
        {
            Action act = () => default(IServiceCollection).HasServiceCount<IQueryExecutor>();
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call AddChainedService with a factory.
        /// </summary>
        [TestMethod]
        public void CanCallAddChainedServiceWithServicesAndFactoryAndServiceLifetime()
        {
            var services = new ServiceCollection();
            var queryExecutorMock = new Mock<IQueryExecutor>();

            Func<IServiceProvider, IQueryExecutor, IQueryExecutor> factory = (s, next) => queryExecutorMock.Object;

            var serviceLifetime = ServiceLifetime.Singleton;
            services.AddChainedService(factory, serviceLifetime);

            var provider = services.BuildServiceProvider();

            var result = provider.GetRequiredService<IQueryExecutor>();

            result.Should().Be(queryExecutorMock.Object);
        }

        /// <summary>
        /// Cannot call AddChainedService with a default servicecollection.
        /// </summary>
        [TestMethod]
        public void CannotCallAddChainedServiceWithServicesAndFactoryAndServiceLifetimeWithNullServices()
        {
            Action act = () => default(IServiceCollection).AddChainedService(default(Func<IServiceProvider, IQueryExecutor, IQueryExecutor>), ServiceLifetime.Scoped);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call AddChainedService with a null factory.
        /// </summary>
        [TestMethod]
        public void CannotCallAddChainedServiceWithServicesAndFactoryAndServiceLifetimeWithNullFactory()
        {
            var services = new ServiceCollection();
            Action act = () => services.AddChainedService(default(Func<IServiceProvider, IQueryExecutor, IQueryExecutor>), ServiceLifetime.Singleton);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call AddChainedService with a service and implementation type.
        /// </summary>
        [TestMethod]
        public void CanCallAddChainedServiceWithServicesAndServiceLifetime()
        {
            var services = new ServiceCollection();

            var serviceLifetime = ServiceLifetime.Singleton;
            services.AddChainedService<IQueryExecutor, Type1>(serviceLifetime);
            services.AddChainedService<IQueryExecutor, Type2>(serviceLifetime);

            var container = services.BuildServiceProvider();

            var result = container.GetRequiredService<IQueryExecutor>();
            result.Should().BeAssignableTo<Type2>();
            var type2 = result as Type2;
            type2.Inner.Should().BeAssignableTo<Type1>();
        }

        /// <summary>
        /// Cannot call AddChainedService with a null servicecollection.
        /// </summary>
        [TestMethod]
        public void CannotCallAddChainedServiceWithServicesAndServiceLifetimeWithNullServices()
        {
            Action act = () => default(IServiceCollection).AddChainedService<IQueryExecutor, Type1>(ServiceLifetime.Transient);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call AddRestierCoreServices.
        /// </summary>
        [TestMethod]
        public void CanCallAddRestierCoreServices()
        {
            var services = new ServiceCollection();

            var result = services.AddRestierCoreServices();
            result.HasService<IQueryExecutor>().Should().BeTrue();
            result.HasService<PropertyBag>().Should().BeTrue();
        }

        /// <summary>
        /// Cannot call AddRestierCoreServices with null first argument.
        /// </summary>
        [TestMethod]
        public void CannotCallAddRestierCoreServicesWithNullServices()
        {
            Action act = () => default(IServiceCollection).AddRestierCoreServices();
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call AddRestierConventionBasedServices.
        /// </summary>
        [TestMethod]
        public void CanCallAddRestierConventionBasedServices()
        {
            var services = new ServiceCollection();

            var result = services.AddRestierConventionBasedServices(typeof(TestApi));

            result.HasService<IChangeSetItemAuthorizer>().Should().BeTrue();
            result.HasService<IChangeSetItemFilter>().Should().BeTrue();
            result.HasService<IChangeSetItemValidator>().Should().BeTrue();
            result.HasService<IQueryExpressionProcessor>().Should().BeTrue();
            result.HasService<IOperationAuthorizer>().Should().BeTrue();
            result.HasService<IOperationFilter>().Should().BeTrue();
        }

        /// <summary>
        /// Cannot call AddRestierConventionBasedServices with null first argument.
        /// </summary>
        [TestMethod]
        public void CannotCallAddRestierConventionBasedServicesWithNullServices()
        {
            Action act = () => default(IServiceCollection).AddRestierConventionBasedServices(Type.GetType("TestValue2064338526", false, false));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Cannot call AddRestierConventionBasedServices with null api type.
        /// </summary>
        [TestMethod]
        public void CannotCallAddRestierConventionBasedServicesWithNullApiType()
        {
            Action act = () => new Mock<IServiceCollection>().Object.AddRestierConventionBasedServices(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that HasService returns true correctly.
        /// </summary>
        [TestMethod]
        public void HasServiceReturnsTrueCorrectly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasService<IChangeSetInitializer>().Should().Be(true);
        }

        /// <summary>
        /// Checks that HasService returns false correctly.
        /// </summary>
        [TestMethod]
        public void HasServiceReturnsFalseCorrectly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasService<StoreModelMapper>().Should().Be(false);
        }

        /// <summary>
        /// Checks that HasServiceCount returns 0 correctly.
        /// </summary>
        [TestMethod]
        public void HasServiceCount_Returns0Correctly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasServiceCount<StoreModelMapper>().Should().Be(0);
        }

        /// <summary>
        /// Checks that HasServiceCount returns one correctly.
        /// </summary>
        [TestMethod]
        public void HasServiceCount_Returns1Correctly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.Should().HaveCount(4);
            services.HasServiceCount<IChangeSetInitializer>().Should().Be(1);
        }

        /// <summary>
        /// Checks that HasService returns 2 correctly.
        /// </summary>
        [TestMethod]
        public void HasServiceCountReturns2Correctly()
        {
            var services = new ServiceCollection();
            services.AddTestDefaultServices();
            services.AddSingleton<ISubmitExecutor, DefaultSubmitExecutor>();
            services.Should().HaveCount(5);
            services.HasServiceCount<ISubmitExecutor>().Should().Be(2);
        }

        private class Type1 : IQueryExecutor
        {
            public IQueryExecutor Inner { get; set; }

            public Task<QueryResult> ExecuteExpressionAsync<TResult>(QueryContext context, IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<QueryResult> ExecuteQueryAsync<TElement>(QueryContext context, IQueryable<TElement> query, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class Type2 : IQueryExecutor
        {
            public IQueryExecutor Inner { get; set; }

            public Task<QueryResult> ExecuteExpressionAsync<TResult>(QueryContext context, IQueryProvider queryProvider, Expression expression, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<QueryResult> ExecuteQueryAsync<TElement>(QueryContext context, IQueryable<TElement> query, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class TestApi : ApiBase
        {
            public TestApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }
    }
}