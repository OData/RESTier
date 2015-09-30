// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class ApiBaseTests
    {
        private class TestApi : ApiBase
        {
        }

        [Fact]
        public void DefaultApiBaseCanBeCreatedAndDisposed()
        {
            using (var api = new TestApi())
            {
                api.Dispose();
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        private class TestApiParticipantAttribute :
            ApiParticipantAttribute
        {
            public TestApiParticipantAttribute(string value)
            {
                this.Value = value;
            }

            public string Value { get; private set; }

            public override void Configure(
                ApiConfiguration configuration,
                Type type)
            {
                base.Configure(configuration, type);
                Assert.Same(typeof(TestApiWithParticipants), type);
                configuration.SetProperty(this.Value, true);
            }

            public override void Initialize(
                ApiContext context,
                Type type, object instance)
            {
                base.Initialize(context, type, instance);
                Assert.Same(typeof(TestApiWithParticipants), type);
                context.SetProperty(this.Value + ".Self", instance);
                context.SetProperty(this.Value, true);
            }

            public override void Dispose(
                ApiContext context,
                Type type, object instance)
            {
                Assert.Same(typeof(TestApiWithParticipants), type);
                context.SetProperty(this.Value, false);
                base.Dispose(context, type, instance);
            }
        }

        [TestApiParticipant("Test1")]
        [TestApiParticipant("Test2")]
        private class TestApiWithParticipants : ApiBase
        {
        }

        [Fact]
        public void TestApiAppliesApiParticipantsCorrectly()
        {
            IApi api = new TestApiWithParticipants();

            var configuration = api.Context.Configuration;
            Assert.True(configuration.GetProperty<bool>("Test1"));
            Assert.True(configuration.GetProperty<bool>("Test2"));

            var context = api.Context;
            Assert.True(context.GetProperty<bool>("Test1"));
            Assert.Same(api, context.GetProperty("Test1.Self"));
            Assert.True(context.GetProperty<bool>("Test2"));
            Assert.Same(api, context.GetProperty("Test2.Self"));

            (api as IDisposable).Dispose();
            Assert.False(context.GetProperty<bool>("Test2"));
            Assert.False(context.GetProperty<bool>("Test1"));
        }
    }
}
