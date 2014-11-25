// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Domain.Tests
{
    [TestClass]
    public class InvocationContextTests
    {
        [TestMethod]
        public void NewInvocationContextIsConfiguredCorrectly()
        {
            var configuration = new DomainConfiguration();
            configuration.EnsureCommitted();
            var domainContext = new DomainContext(configuration);
            var context = new InvocationContext(domainContext);
            Assert.AreSame(domainContext, context.DomainContext);
        }

        [TestMethod]
        public void InvocationContextGetsHookPointsCorrectly()
        {
            var configuration = new DomainConfiguration();
            var singletonHookPoint = new object();
            configuration.SetHookPoint(typeof(object), singletonHookPoint);
            var multiCastHookPoint = new object();
            configuration.AddHookPoint(typeof(object), multiCastHookPoint);
            configuration.EnsureCommitted();

            var domainContext = new DomainContext(configuration);
            var context = new InvocationContext(domainContext);

            Assert.AreSame(singletonHookPoint, context.GetHookPoint<object>());
            Assert.IsTrue(context.GetHookPoints<object>()
                .SequenceEqual(new object[] { multiCastHookPoint }));
        }
    }
}
