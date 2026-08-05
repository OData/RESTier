// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests.EFCore.MultiTenancy;

public sealed class InMemoryTenantConnectionStringProvider : IConnectionStringProvider
{
    private readonly Dictionary<string, string> map;

    public InMemoryTenantConnectionStringProvider(IDictionary<string, string> map)
    {
        this.map = new Dictionary<string, string>(map, StringComparer.OrdinalIgnoreCase);
    }

    public string GetConnectionString(string tenantId)
    {
        return TryGetConnectionString(tenantId, out var name)
            ? name
            : throw new InvalidOperationException($"Unknown tenant '{tenantId}'.");
    }

    public bool TryGetConnectionString(string tenantId, out string connectionString)
    {
        return map.TryGetValue(tenantId ?? string.Empty, out connectionString);
    }
}
