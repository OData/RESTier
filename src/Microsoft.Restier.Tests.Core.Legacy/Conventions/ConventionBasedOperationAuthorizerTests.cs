// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedOperationAuthorizer"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ConventionBasedOperationAuthorizerTests
    {
        private readonly IServiceProvider serviceProvider;
        private readonly TestTraceListener testTraceListener = new TestTraceListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedOperationAuthorizerTests"/> class.
        /// </summary>
        public ConventionBasedOperationAuthorizerTests()
        {
            serviceProvider = new ServiceProviderMock().ServiceProvider.Object;
            Trace.Listeners.Add(testTraceListener);
        }

        /// <summary>
        /// Checks whether the <see cref="ConventionBasedOperationAuthorizer"/> can be constructed.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ConventionBasedOperationAuthorizer(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that the constructor cannot be called with a null type.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedOperationAuthorizer(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Check that AuthorizeAsync can be called and returns true by default.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallAuthorizeAsync()
        {
            var context = new OperationContext(
                new EmptyApi(serviceProvider),
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedOperationAuthorizer(typeof(EmptyApi));
            var result = await testClass.AuthorizeAsync(context, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should be true by default.");
        }

        /// <summary>
        /// Check that AuthorizeAsync invokes the CanInsertObject method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AuthorizeAsyncInvokesConventionMethod()
        {
            var api = new NoPermissionApi(serviceProvider);
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedOperationAuthorizer(typeof(NoPermissionApi));
            var result = await testClass.AuthorizeAsync(context, cancellationToken);
            result.Should().BeFalse("AuthorizeAsync should invoke CanInsertObject.");
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of an incorrect visibility.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AuthorizeAsyncWithPrivateMethod()
        {
            testTraceListener.Clear();
            var api = new PrivateMethodApi(serviceProvider);
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedOperationAuthorizer(typeof(PrivateMethodApi));
            var result = await testClass.AuthorizeAsync(context, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because CanInsertObject is private.");
            testTraceListener.Messages.Should().Contain("inaccessible due to its protection level");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of a wrong return type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AuthorizeAsyncWithWrongReturnType()
        {
            testTraceListener.Clear();
            var api = new WrongReturnTypeApi(serviceProvider);
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedOperationAuthorizer(typeof(WrongReturnTypeApi));
            var result = await testClass.AuthorizeAsync(context, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because CanInsertObject returns an int.");
            testTraceListener.Messages.Should().Contain("does not return a boolean value");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of a wrong api type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AuthorizeAsyncWithWrongApiType()
        {
            testTraceListener.Clear();
            var api = new WrongReturnTypeApi(serviceProvider);
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedOperationAuthorizer(typeof(NoPermissionApi));
            var result = await testClass.AuthorizeAsync(context, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because the api type is incorrect.");
            testTraceListener.Messages.Should().Contain("is of the incorrect type");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that AuthorizeAsync does not invoke CanInsertObject because of a wrong number of arguments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AuthorizeAsyncWithWrongNumberOfArguments()
        {
            testTraceListener.Clear();
            var api = new IncorrectArgumentsApi(serviceProvider);
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedOperationAuthorizer(typeof(IncorrectArgumentsApi));
            var result = await testClass.AuthorizeAsync(context, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should return true, because the api type is incorrect.");
            testTraceListener.Messages.Should().Contain("incorrect number of arguments");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Checks that AuthorizeAsync throws when the submit context is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallAuthorizeAsyncWithNullContext()
        {
            var testClass = new ConventionBasedOperationAuthorizer(typeof(EmptyApi));
            Func<Task> act = () => testClass.AuthorizeAsync(
                default(OperationContext),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }
        }

        private class PrivateMethodApi : ApiBase
        {
            public PrivateMethodApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            private bool CanExecuteTest()
            {
                InvocationCount++;
                return false;
            }
        }

        private class WrongReturnTypeApi : ApiBase
        {
            public WrongReturnTypeApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal int CanExecuteTest()
            {
                InvocationCount++;
                return 0;
            }
        }

        private class NoPermissionApi : ApiBase
        {
            public NoPermissionApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal bool CanExecuteTest()
            {
                InvocationCount++;
                return false;
            }
        }

        private class IncorrectArgumentsApi : ApiBase
        {
            public IncorrectArgumentsApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal bool CanExecuteTest(int arg)
            {
                InvocationCount++;
                return false;
            }
        }
    }
}