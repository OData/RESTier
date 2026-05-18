// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.FeatureTests;

/// <summary>
/// Plain entity type used as the row type for the anonymous-access fixture APIs.
/// </summary>
public class AnonPerson
{
    public int Id { get; set; }
    public string Name { get; set; }
}

/// <summary>
/// API where the entire class is anonymous-allowed. With a global [Authorize] filter, every
/// route this API serves should bypass authentication.
/// </summary>
[AllowAnonymous]
public class AnonymousAtClassApi : ApiBase
{
    public AnonymousAtClassApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();
}

/// <summary>
/// API that does NOT declare [AllowAnonymous]. Used as the control case: with a global
/// [Authorize] filter, every route should require authentication.
/// </summary>
public class RequireAuthApi : ApiBase
{
    public RequireAuthApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();
}

/// <summary>
/// API with one [AllowAnonymous] operation method, one operation method behind an
/// [Authorize(Policy="AdminOnly")] gate, and one operation method with no attribute (which
/// inherits the global [Authorize] filter at the controller level).
///
/// Operations are <see cref="OperationType.Action"/> rather than functions because functions
/// must return a value. Actions can be void, which sidesteps RESTier's serializer requirements
/// for return types that this test fixture doesn't otherwise wire up.
/// </summary>
public class AnonymousAtOperationApi : ApiBase
{
    public AnonymousAtOperationApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();

    [UnboundOperation(OperationType = OperationType.Action)]
    [AllowAnonymous]
    public void Hello() { }

    [UnboundOperation(OperationType = OperationType.Action)]
    [Authorize(Policy = "AdminOnly")]
    public void AdminGreeting() { }

    [UnboundOperation(OperationType = OperationType.Action)]
    public void DefaultGreeting() { }
}

/// <summary>
/// Base API class with [Authorize]. Used together with <see cref="InheritsAuthApi"/> to verify inheritance.
/// </summary>
[Authorize]
public class BaseAuthApi : ApiBase
{
    public BaseAuthApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }

    [Resource]
    public IQueryable<AnonPerson> People => System.Linq.Enumerable.Empty<AnonPerson>().AsQueryable();
}

/// <summary>
/// Subclass with no attributes — inherits [Authorize] from <see cref="BaseAuthApi"/>.
/// </summary>
public class InheritsAuthApi : BaseAuthApi
{
    public InheritsAuthApi(IEdmModel model, IQueryHandler q, ISubmitHandler s) : base(model, q, s) { }
}
