// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.Restier.Tests.Shared.Scenarios.Annotated;

public class AnnotatedContext : DbContext
{
    public AnnotatedContext(DbContextOptions<AnnotatedContext> options) : base(options)
    {
    }

    public DbSet<AnnotatedEntity> AnnotatedEntities { get; set; }
}
