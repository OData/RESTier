// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ApiBase"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class ApiBaseTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;
        private TestApiBase testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiBaseTests"/> class.
        /// </summary>
        public ApiBaseTests()
        {
            serviceProviderFixture = new ServiceProviderMock();
            testClass = new TestApiBase(serviceProviderFixture.ServiceProvider.Object);
        }

        /// <summary>
        /// Cannot construct with a null Service provider.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullServiceProvider()
        {
            Action act = () => new TestApiBase(default(IServiceProvider));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Can call SubmitAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallSubmitAsync()
        {
            var changeSet = new ChangeSet();
            changeSet.Entries.Add(
                new DataModificationItem(
                    "Tests",
                    typeof(Test),
                    typeof(Test),
                    RestierEntitySetOperation.Update,
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>(),
                    new Dictionary<string, object>()));
            var cancellationToken = CancellationToken.None;

            bool authCalled = false;

            // check for authorizer invocation.
            serviceProviderFixture.ChangeSetItemAuthorizer
                .Setup(x => x.AuthorizeAsync(It.IsAny<SubmitContext>(), It.IsAny<ChangeSetItem>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    authCalled = true;
                    return Task.FromResult(authCalled);
                });

            bool preFilterCalled = false;
            bool postFilterCalled = false;

            // check for filter invocation.
            serviceProviderFixture.ChangeSetItemFilter
                .Setup(x => x.OnChangeSetItemProcessingAsync(
                    It.IsAny<SubmitContext>(),
                    It.IsAny<ChangeSetItem>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    preFilterCalled = true;
                    return Task.CompletedTask;
                });
            serviceProviderFixture.ChangeSetItemFilter
                .Setup(x => x.OnChangeSetItemProcessedAsync(
                    It.IsAny<SubmitContext>(),
                    It.IsAny<ChangeSetItem>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    postFilterCalled = true;
                    return Task.CompletedTask;
                });

            bool validationCalled = false;

            // check for validator invocation.
            serviceProviderFixture.ChangeSetItemValidator
                .Setup(x => x.ValidateChangeSetItemAsync(
                    It.IsAny<SubmitContext>(),
                    It.IsAny<ChangeSetItem>(),
                    It.IsAny<Collection<ChangeSetItemValidationResult>>(),
                    It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    validationCalled = true;
                    return Task.FromResult(authCalled);
                });

            var result = await testClass.SubmitAsync(changeSet, cancellationToken);
            authCalled.Should().BeTrue("AuthorizeAsync was not called");
            preFilterCalled.Should().BeTrue("OnChangeSetItemProcessingAsync was not called");
            postFilterCalled.Should().BeTrue("OnChangeSetItemProcessedAsync was not called");
            validationCalled.Should().BeTrue("ValidateChangeSetItemAsync was not called");
        }

        /// <summary>
        /// Can call SubmitAsync with unprocessed results. They should be returned immediately.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallSubmitAsyncWithUnprocessedResults()
        {
            var changeSet = new ChangeSet();
            var cancellationToken = CancellationToken.None;
            var submitResult = new SubmitResult(changeSet);

            // setup changeSetInitializer to produce a result immediately.
            serviceProviderFixture.ChangeSetInitializer
                .Setup(x => x.InitializeAsync(It.IsAny<SubmitContext>(), It.IsAny<CancellationToken>()))
                .Returns<SubmitContext, CancellationToken>((s, c) =>
            {
                s.Result = submitResult;
                return Task.CompletedTask;
                });
            var result = await testClass.SubmitAsync(changeSet, cancellationToken);
            result.Should().Be(submitResult);
        }

        /// <summary>
        /// Cannot call SubmitAsync with a null changeset.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallSubmitAsyncWithNullChangeSet()
        {
            serviceProviderFixture.ChangeSetInitializer.Reset();
            Func<Task> act = () => testClass.SubmitAsync(default(ChangeSet), CancellationToken.None);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        /// <summary>
        /// Can call Dispose with no parameters.
        /// </summary>
        [TestMethod]
        public void CanCallDisposeWithNoParameters()
        {
            testClass.Dispose();
            testClass.Disposed.Should().BeTrue("ApiBase instance is not disposed.");
        }

        /// <summary>
        /// ServiceProvider is initialized correctly.
        /// </summary>
        [TestMethod]
        public void ServiceProviderIsInitializedCorrectly()
        {
            testClass.ServiceProvider.Should().Be(serviceProviderFixture.ServiceProvider.Object);
        }

        private class TestApiBase : ApiBase
        {
            public TestApiBase(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public bool Disposed { get; private set; }

            protected override void Dispose(bool disposing)
            {
                Disposed = true;
                base.Dispose(disposing);
            }
        }

        private class Test
        {
            public string Name { get; set; }
        }
    }
}