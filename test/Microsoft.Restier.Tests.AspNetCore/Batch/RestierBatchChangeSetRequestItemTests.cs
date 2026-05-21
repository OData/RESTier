// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Batch;

/// <summary>
/// Unit tests for the <see cref="RestierBatchChangeSetRequestItem"/> class."/>
/// </summary>
[TestClass]
public class RestierBatchChangeSetRequestItemTests
{
    public TestContext TestContext { get; set; }

    private readonly IQueryHandler queryHandler;
    private readonly IEdmModel model;
    private readonly ISubmitHandler submitHandler;
    private readonly ApiBase apiBase;
    private readonly IEnumerable<HttpContext> httpContexts;
    private readonly RestierBatchChangeSetRequestItem testItem;

    public RestierBatchChangeSetRequestItemTests()
    {
        queryHandler = Substitute.For<IQueryHandler>();
        model = Substitute.For<IEdmModel>();
        submitHandler = Substitute.For<ISubmitHandler>();
        // Mock ApiBase
        apiBase = Substitute.For<EmptyApi>(model, queryHandler, submitHandler);

        // Mock HttpContext
        var httpContextMock = Substitute.For<HttpContext>();
        httpContexts = new List<HttpContext> { httpContextMock };

        // Create test instance
        testItem = new RestierBatchChangeSetRequestItem(apiBase, httpContexts);
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenApiIsNull()
    {
        // Act
        Action act = () => new RestierBatchChangeSetRequestItem(null, httpContexts);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*api*");
    }

    [TestMethod]
    public void Constructor_ShouldThrowArgumentNullException_WhenContextsIsNull()
    {
        // Act
        Action act = () => new RestierBatchChangeSetRequestItem(apiBase, null);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*contexts*");
    }

    [TestMethod]
    public async Task SendRequestAsync_ShouldThrowArgumentNullException_WhenHandlerIsNull()
    {
        // Act
        Func<Task> act = async () => await testItem.SendRequestAsync(null);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*handler*");
    }

    [TestMethod]
    public async Task SendRequestAsync_ShouldReturnChangeSetResponseItem_WhenRequestFails()
    {
        // Arrange
        var handler = Substitute.For<RequestDelegate>();
        var httpContextMock = httpContexts.First();
        httpContextMock.Response.StatusCode = StatusCodes.Status500InternalServerError;

        // Act
        var result = await testItem.SendRequestAsync(handler);

        // Assert
        result.Should().BeOfType<ChangeSetResponseItem>();
        var responseItem = (ChangeSetResponseItem)result;
        responseItem.Contexts.Should().ContainSingle();
        responseItem.Contexts.First().Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [TestMethod]
    public async Task SendRequestAsync_ShouldReturnChangeSetResponseItem_WhenAllRequestsSucceed()
    {
        // Arrange
        var handler = Substitute.For<RequestDelegate>();
        var httpContextMock = httpContexts.First();
        httpContextMock.Response.StatusCode = StatusCodes.Status200OK;

        // Act
        var result = await testItem.SendRequestAsync(handler);

        // Assert
        result.Should().BeOfType<ChangeSetResponseItem>();
        var responseItem = (ChangeSetResponseItem)result;
        responseItem.Contexts.Should().ContainSingle();
        responseItem.Contexts.First().Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [TestMethod]
    public async Task SubmitChangeSet_ShouldCallApiSubmitAsync()
    {
        // Arrange
        var changeSet = new ChangeSet();

        // Act
        await testItem.SubmitChangeSet(changeSet);

        // Assert
        await apiBase.Received(1).SubmitAsync(changeSet, TestContext.CancellationTokenSource.Token);
    }

    [TestMethod]
    public void SetChangeSetProperty_ShouldSetChangeSetPropertyOnAllContexts()
    {
        // Arrange
        var changeSetProperty = new RestierChangeSetProperty(testItem);

        // Act
        var setChangeSetPropertyMethod = typeof(RestierBatchChangeSetRequestItem)
            .GetMethod("SetChangeSetProperty", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        setChangeSetPropertyMethod.Invoke(testItem, new object[] { changeSetProperty });

        // Assert
        foreach (var context in httpContexts)
        {
            context.Received(1).SetChangeSet(changeSetProperty);
        }
    }

    public class EmptyApi : ApiBase
    {
        public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
        {
        }
    }
}
