// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Core.Submit
{

    /// <summary>
    /// Unit tests for the <see cref="DefaultChangeSetInitializer"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class DefaultChangeSetInitializerTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;
        private DefaultChangeSetInitializer testClass;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultChangeSetInitializerTests"/> class.
        /// </summary>
        public DefaultChangeSetInitializerTests()
        {
            testClass = new DefaultChangeSetInitializer();
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
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
            var serviceProvider = Substitute.For<IServiceProvider>();
            var context = new SubmitContext(new TestApi(model, queryHandler, submitHandler), null);
            var cancellationToken = CancellationToken.None;

            await testClass.InitializeAsync(context, cancellationToken);

            context.ChangeSet.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot call InitializeAsync with a null context.
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
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}