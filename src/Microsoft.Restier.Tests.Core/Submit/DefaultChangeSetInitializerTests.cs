// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Submit
{

    /// <summary>
    /// Unit tests for the <see cref="DefaultChangeSetInitializer"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DefaultChangeSetInitializerTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;
        private DefaultChangeSetInitializer testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultChangeSetInitializerTests"/> class.
        /// </summary>
        public DefaultChangeSetInitializerTests()
        {
            testClass = new DefaultChangeSetInitializer();
            serviceProviderFixture = new ServiceProviderMock();
        }

        /// <summary>
        /// Can construct an instance of the <see cref="DefaultChangeSetInitializer"/> class.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new DefaultChangeSetInitializer();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call InitializeAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallInitializeAsync()
        {
            var context = new SubmitContext(new TestApi(serviceProviderFixture.ServiceProvider.Object), null);
            var cancellationToken = CancellationToken.None;
            await testClass.InitializeAsync(context, cancellationToken);
            context.ChangeSet.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot call InitializeAsync with a null ontext.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallInitializeAsyncWithNullContext()
        {
            Func<Task> act = () => testClass.InitializeAsync(default(SubmitContext), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
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