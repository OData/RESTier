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
    /// Unit tests for the <see cref="DefaultSubmitExecutor"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DefaultSubmitExecutorTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;
        private DefaultSubmitExecutor testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSubmitExecutorTests"/> class.
        /// </summary>
        public DefaultSubmitExecutorTests()
        {
            testClass = new DefaultSubmitExecutor();
            serviceProviderFixture = new ServiceProviderMock();
        }

        /// <summary>
        /// Can construct.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new DefaultSubmitExecutor();
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Can call ExecuteSubmitAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallExecuteSubmitAsync()
        {
            var context = new SubmitContext(new TestApi(serviceProviderFixture.ServiceProvider.Object), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.ExecuteSubmitAsync(context, cancellationToken);
            result.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot call ExecuteSubmitAsync with a null context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallExecuteSubmitAsyncWithNullContext()
        {
            Func<Task> act = () => testClass.ExecuteSubmitAsync(default(SubmitContext), CancellationToken.None);
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