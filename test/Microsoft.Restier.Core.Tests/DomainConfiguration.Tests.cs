// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
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
            Assert.NotNull(configuration.BaseConfiguration);
            Assert.Null(configuration.BaseConfiguration.Key);
            Assert.Null(configuration.BaseConfiguration.BaseConfiguration);
            Assert.True(configuration.BaseConfiguration.IsCommitted);
            Assert.False(configuration.IsCommitted);

            Assert.True(configuration.GetHookPoint<IQueryHandler>() is DefaultQueryHandler);
            Assert.True(configuration.GetHookPoint<ISubmitHandler>() is DefaultSubmitHandler);
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
        public void CommittedConfigurationCannotSetHookPoint()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();

            Assert.Throws<InvalidOperationException>(() => configuration.SetHookPoint(typeof(object), new object()));
        }

        [Fact]
        public void CommittedConfigurationCannotAddHookPoint()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();

            Assert.Throws<InvalidOperationException>(() => configuration.AddHookPoint(typeof(object), new object()));
        }

        [Fact]
        public void ConfigurationCannotSetHookPointOfWrongType()
        {
            var configuration = new DomainConfiguration();
            Assert.Throws<ArgumentException>(() => configuration.SetHookPoint(typeof(IDisposable), new object()));
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

            Assert.False(configuration.HasHookPoint(typeof(object)));
            Assert.Null(configuration.GetHookPoint<object>());
            Assert.False(configuration.HasHookPoints(typeof(object)));
            Assert.False(configuration.GetHookPoints<object>().Any());

            var singletonHookPoint = new object();
            configuration.SetHookPoint(typeof(object), singletonHookPoint);
            Assert.True(configuration.HasHookPoint(typeof(object)));
            Assert.Same(singletonHookPoint,
                configuration.GetHookPoint<object>());
            Assert.False(configuration.HasHookPoints(typeof(object)));
            Assert.False(configuration.GetHookPoints<object>().Any());

            var multiCastHookPoint1 = new object();
            configuration.AddHookPoint(typeof(object), multiCastHookPoint1);
            Assert.Same(singletonHookPoint,
                configuration.GetHookPoint<object>());
            Assert.True(configuration.HasHookPoints(typeof(object)));
            Assert.True(configuration.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1 }));

            var multiCastHookPoint2 = new object();
            configuration.AddHookPoint(typeof(object), multiCastHookPoint2);
            Assert.True(configuration.HasHookPoints(typeof(object)));
            Assert.True(configuration.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint2 }));
        }

        [Fact]
        public void DerivedConfigurationIsConfiguredCorrectly()
        {
            var baseConfig = new DomainConfiguration();
            var derivedConfig = new DomainConfiguration(baseConfig);

            Assert.Same(baseConfig, derivedConfig.BaseConfiguration);

            Assert.False(derivedConfig.HasProperty("Test"));
            Assert.Null(derivedConfig.GetProperty<string>("Test"));

            baseConfig.SetProperty("Test", "Test");
            Assert.True(derivedConfig.HasProperty("Test"));
            Assert.Equal("Test", derivedConfig.GetProperty<string>("Test"));

            derivedConfig.SetProperty("Test", "Test2");
            Assert.True(derivedConfig.HasProperty("Test"));
            Assert.Equal("Test2", derivedConfig.GetProperty<string>("Test"));
            Assert.Equal("Test", baseConfig.GetProperty<string>("Test"));

            derivedConfig.ClearProperty("Test");
            Assert.True(derivedConfig.HasProperty("Test"));
            Assert.Equal("Test", derivedConfig.GetProperty<string>("Test"));

            var singletonHookPoint = new object();
            baseConfig.SetHookPoint(typeof(object), singletonHookPoint);
            Assert.True(derivedConfig.HasHookPoint(typeof(object)));
            Assert.Same(singletonHookPoint,
                derivedConfig.GetHookPoint<object>());

            var derivedSingletonHookPoint = new object();
            derivedConfig.SetHookPoint(typeof(object), derivedSingletonHookPoint);
            Assert.True(derivedConfig.HasHookPoint(typeof(object)));
            Assert.Same(derivedSingletonHookPoint,
                derivedConfig.GetHookPoint<object>());
            Assert.Same(singletonHookPoint,
                baseConfig.GetHookPoint<object>());

            var multiCastHookPoint1 = new object();
            baseConfig.AddHookPoint(typeof(object), multiCastHookPoint1);
            Assert.True(derivedConfig.HasHookPoints(typeof(object)));
            Assert.True(derivedConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1 }));

            var multiCastHookPoint2 = new object();
            derivedConfig.AddHookPoint(typeof(object), multiCastHookPoint2);
            Assert.True(derivedConfig.HasHookPoints(typeof(object)));
            Assert.True(derivedConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint2 }));
            Assert.True(baseConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1 }));

            var multiCastHookPoint3 = new object();
            baseConfig.AddHookPoint(typeof(object), multiCastHookPoint3);
            Assert.True(derivedConfig.HasHookPoints(typeof(object)));
            Assert.True(derivedConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint3, multiCastHookPoint2 }));
            Assert.True(baseConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint3 }));
        }

        [Fact]
        public void DerivedConfigurationCannotCommitWithUncommittedBase()
        {
            var baseConfig = new DomainConfiguration();
            var derivedConfig = new DomainConfiguration(baseConfig);

            Assert.Throws<InvalidOperationException>(() => derivedConfig.EnsureCommitted());
        }
    }
}
