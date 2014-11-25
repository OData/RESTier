// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Domain.Tests
{
    using System;
    using System.Collections.Generic;
    using Model;
    using Query;
    using Submit;

    [TestClass]
    public class DomainConfigurationTests
    {
        [TestMethod]
        public void EmptyConfigurationIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();

            Assert.IsNull(configuration.Key);
            Assert.IsNotNull(configuration.BaseConfiguration);
            Assert.IsNull(configuration.BaseConfiguration.Key);
            Assert.IsNull(configuration.BaseConfiguration.BaseConfiguration);
            Assert.IsTrue(configuration.BaseConfiguration.IsCommitted);
            Assert.IsFalse(configuration.IsCommitted);

            Assert.IsTrue(configuration.GetHookPoint<IModelHandler>() is DefaultModelHandler);
            Assert.IsTrue(configuration.GetHookPoint<IQueryHandler>() is DefaultQueryHandler);
            Assert.IsTrue(configuration.GetHookPoint<ISubmitHandler>() is DefaultSubmitHandler);
        }

        [TestMethod]
        public void CachedConfigurationIsCachedCorrectly()
        {
            var key = Guid.NewGuid().ToString();
            var configuration = new DomainConfiguration(key);

            var cached = DomainConfiguration.FromKey(key);
            Assert.AreSame(configuration, cached);

            DomainConfiguration.Invalidate(key);
            Assert.IsNull(DomainConfiguration.FromKey(key));
        }

        [TestMethod]
        public void CommittedConfigurationIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();

            configuration.EnsureCommitted();
            Assert.IsTrue(configuration.IsCommitted);

            configuration.EnsureCommitted();
            Assert.IsTrue(configuration.IsCommitted);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CommittedConfigurationCannotSetHookPoint()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();

            configuration.SetHookPoint(typeof(object), new object());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CommittedConfigurationCannotAddHookPoint()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();

            configuration.AddHookPoint(typeof(object), new object());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConfigurationCannotSetHookPointOfWrongType()
        {
            var configuration = new DomainConfiguration();
            configuration.SetHookPoint(typeof(IDisposable), new object());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConfigurationCannotAddHookPointOfWrongType()
        {
            var configuration = new DomainConfiguration();
            configuration.AddHookPoint(typeof(IDisposable), new object());
        }

        [TestMethod]
        public void ConfigurationRegistersHookPointsCorrectly()
        {
            var configuration = new DomainConfiguration();

            Assert.IsFalse(configuration.HasHookPoint(typeof(object)));
            Assert.IsNull(configuration.GetHookPoint<object>());
            Assert.IsFalse(configuration.HasHookPoints(typeof(object)));
            Assert.IsFalse(configuration.GetHookPoints<object>().Any());

            var singletonHookPoint = new object();
            configuration.SetHookPoint(typeof(object), singletonHookPoint);
            Assert.IsTrue(configuration.HasHookPoint(typeof(object)));
            Assert.AreSame(singletonHookPoint,
                configuration.GetHookPoint<object>());
            Assert.IsFalse(configuration.HasHookPoints(typeof(object)));
            Assert.IsFalse(configuration.GetHookPoints<object>().Any());

            var multiCastHookPoint1 = new object();
            configuration.AddHookPoint(typeof(object), multiCastHookPoint1);
            Assert.AreSame(singletonHookPoint,
                configuration.GetHookPoint<object>());
            Assert.IsTrue(configuration.HasHookPoints(typeof(object)));
            Assert.IsTrue(configuration.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1 }));

            var multiCastHookPoint2 = new object();
            configuration.AddHookPoint(typeof(object), multiCastHookPoint2);
            Assert.IsTrue(configuration.HasHookPoints(typeof(object)));
            Assert.IsTrue(configuration.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint2 }));
        }

        [TestMethod]
        public void ConfigurationWithProfilerReturnsProfiledHookPoints()
        {
            var configuration = new DomainConfiguration();
            var profiler = new TestDomainProfiler();
            configuration.AddHookPoint(typeof(IDomainProfiler), profiler);

            // Profilers are not themselves profiled
            Assert.AreSame(profiler, configuration
                .GetHookPoints<IDomainProfiler>().Single());

            var singletonHookPoint = new object();
            var singletonHookPointProfiled = new object();
            profiler.RegisterProfiledInstance(
                singletonHookPoint, singletonHookPointProfiled);
            configuration.SetHookPoint(typeof(object), singletonHookPoint);
            Assert.AreSame(singletonHookPointProfiled,
                configuration.GetHookPoint<object>());

            var multiCastHookPoint = new object();
            var multiCastHookPointProfiled = new object();
            profiler.RegisterProfiledInstance(
                multiCastHookPoint, multiCastHookPointProfiled);
            configuration.AddHookPoint(typeof(object), multiCastHookPoint);
            Assert.IsTrue(configuration.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPointProfiled }));
        }

        private class TestDomainProfiler : IDomainProfiler
        {
            IDictionary<object, object> profiledInstances =
                new Dictionary<object, object>();

            public void RegisterProfiledInstance<T>(
                T instance, T profiledInstance)
            {
                this.profiledInstances.Add(instance, profiledInstance);
            }

            public T Profile<T>(T instance)
            {
                Assert.IsTrue(this.profiledInstances.ContainsKey(instance));
                return (T)this.profiledInstances[instance];
            }
        } 

        [TestMethod]
        public void DerivedConfigurationIsConfiguredCorrectly()
        {
            var baseConfig = new DomainConfiguration();
            var derivedConfig = new DomainConfiguration(baseConfig);

            Assert.AreSame(baseConfig, derivedConfig.BaseConfiguration);

            Assert.IsFalse(derivedConfig.HasProperty("Test"));
            Assert.IsNull(derivedConfig.GetProperty<string>("Test"));

            baseConfig.SetProperty("Test", "Test");
            Assert.IsTrue(derivedConfig.HasProperty("Test"));
            Assert.AreEqual("Test", derivedConfig.GetProperty<string>("Test"));

            derivedConfig.SetProperty("Test", "Test2");
            Assert.IsTrue(derivedConfig.HasProperty("Test"));
            Assert.AreEqual("Test2", derivedConfig.GetProperty<string>("Test"));
            Assert.AreEqual("Test", baseConfig.GetProperty<string>("Test"));

            derivedConfig.ClearProperty("Test");
            Assert.IsTrue(derivedConfig.HasProperty("Test"));
            Assert.AreEqual("Test", derivedConfig.GetProperty<string>("Test"));

            var singletonHookPoint = new object();
            baseConfig.SetHookPoint(typeof(object), singletonHookPoint);
            Assert.IsTrue(derivedConfig.HasHookPoint(typeof(object)));
            Assert.AreSame(singletonHookPoint,
                derivedConfig.GetHookPoint<object>());

            var derivedSingletonHookPoint = new object();
            derivedConfig.SetHookPoint(typeof(object), derivedSingletonHookPoint);
            Assert.IsTrue(derivedConfig.HasHookPoint(typeof(object)));
            Assert.AreSame(derivedSingletonHookPoint,
                derivedConfig.GetHookPoint<object>());
            Assert.AreSame(singletonHookPoint,
                baseConfig.GetHookPoint<object>());

            var multiCastHookPoint1 = new object();
            baseConfig.AddHookPoint(typeof(object), multiCastHookPoint1);
            Assert.IsTrue(derivedConfig.HasHookPoints(typeof(object)));
            Assert.IsTrue(derivedConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1 }));

            var multiCastHookPoint2 = new object();
            derivedConfig.AddHookPoint(typeof(object), multiCastHookPoint2);
            Assert.IsTrue(derivedConfig.HasHookPoints(typeof(object)));
            Assert.IsTrue(derivedConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint2 }));
            Assert.IsTrue(baseConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1 }));

            var multiCastHookPoint3 = new object();
            baseConfig.AddHookPoint(typeof(object), multiCastHookPoint3);
            Assert.IsTrue(derivedConfig.HasHookPoints(typeof(object)));
            Assert.IsTrue(derivedConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint3, multiCastHookPoint2 }));
            Assert.IsTrue(baseConfig.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint1, multiCastHookPoint3 }));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DerivedConfigurationCannotCommitWithUncommittedBase()
        {
            var baseConfig = new DomainConfiguration();
            var derivedConfig = new DomainConfiguration(baseConfig);

            derivedConfig.EnsureCommitted();
        }
    }
}
