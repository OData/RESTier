// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Batch;

/// <summary>
/// Unit tests for the <see cref="RestierChangeSetProperty"/> class.
/// </summary>
[TestClass]
public class RestierChangeSetPropertyTests
{
    private readonly IQueryHandler queryHandler;
    private readonly IEdmModel model;
    private readonly ISubmitHandler submitHandler;
    private readonly ApiBase apiBase;

    public RestierChangeSetPropertyTests()
    {
        queryHandler = Substitute.For<IQueryHandler>();
        model = Substitute.For<IEdmModel>();
        submitHandler = Substitute.For<ISubmitHandler>();
        // Mock ApiBase
        apiBase = Substitute.For<EmptyApi>(model, queryHandler, submitHandler);
    }

    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var changeSetRequestItem = new RestierBatchChangeSetRequestItem(
            apiBase,
            new[] { Substitute.For<HttpContext>() }
        );

        // Act
        var changeSetProperty = new RestierChangeSetProperty(changeSetRequestItem);

        // Assert
        changeSetProperty.Exceptions.Should().NotBeNull();
        changeSetProperty.Exceptions.Should().BeEmpty();
        changeSetProperty.ChangeSet.Should().BeNull();
    }

    [TestMethod]
    public async Task OnChangeSetCompleted_ShouldCompleteSuccessfully_WhenNoExceptions()
    {
        // Arrange
        var changeSetRequestItem = new RestierBatchChangeSetRequestItem(
            apiBase,
            new[] { Substitute.For<HttpContext>() }
        );
        var changeSetProperty = new RestierChangeSetProperty(changeSetRequestItem)
        {
            ChangeSet = new ChangeSet()
        };
        submitHandler.SubmitAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SubmitResult(changeSetProperty.ChangeSet)));

        // Act
        var task = changeSetProperty.OnChangeSetCompleted();

        // Assert
        await task;
        await submitHandler.Received(1).SubmitAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task OnChangeSetCompleted_ShouldHandleExceptionsFromSubmitChangeSet()
    {
        // Arrange
        var changeSetRequestItem = new RestierBatchChangeSetRequestItem(
                  apiBase,
                  new[] { Substitute.For<HttpContext>() }
              );
        submitHandler.SubmitAsync(Arg.Any<SubmitContext>(), Arg.Any<CancellationToken>()).Throws((new InvalidOperationException("Test exception")));

        var changeSetProperty = new RestierChangeSetProperty(changeSetRequestItem)
        {
            ChangeSet = new ChangeSet()
        };

        // Act & Assert
        var exception = (await FluentActions.Awaiting(() => changeSetProperty.OnChangeSetCompleted()).Should().ThrowAsync<InvalidOperationException>()).Which;
        exception.Message.Should().Be("Test exception");
    }

    public class EmptyApi : ApiBase
    {
        public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
        {
        }
    }
}
