// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.OData;
using Microsoft.Restier.AspNetCore;
using Microsoft.Restier.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Filters;

/// <summary>
/// Unit tests for the <see cref="RestierExceptionFilterAttribute"/> class.
/// </summary>
[TestClass]
public class RestierExceptionFilterAttributeTests
{
    private readonly RestierExceptionFilterAttribute _filter;

    public RestierExceptionFilterAttributeTests()
    {
        _filter = new RestierExceptionFilterAttribute();
    }

    [TestMethod]
    public async Task OnExceptionAsync_Should_Handle_ChangeSetValidationException()
    {
        // Arrange
        var context = CreateExceptionContext(new ChangeSetValidationException("Validation failed"));
        var cancellationToken = CancellationToken.None;

        // Act
        await _filter.OnExceptionAsync(context);

        // Assert
        context.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [TestMethod]
    public async Task OnExceptionAsync_Should_Handle_CommonException()
    {
        // Arrange
        var context = CreateExceptionContext(new ODataException("OData error"));
        var cancellationToken = CancellationToken.None;

        // Act
        await _filter.OnExceptionAsync(context);

        // Assert
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task HandleChangeSetValidationException_Should_Return_True_For_ChangeSetValidationException()
    {
        // Arrange
        var context = CreateExceptionContext(new ChangeSetValidationException("Validation failed"));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await InvokePrivateMethod<Task<bool>>(
            "HandleChangeSetValidationException",
            new object[] { context, cancellationToken });

        // Assert
        result.Should().BeTrue();
        context.Result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [TestMethod]
    public async Task HandleCommonException_Should_Return_True_For_ODataException()
    {
        // Arrange
        var context = CreateExceptionContext(new ODataException("OData error"));
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await InvokePrivateMethod<Task<bool>>(
            "HandleCommonException",
            new object[] { context, cancellationToken });

        // Assert
        result.Should().BeTrue();
        var objectResult = context.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task HandleCommonException_Should_Return_False_For_Null_Exception()
    {
        // Arrange
        var context = CreateExceptionContext(null);
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await InvokePrivateMethod<Task<bool>>(
            "HandleCommonException",
            new object[] { context, cancellationToken });

        // Assert
        result.Should().BeFalse();
        context.Result.Should().BeNull();
    }

    private ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = Substitute.For<HttpContext>();
        var routeData = new RouteData();

        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor(), new ModelStateDictionary());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    private T InvokePrivateMethod<T>(string methodName, object[] parameters)
    {
        var method = typeof(RestierExceptionFilterAttribute).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        return (T)method.Invoke(null, parameters);
    }
}
