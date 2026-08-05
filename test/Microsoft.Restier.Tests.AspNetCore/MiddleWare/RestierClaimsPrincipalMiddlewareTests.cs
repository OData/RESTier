// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Restier.AspNetCore.Middleware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.AspNetCore.Middleware;

/// <summary>
/// Unit tests for <see cref="RestierClaimsPrincipalMiddleware"/>.
/// </summary>
[TestClass]
public class RestierClaimsPrincipalMiddlewareTests
{
    [TestMethod]
    public async Task InvokeAsync_ShouldSetHttpContextInContextAccessor()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        contextAccessor.HttpContext = null;
        var nextMiddleware = Substitute.For<RequestDelegate>();
        var middleware = new RestierClaimsPrincipalMiddleware(nextMiddleware);

        // Act
        await middleware.InvokeAsync(httpContext, contextAccessor);

        // Assert
        contextAccessor.HttpContext.Should().Be(httpContext);
    }

    [TestMethod]
    public async Task InvokeAsync_ShouldSetClaimsPrincipalSelector()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        contextAccessor.HttpContext = null;
        var nextMiddleware = Substitute.For<RequestDelegate>();
        var middleware = new RestierClaimsPrincipalMiddleware(nextMiddleware);

        // Act
        await middleware.InvokeAsync(httpContext, contextAccessor);

        // Assert
        ClaimsPrincipal.ClaimsPrincipalSelector.Should().NotBeNull();
        ClaimsPrincipal.ClaimsPrincipalSelector().Should().Be(httpContext.User);
    }

    [TestMethod]
    public async Task InvokeAsync_ShouldCallNextMiddleware()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var nextMiddleware = Substitute.For<RequestDelegate>();
        var middleware = new RestierClaimsPrincipalMiddleware(nextMiddleware);

        // Act
        await middleware.InvokeAsync(httpContext, contextAccessor);

        // Assert
        await nextMiddleware.Received(1).Invoke(httpContext);
    }
}
