// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class DomainConfigurationTests
    {
        [Fact]
        public void EmptyConfigurationIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();

            Assert.Null(configuration.Key);
            Assert.False(configuration.IsCommitted);
        }

        [Fact]
        public void CachedConfigurationIsCachedCorrectly()
        {
            var key = Guid.NewGuid().ToString();
            var configuration = new DomainConfiguration(key);

            var cached = DomainConfiguration.FromKey(key);
            Assert.Same(configuration, cached);

            DomainConfiguration.Invalidate(key);
            Assert.Null(DomainConfiguration.FromKey(key));
        }

        [Fact]
        public void CommittedConfigurationIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();

            configuration.EnsureCommitted();
            Assert.True(configuration.IsCommitted);

            configuration.EnsureCommitted();
            Assert.True(configuration.IsCommitted);
        }

        [Fact]
        public void CommittedConfigurationCannotAddHookPoint()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();

            Assert.Throws<InvalidOperationException>(() => configuration.AddHookPoint(typeof(object), new object()));
        }

        [Fact]
        public void CommittedConfigurationCannotAddHookHandler()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();

            Assert.Throws<InvalidOperationException>(
                () => configuration.AddHookHandler<IHookHandler>(new TestModelBuilder()));
        }

        [Fact]
        public void ConfigurationCannotAddHookHandlerOfWrongType()
        {
            var configuration = new DomainConfiguration();
            Assert.Throws<InvalidOperationException>(
                () => configuration.AddHookHandler<TestModelBuilder>(new TestModelBuilder()));
        }

        [Fact]
        public void ConfigurationCannotAddHookPointOfWrongType()
        {
            var configuration = new DomainConfiguration();
            Assert.Throws<ArgumentException>(() => configuration.AddHookPoint(typeof(IDisposable), new object()));
        }

        [Fact]
        public void ConfigurationRegistersHookPointsCorrectly()
        {
            var configuration = new DomainConfiguration();

            Assert.Null(configuration.GetHookHandler<IHookA>());
            Assert.False(configuration.HasHookPoints(typeof(object)));
            Assert.False(configuration.GetHookPoints<object>().Any());

            var singletonHookPoint = new HookA();
            configuration.AddHookHandler<IHookA>(singletonHookPoint);
            Assert.Same(singletonHookPoint, configuration.GetHookHandler<IHookA>());
            Assert.False(configuration.HasHookPoints(typeof(object)));
            Assert.False(configuration.GetHookPoints<object>().Any());

            var multiCastHookPoint1 = new object();
            configuration.AddHookPoint(typeof(object), multiCastHookPoint1);
            Assert.Same(singletonHookPoint, configuration.GetHookHandler<IHookA>());
            Assert.True(configuration.HasHookPoints(typeof(object)));
            Assert.True(configuration.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1 }));

            var multiCastHookPoint2 = new object();
            configuration.AddHookPoint(typeof(object), multiCastHookPoint2);
            Assert.True(configuration.HasHookPoints(typeof(object)));
            Assert.True(configuration.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint2 }));
        }

        private class TestModelBuilder : IModelBuilder
        {
            public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private interface IHookA : IHookHandler
        {
        }

        private class HookA : IHookA
        {
        }
    }
}
