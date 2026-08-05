// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.MultiTenancy;

public class PathSegmentTenantResolutionMiddleware
{
    private readonly RequestDelegate next;
    private readonly IConnectionStringProvider connectionStrings;

    public PathSegmentTenantResolutionMiddleware(RequestDelegate next, IConnectionStringProvider connectionStrings)
    {
        this.next = next;
        this.connectionStrings = connectionStrings;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var trimmed = path.TrimStart('/');
        var slash = trimmed.IndexOf('/');
        var tenantId = slash < 0 ? trimmed : trimmed.Substring(0, slash);

        if (string.IsNullOrEmpty(tenantId))
        {
            await WriteBadRequestAsync(context, "Tenant segment is missing from the request path.");
            return;
        }

        if (!connectionStrings.TryGetConnectionString(tenantId, out _))
        {
            await WriteBadRequestAsync(context, $"Unknown tenant '{tenantId}'.");
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<ITenantContext>();
        tenantContext.TenantId = tenantId;

        var remainder = slash < 0 ? string.Empty : trimmed.Substring(slash);
        context.Request.PathBase = context.Request.PathBase.Add("/" + tenantId);
        context.Request.Path = remainder;

        await next(context);
    }

    private static async Task WriteBadRequestAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync(message);
    }
}
