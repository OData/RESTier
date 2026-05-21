// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.MultiTenancy;

[TestClass]
public class PathSegmentTenantResolutionMiddlewareTests
{
    private static (HttpContext ctx, ITenantContext tenant, IDisposable cleanup) BuildContext(string path)
    {
        var services = new ServiceCollection();
        services.AddScoped<ITenantContext, TenantContext>();
        var sp = services.BuildServiceProvider();
        var scope = sp.CreateScope();

        var ctx = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
        };
        ctx.Request.Path = path;
        ctx.Response.Body = new MemoryStream();

        var tenant = ctx.RequestServices.GetRequiredService<ITenantContext>();
        return (ctx, tenant, new ScopeAndProvider(scope, sp));
    }

    private sealed class ScopeAndProvider : IDisposable
    {
        private readonly IServiceScope scope;
        private readonly ServiceProvider provider;

        public ScopeAndProvider(IServiceScope scope, ServiceProvider provider)
        {
            this.scope = scope;
            this.provider = provider;
        }

        public void Dispose()
        {
            scope.Dispose();
            provider.Dispose();
        }
    }

    private static IConnectionStringProvider MakeProvider()
    {
        return new InMemoryTenantConnectionStringProvider(new Dictionary<string, string>
        {
            ["acme"] = "tenant-acme-db",
            ["globex"] = "tenant-globex-db",
        });
    }

    [TestMethod]
    public async Task KnownTenant_StripsSegmentAndPopulatesContext()
    {
        var (ctx, tenant, cleanup) = BuildContext("/acme/odata/Books");
        using var _ = cleanup;
        var provider = MakeProvider();
        var nextCalled = false;
        RequestDelegate next = c => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new PathSegmentTenantResolutionMiddleware(next, provider);

        await middleware.InvokeAsync(ctx);

        nextCalled.Should().BeTrue();
        tenant.TenantId.Should().Be("acme");
        ctx.Request.PathBase.Value.Should().Be("/acme");
        ctx.Request.Path.Value.Should().Be("/odata/Books");
        ctx.Response.StatusCode.Should().Be(200, because: "default status when next pipeline ran without overriding");
    }

    [TestMethod]
    public async Task UnknownTenant_ShortCircuitsWith400()
    {
        var (ctx, tenant, cleanup) = BuildContext("/unknown/odata/Books");
        using var _ = cleanup;
        var provider = MakeProvider();
        var nextCalled = false;
        RequestDelegate next = c => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new PathSegmentTenantResolutionMiddleware(next, provider);

        await middleware.InvokeAsync(ctx);

        nextCalled.Should().BeFalse(because: "the middleware should short-circuit on an unknown tenant");
        ctx.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        tenant.TenantId.Should().BeNull(because: "TenantId is only populated after a successful lookup");
        ctx.Request.PathBase.Value.Should().BeEmpty();
        ctx.Request.Path.Value.Should().Be("/unknown/odata/Books", because: "the path should not be rewritten on the failure path");
    }

    [TestMethod]
    public async Task EmptyPath_ShortCircuitsWith400()
    {
        var (ctx, _, cleanup) = BuildContext("/");
        using var _ = cleanup;
        var provider = MakeProvider();
        var nextCalled = false;
        RequestDelegate next = c => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new PathSegmentTenantResolutionMiddleware(next, provider);

        await middleware.InvokeAsync(ctx);

        nextCalled.Should().BeFalse();
        ctx.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task TenantOnlyPath_StillRewritesPathBase()
    {
        // Tenant-only request like /acme/ — the rewritten path is just "/", which RESTier
        // would treat as the service document. The middleware should still strip the
        // tenant and populate context.
        var (ctx, tenant, cleanup) = BuildContext("/acme");
        using var _ = cleanup;
        var provider = MakeProvider();
        var nextCalled = false;
        RequestDelegate next = c => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new PathSegmentTenantResolutionMiddleware(next, provider);

        await middleware.InvokeAsync(ctx);

        nextCalled.Should().BeTrue();
        tenant.TenantId.Should().Be("acme");
        ctx.Request.PathBase.Value.Should().Be("/acme");
        ctx.Request.Path.Value.Should().Be(string.Empty);
    }
}
