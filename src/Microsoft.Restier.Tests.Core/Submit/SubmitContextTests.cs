// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Submit
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.Restier.Tests.Shared;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    /// <summary>
    /// Unit tests for the <see cref="SubmitContext"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SubmitContextTests
    {
        private readonly ServiceProviderMock serviceProviderFixture;
        private SubmitContext testClass;
        private ApiBase api;
        private ChangeSet changeSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitContextTests"/> class.
        /// </summary>
        public SubmitContextTests()
        {
            serviceProviderFixture = new ServiceProviderMock();
            api = new TestApi(serviceProviderFixture.ServiceProvider.Object);
            changeSet = new ChangeSet();
            testClass = new SubmitContext(api, changeSet);
        }

        /// <summary>
        /// Can construct an instance fo the <see cref="SubmitContext"/> class.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new SubmitContext(api, changeSet);
            instance.Should().NotBeNull();
        }

        /// <summary>
        /// Cannot constructo with a null Api.
        /// </summary>
        [TestMethod]
        public void CannotConstructWithNullApi()
        {
            Action act = () => new SubmitContext(default(ApiBase), new ChangeSet());
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Changeset is initialized correctly.
        /// </summary>
        [TestMethod]
        public void ChangeSetIsInitializedCorrectly()
        {
            testClass.ChangeSet.Should().Be(changeSet);
        }

        /// <summary>
        /// Can set and get the ChangeSet.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetChangeSet()
        {
            var testValue = new ChangeSet();
            testClass.ChangeSet = testValue;
            testClass.ChangeSet.Should().Be(testValue);
        }

        /// <summary>
        /// Can set and get the ChangeSet.
        /// </summary>
        [TestMethod]
        public void CannotSetAndGetChangeSetWithResult()
        {
            var testValue = new ChangeSet();
            testClass.ChangeSet = testValue;
            testClass.Result = new SubmitResult(testClass.ChangeSet);
            Action act = () => testClass.ChangeSet = new ChangeSet();
            act.Should().Throw<InvalidOperationException>();
        }

        /// <summary>
        /// Can set and get result.
        /// </summary>
        [TestMethod]
        public void CanSetAndGetResult()
        {
            var testValue = new SubmitResult(new Exception());
            testClass.Result = testValue;
            testClass.Result.Should().Be(testValue);
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