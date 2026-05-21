// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Microsoft.Restier.Tests.AspNetCore.Extensions
{
    /// <summary>
    /// Unit tests for the <see cref="RestierHttpContextExtensions"/> class.
    /// </summary>
    [TestClass]
    public class RestierHttpContextExtensionsTests
    {
        private readonly RestierBatchChangeSetRequestItem restierBatchRequestItem;

        public RestierHttpContextExtensionsTests()
        {
            restierBatchRequestItem = new RestierBatchChangeSetRequestItem(
                new EmptyApi(Substitute.For<IEdmModel>(), Substitute.For<IQueryHandler>(), Substitute.For<ISubmitHandler>()),
                new[] { Substitute.For<HttpContext>() }
            );
        }

        [TestMethod]
        public void SetChangeSet_ShouldAddChangeSetToHttpContextItems()
        {
            // Arrange
            var context = Substitute.For<HttpContext>();
            var items = new System.Collections.Generic.Dictionary<object, object>();
            context.Items.Returns(items);

            var changeSetProperty = new RestierChangeSetProperty(restierBatchRequestItem);

            // Act
            context.SetChangeSet(changeSetProperty);

            // Assert
            items.ContainsKey("Microsoft.Restier.Submit.ChangeSet").Should().BeTrue();
            items["Microsoft.Restier.Submit.ChangeSet"].Should().Be(changeSetProperty);
        }

        [TestMethod]
        public void GetChangeSet_ShouldReturnChangeSetFromHttpContextItems()
        {
            // Arrange
            var context = Substitute.For<HttpContext>();
            var items = new System.Collections.Generic.Dictionary<object, object>();
            var changeSetProperty = new RestierChangeSetProperty(restierBatchRequestItem);
            items["Microsoft.Restier.Submit.ChangeSet"] = changeSetProperty;
            context.Items.Returns(items);

            // Act
            var result = context.GetChangeSet();

            // Assert
            result.Should().Be(changeSetProperty);
        }

        [TestMethod]
        public void GetChangeSet_ShouldReturnNullIfChangeSetNotPresent()
        {
            // Arrange
            var context = Substitute.For<HttpContext>();
            var items = new System.Collections.Generic.Dictionary<object, object>();
            context.Items.Returns(items);

            // Act
            var result = context.GetChangeSet();

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void SetChangeSet_ShouldThrowArgumentNullException_WhenContextIsNull()
        {
            // Arrange
            HttpContext context = null;
            var changeSetProperty = new RestierChangeSetProperty(restierBatchRequestItem);

            // Act & Assert
            FluentActions.Invoking(() => context.SetChangeSet(changeSetProperty)).Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void GetChangeSet_ShouldThrowArgumentNullException_WhenContextIsNull()
        {
            // Arrange
            HttpContext context = null;

            // Act & Assert
            FluentActions.Invoking(() => context.GetChangeSet()).Should().Throw<ArgumentNullException>();
        }

        public class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}
