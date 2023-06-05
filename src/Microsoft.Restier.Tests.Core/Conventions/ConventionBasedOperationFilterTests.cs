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
    /// Unit tests for the <see cref="ConventionBasedOperationFilter"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ConventionBasedOperationFilterTests
    {
        private readonly IServiceProvider serviceProvider;
        private readonly TestTraceListener testTraceListener = new TestTraceListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedOperationFilterTests"/> class.
        /// </summary>
        public ConventionBasedOperationFilterTests()
        {
            serviceProvider = new ServiceProviderMock().ServiceProvider.Object;
            Trace.Listeners.Add(testTraceListener);
        }

        /// <summary>
        /// Checks whether the <see cref="ConventionBasedOperationFilter"/> can be constructed.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ConventionBasedOperationFilter(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that the constructor cannot be called with a null type.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedOperationFilter(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync can be called.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallOnOperationExecutingAsync()
        {
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi));
            var context = new OperationContext(
                new EmptyApi(serviceProvider),
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutingAsync(context, cancellationToken);
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync invokes the OnExecutingTest method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutingAsyncInvokesConventionMethod()
        {
            var api = new ExecuteApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutingAsync(context, cancellationToken);
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync invokes the OnExecutingTest method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutingAsyncInvokesAsyncConventionMethod()
        {
            var api = new ExecuteApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutingAsync(context, cancellationToken);
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Checks that OnOperationExecutingAsync throws when the submit context is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallOnOperationExecutingAsyncWithNullContext()
        {
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi));
            Func<Task> act = () => testClass.OnOperationExecutingAsync(
                default(OperationContext),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Check that OnOperationExecutedAsync can be called.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallOnOperationExecutedAsync()
        {
            var api = new EmptyApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutedAsync(context, cancellationToken);
        }

        /// <summary>
        /// Check that OnOperationExecutedAsync invokes the OnExecutedTest method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutedAsyncInvokesConventionMethod()
        {
            var api = new ExecuteApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutedAsync(context, cancellationToken);
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Check that OnOperationExecutedAsync invokes the OnExecutedTestAsync method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutedAsyncInvokesAsyncConventionMethod()
        {
            var api = new ExecuteAsyncApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteAsyncApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutedAsync(context, cancellationToken);
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync does not invoke OnExecutingTest because of an incorrect visibility.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutingAsyncWithPrivateMethod()
        {
            var api = new PrivateMethodApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(PrivateMethodApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutingAsync(context, cancellationToken);
            testTraceListener.Messages.Should().Contain("inaccessible due to its protection level");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync does not invoke OnExecutingTest because of a wrong return type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutingWithWrongReturnType()
        {
            var api = new WrongReturnTypeApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(WrongReturnTypeApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutingAsync(context, cancellationToken);
            testTraceListener.Messages.Should().Contain("does not return");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync does not invoke OnExecutingTest because of a wrong api type.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutingWithWrongApiType()
        {
            var api = new PrivateMethodApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutingAsync(context, cancellationToken);
            testTraceListener.Messages.Should().Contain("is of the incorrect type");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync does not invoke OnExecutingTest because of a wrong number of arguments.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task OnOperationExecutingWithWrongNumberOfArguments()
        {
            var api = new IncorrectArgumentsApi(serviceProvider);
            var testClass = new ConventionBasedOperationFilter(typeof(IncorrectArgumentsApi));
            var context = new OperationContext(
                api,
                s => new object(),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;
            await testClass.OnOperationExecutingAsync(context, cancellationToken);
            testTraceListener.Messages.Should().Contain("incorrect number of arguments");
            api.InvocationCount.Should().Be(0);
        }

        /// <summary>
        /// Checks that OnOperationExecutedAsync throws when the submit context is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallOnOperationExecutedAsyncWithNullContext()
        {
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi));
            Func<Task> act = () => testClass.OnOperationExecutedAsync(
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

        private class ExecuteApi : ApiBase
        {
            public ExecuteApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected void OnExecutingTest()
            {
                InvocationCount++;
            }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            protected async Task OnExecutedTest()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                InvocationCount++;
            }
        }

        private class ExecuteAsyncApi : ApiBase
        {
            public ExecuteAsyncApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected async Task OnExecutingTestAsync()
            {
                InvocationCount++;
                await Task.CompletedTask;
            }

            protected async Task OnExecutedTestAsync()
            {
                InvocationCount++;
                await Task.CompletedTask;
            }
        }

        private class PrivateMethodApi : ApiBase
        {
            public PrivateMethodApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            private void OnExecutingTest(object o)
            {
                InvocationCount++;
            }
        }

        private class WrongReturnTypeApi : ApiBase
        {
            public WrongReturnTypeApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal int OnExecutingTest()
            {
                InvocationCount++;
                return 0;
            }
        }

        private class WrongMethodApi : ApiBase
        {
            public WrongMethodApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal void OnExecutingTest()
            {
                InvocationCount++;
            }
        }

        private class IncorrectArgumentsApi : ApiBase
        {
            public IncorrectArgumentsApi(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal void OnExecutingTest(int arg)
            {
                InvocationCount++;
            }
        }
    }
}