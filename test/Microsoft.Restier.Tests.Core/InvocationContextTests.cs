// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core
{
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Unit tests for the <see cref="InvocationContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class InvocationContextTests
    {
        private readonly InvocationContext testClass;
        private readonly ApiBase api;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvocationContextTests"/> class.
        /// </summary>
        public InvocationContextTests()
        {
            api = new TestApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>());
            testClass = new InvocationContext(api);
        }

        /// <summary>
        /// Can construct an InvocationContext.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new InvocationContext(api);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot construct an InvocationContext with a null api.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new InvocationContext(default(ApiBase));
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Api is initialized correctly.
        /// </summary>
        [TestMethod]
        public void ApiIsInitializedCorrectly()
        {
            testClass.Api.Should().Be(api);
        }

        private class TestApi : ApiBase
        {
            public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}