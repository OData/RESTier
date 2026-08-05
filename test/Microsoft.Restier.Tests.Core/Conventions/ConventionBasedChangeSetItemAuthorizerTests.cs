// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.Restier.Tests.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Core
{
    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedChangeSetItemAuthorizer"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ConventionBasedChangeSetItemAuthorizerTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private readonly DataModificationItem dataModificationItem;
        private readonly TestTraceListener testTraceListener = new TestTraceListener();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedChangeSetItemAuthorizerTests"/> class.
        /// </summary>
        public ConventionBasedChangeSetItemAuthorizerTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
            dataModificationItem = new DataModificationItem(
                "Test",
                typeof(object),
                typeof(object),
                RestierEntitySetOperation.Insert,
                new Dictionary<string, object>(),
                new Dictionary<string, object>(),
                new Dictionary<string, object>());
            Trace.Listeners.Add(testTraceListener);
        }

        /// <summary>
        /// Checks whether the <see cref="ConventionBasedChangeSetItemAuthorizer"/> can be constructed.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Checks that the constructor cannot be called with a null type.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullTargetType()
        {
            Action act = () => new ConventionBasedChangeSetItemAuthorizer(default(Type));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Check that AuthorizeAsync can be called and returns true by default.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CanCallAuthorizeAsync()
        {
            var context = new SubmitContext(new EmptyApi(model, queryHandler, submitHandler), new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should be true by default.");
        }

        /// <summary>
        /// Check that AuthorizeAsync invokes the CanInsertObject method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AuthorizeAsyncInvokesConventionMethod()
        {
            var api = new NoPermissionApi(model, queryHandler, submitHandler);
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(NoPermissionApi));
            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);
            result.Should().BeFalse("AuthorizeAsync should invoke CanInsertObject.");
            api.InvocationCount.Should().Be(1);
        }

        /// <summary>
        /// Check that AuthorizeAsync invokes the CanInsertObject method according to convention.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task AuthorizeAsyncInvokesAsyncConventionMethod()
        {
            var api = new AsyncApi(model, queryHandler, submitHandler);
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(AsyncApi));
            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);
            result.Should().BeTrue("AuthorizeAsync should invoke CanInsertObject.");
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
            var api = new PrivateMethodApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(PrivateMethodApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);
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
            var api = new WrongReturnTypeApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(WrongReturnTypeApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);
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
            var api = new WrongReturnTypeApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(NoPermissionApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);
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
            var api = new IncorrectArgumentsApi(model, queryHandler, submitHandler);
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(IncorrectArgumentsApi));
            var context = new SubmitContext(api, new ChangeSet());
            var cancellationToken = CancellationToken.None;
            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);
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
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            Func<Task> act = () => testClass.AuthorizeAsync(
                default(SubmitContext),
                dataModificationItem,
                CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that AuthorizeAsync throws when the item. is null.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CannotCallAuthorizeAsyncWithNullItem()
        {
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            Func<Task> act = () => testClass.AuthorizeAsync(new SubmitContext(new EmptyApi(model, queryHandler, submitHandler), new ChangeSet()), default(ChangeSetItem), CancellationToken.None);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        /// <summary>
        /// Checks that the Inner property is null by default.
        /// </summary>
        [TestMethod]
        public void InnerPropertyIsNullByDefault()
        {
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            testClass.Inner.Should().BeNull("Inner should be null by default.");
        }

        /// <summary>
        /// Checks that the Inner property can be set and retrieved.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetInnerProperty()
        {
            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi));
            var mockInner = Substitute.For<IChangeSetItemAuthorizer>();
            testClass.Inner = mockInner;
            testClass.Inner.Should().BeSameAs(mockInner, "Inner should return the same instance that was set.");
        }

        /// <summary>
        /// Checks that the Inner property is invoked during AuthorizeAsync.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task InnerPropertyIsInvokedDuringAuthorizeAsync()
        {
            var mockInner = Substitute.For<IChangeSetItemAuthorizer>();
            mockInner.AuthorizeAsync(Arg.Any<SubmitContext>(), Arg.Any<ChangeSetItem>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(false));

            var testClass = new ConventionBasedChangeSetItemAuthorizer(typeof(EmptyApi))
            {
                Inner = mockInner
            };

            var context = new SubmitContext(new EmptyApi(model, queryHandler, submitHandler), new ChangeSet());
            var cancellationToken = CancellationToken.None;

            var result = await testClass.AuthorizeAsync(context, dataModificationItem, cancellationToken);

            result.Should().BeFalse("AuthorizeAsync should return false because Inner returned false.");
            await mockInner.Received(1).AuthorizeAsync(context, dataModificationItem, cancellationToken);
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }

        private class AsyncApi : ApiBase
        {
            public AsyncApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal async Task<bool> CanInsertObjectAsync()
            {
                InvocationCount++;
                return await Task.FromResult(true);
            }
        }

        private class PrivateMethodApi : ApiBase
        {
            public PrivateMethodApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }

            public int InvocationCount { get; private set; }

            private bool CanInsertObject()
            {
                InvocationCount++;
                return false;
            }
        }

        private class WrongReturnTypeApi : ApiBase
        {
            public WrongReturnTypeApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal int CanInsertObject()
            {
                InvocationCount++;
                return 0;
            }
        }

        private class NoPermissionApi : ApiBase
        {
            public NoPermissionApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal bool CanInsertObject()
            {
                InvocationCount++;
                return false;
            }
        }

        private class IncorrectArgumentsApi : ApiBase
        {
            public IncorrectArgumentsApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }

            public int InvocationCount { get; private set; }

            protected internal bool CanInsertObject(int arg)
            {
                InvocationCount++;
                return false;
            }
        }
    }
}