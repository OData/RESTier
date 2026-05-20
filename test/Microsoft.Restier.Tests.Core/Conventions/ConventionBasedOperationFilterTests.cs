// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using NSubstitute;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedOperationFilter"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ConventionBasedOperationFilterTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private readonly TestTraceListener testTraceListener = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedOperationFilterTests"/> class.
        /// </summary>
        public ConventionBasedOperationFilterTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
            Trace.Listeners.Add(testTraceListener);
        }

        /// <summary>
        /// Checks whether the <see cref="ConventionBasedOperationFilter"/> can be constructed.
        /// </summary>
        [Fact]
        public void CanConstruct()
        {
            var instance = new ConventionBasedOperationFilter(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that the constructor cannot be called with a null type.
        /// </summary>
        [Fact]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedOperationFilter(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync can be called.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task CanCallOnOperationExecutingAsync()
        {
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi));
            var context = new OperationContext(
                new EmptyApi(model, queryHandler, submitHandler),
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutingAsyncInvokesConventionMethod()
        {
            var api = new ExecuteApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutingAsyncInvokesAsyncConventionMethod()
        {
            var api = new ExecuteApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
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
        [Fact]
        public async Task CanCallOnOperationExecutedAsync()
        {
            var api = new EmptyApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutedAsyncInvokesConventionMethod()
        {
            var api = new ExecuteApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutedAsyncInvokesAsyncConventionMethod()
        {
            var api = new ExecuteAsyncApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteAsyncApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutingAsyncWithPrivateMethod()
        {
            var api = new PrivateMethodApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(PrivateMethodApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutingWithWrongReturnType()
        {
            var api = new WrongReturnTypeApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(WrongReturnTypeApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutingWithWrongApiType()
        {
            var api = new PrivateMethodApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(ExecuteApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task OnOperationExecutingWithWrongNumberOfArguments()
        {
            var api = new IncorrectArgumentsApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedOperationFilter(typeof(IncorrectArgumentsApi));
            var context = new OperationContext(
                api,
                s => (true, new object()),
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
        [Fact]
        public async Task CannotCallOnOperationExecutedAsyncWithNullContext()
        {
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi));
            Func<Task> act = () => testClass.OnOperationExecutedAsync(
                default(OperationContext),
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Check that OnOperationExecutingAsync invokes the Inner IOperationFilter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnOperationExecutingAsyncInvokesInnerFilter()
        {
            // Arrange
            var innerFilter = Substitute.For<IOperationFilter>();
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi))
            {
                Inner = innerFilter
            };
            var context = new OperationContext(
                new EmptyApi(model, queryHandler, submitHandler),
                s => (true, new object()),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;

            // Act
            await testClass.OnOperationExecutingAsync(context, cancellationToken);

            // Assert
            await innerFilter.Received(1).OnOperationExecutingAsync(context, cancellationToken);
        }

        /// <summary>
        /// Check that OnOperationExecutedAsync invokes the Inner IOperationFilter.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [Fact]
        public async Task OnOperationExecutedAsyncInvokesInnerFilter()
        {
            // Arrange
            var innerFilter = Substitute.For<IOperationFilter>();
            var testClass = new ConventionBasedOperationFilter(typeof(EmptyApi))
            {
                Inner = innerFilter
            };
            var context = new OperationContext(
                new EmptyApi(model, queryHandler, submitHandler),
                s => (true, new object()),
                "Test",
                true,
                null);
            var cancellationToken = CancellationToken.None;

            // Act
            await testClass.OnOperationExecutedAsync(context, cancellationToken);

            // Assert
            await innerFilter.Received(1).OnOperationExecutedAsync(context, cancellationToken);
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class ExecuteApi : ApiBase
        {
            public ExecuteApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
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
            public ExecuteAsyncApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
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
            public PrivateMethodApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
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
            public WrongReturnTypeApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
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
            public WrongMethodApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
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
            public IncorrectArgumentsApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
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