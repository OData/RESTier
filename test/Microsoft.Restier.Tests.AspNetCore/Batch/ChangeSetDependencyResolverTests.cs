// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Batch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Restier.Tests.AspNetCore.Batch;

/// <summary>
/// Unit tests for the <see cref="ChangeSetDependencyResolver"/> class.
/// </summary>
[TestClass]
public class ChangeSetDependencyResolverTests
{
    #region DetectDependencies Tests

    [TestMethod]
    public void DetectDependencies_NoDependencies_ReturnsEmpty()
    {
        // Arrange
        var contentIdToUrl = new Dictionary<string, string>
        {
            { "1", "http://localhost/api/Books" },
            { "2", "http://localhost/api/Categories" },
        };

        // Act
        var result = ChangeSetDependencyResolver.DetectDependencies(contentIdToUrl);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void DetectDependencies_DirectReference_ReturnsDependency()
    {
        // Arrange
        var contentIdToUrl = new Dictionary<string, string>
        {
            { "1", "http://localhost/api/Books" },
            { "2", "$1/Details" },
        };

        // Act
        var result = ChangeSetDependencyResolver.DetectDependencies(contentIdToUrl);

        // Assert
        result.Should().ContainKey("2");
        result["2"].Should().ContainSingle().Which.Should().Be("1");
    }

    [TestMethod]
    public void DetectDependencies_MultipleReferences_ReturnsAll()
    {
        // Arrange
        var contentIdToUrl = new Dictionary<string, string>
        {
            { "1", "http://localhost/api/Books" },
            { "2", "http://localhost/api/Authors" },
            { "3", "$1/Authors/$2" },
        };

        // Act
        var result = ChangeSetDependencyResolver.DetectDependencies(contentIdToUrl);

        // Assert
        result.Should().ContainKey("3");
        result["3"].Should().HaveCount(2);
        result["3"].Should().Contain("1");
        result["3"].Should().Contain("2");
    }

    [TestMethod]
    public void DetectDependencies_DollarSignNotContentId_ReturnsEmpty()
    {
        // Arrange
        var contentIdToUrl = new Dictionary<string, string>
        {
            { "1", "http://localhost/api/Books?$filter=Price gt 10&$top=5" },
        };

        // Act
        var result = ChangeSetDependencyResolver.DetectDependencies(contentIdToUrl);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ComputeExpectedEntityUrl Tests

    [TestMethod]
    public void ComputeExpectedEntityUrl_PatchRequest_ReturnsRequestUrl()
    {
        // Arrange
        var context = CreateMockHttpContext("PATCH", "http://localhost/api/Books(1)");
        var model = CreateEdmModel();

        // Act
        var result = ChangeSetDependencyResolver.ComputeExpectedEntityUrl(context, model);

        // Assert
        result.Should().Be("http://localhost/api/Books(1)");
    }

    [TestMethod]
    public void ComputeExpectedEntityUrl_DeleteRequest_ReturnsRequestUrl()
    {
        // Arrange
        var context = CreateMockHttpContext("DELETE", "http://localhost/api/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)");
        var model = CreateEdmModel();

        // Act
        var result = ChangeSetDependencyResolver.ComputeExpectedEntityUrl(context, model);

        // Assert
        result.Should().Be("http://localhost/api/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)");
    }

    [TestMethod]
    public void ComputeExpectedEntityUrl_PostWithGuidKey_ReturnsEntityUrl()
    {
        // Arrange
        var body = "{\"Id\":\"79874b37-ce46-4f4c-aa74-8e02ce4d8b67\",\"Title\":\"Test Book\"}";
        var context = CreateMockHttpContext("POST", "http://localhost/api/Books", body);
        var model = CreateEdmModel();

        // Act
        var result = ChangeSetDependencyResolver.ComputeExpectedEntityUrl(context, model);

        // Assert
        result.Should().Be("http://localhost/api/Books(79874b37-ce46-4f4c-aa74-8e02ce4d8b67)");
    }

    [TestMethod]
    public void ComputeExpectedEntityUrl_PostWithIntKey_ReturnsEntityUrl()
    {
        // Arrange
        var body = "{\"Id\":42,\"Name\":\"Test Category\"}";
        var context = CreateMockHttpContext("POST", "http://localhost/api/Categories", body);
        var model = CreateEdmModelWithIntKey();

        // Act
        var result = ChangeSetDependencyResolver.ComputeExpectedEntityUrl(context, model);

        // Assert
        result.Should().Be("http://localhost/api/Categories(42)");
    }

    [TestMethod]
    public void ComputeExpectedEntityUrl_PostWithoutKeyInBody_ReturnsNull()
    {
        // Arrange
        var body = "{\"Title\":\"Test Book\"}";
        var context = CreateMockHttpContext("POST", "http://localhost/api/Books", body);
        var model = CreateEdmModel();

        // Act
        var result = ChangeSetDependencyResolver.ComputeExpectedEntityUrl(context, model);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ResolveContentIdInUrl Tests

    [TestMethod]
    public void ResolveContentIdInUrl_ReplacesReference()
    {
        // Arrange
        var url = "$1/Details";
        var mapping = new Dictionary<string, string>
        {
            { "1", "http://localhost/api/Books(1)" },
        };

        // Act
        var result = ChangeSetDependencyResolver.ResolveContentIdInUrl(url, mapping);

        // Assert
        result.Should().Be("http://localhost/api/Books(1)/Details");
    }

    [TestMethod]
    public void ResolveContentIdInUrl_PreservesODataQueryOptions()
    {
        // Arrange
        var url = "http://localhost/api/Books?$filter=Price gt 10&$top=5";
        var mapping = new Dictionary<string, string>();

        // Act
        var result = ChangeSetDependencyResolver.ResolveContentIdInUrl(url, mapping);

        // Assert
        result.Should().Be("http://localhost/api/Books?$filter=Price gt 10&$top=5");
    }

    #endregion

    #region Test Helpers

    private static HttpContext CreateMockHttpContext(string method, string url, string body = null)
    {
        var context = new DefaultHttpContext();
        var uri = new Uri(url);

        context.Request.Method = method;
        context.Request.Scheme = uri.Scheme;
        context.Request.Host = uri.IsDefaultPort
            ? new HostString(uri.Host)
            : new HostString(uri.Host, uri.Port);
        context.Request.Path = uri.AbsolutePath;
        context.Request.QueryString = new QueryString(uri.Query);

        if (body is not null)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(body);
            writer.Flush();
            stream.Position = 0;
            context.Request.Body = stream;
        }

        return context;
    }

    private static IEdmModel CreateEdmModel()
    {
        var model = new EdmModel();
        var entityType = new EdmEntityType("Test", "Book");
        entityType.AddKeys(entityType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Guid));
        entityType.AddStructuralProperty("Title", EdmPrimitiveTypeKind.String);
        model.AddElement(entityType);

        var container = new EdmEntityContainer("Test", "Default");
        container.AddEntitySet("Books", entityType);
        model.AddElement(container);

        return model;
    }

    private static IEdmModel CreateEdmModelWithIntKey()
    {
        var model = new EdmModel();
        var entityType = new EdmEntityType("Test", "Category");
        entityType.AddKeys(entityType.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));
        entityType.AddStructuralProperty("Name", EdmPrimitiveTypeKind.String);
        model.AddElement(entityType);

        var container = new EdmEntityContainer("Test", "Default");
        container.AddEntitySet("Categories", entityType);
        model.AddElement(container);

        return model;
    }

    #endregion
}
