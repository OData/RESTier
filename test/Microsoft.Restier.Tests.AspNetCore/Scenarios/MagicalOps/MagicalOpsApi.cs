// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.OData.Edm;
using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;

namespace Microsoft.Restier.Tests.AspNetCore.Scenarios.MagicalOps;

public class MagicalOpsApi : ApiBase
{
    public MagicalOpsApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
        : base(model, queryHandler, submitHandler)
    {
    }

    // Nullable parameter — the #656 literal repro.
    [UnboundOperation]
    public int? Echo(int? parameter1) => parameter1;

    // Compiler-default optional parameter.
    [UnboundOperation]
    public int WithDefault(int parameter1 = 5) => parameter1;

    // Nullable + optional. Explicit null must beat default substitution.
    [UnboundOperation]
    public int? NullableWithDefault(int? parameter1 = 5) => parameter1;

    // Unknown complex input / unknown complex output — the #651 literal repro.
    [UnboundOperation]
    public SearchResult Search(SearchCriteria criteria)
        => new SearchResult { Found = (criteria?.Limit ?? 0) > 0 };

    [UnboundOperation]
    [Description("Returns nothing.")]
    [Obsolete("Use NewMethod instead.")]
    public int DeprecatedMethod() => 0;
}

public class SearchCriteria
{
    public string Query { get; set; }
    public int Limit { get; set; }
}

public class SearchResult
{
    public bool Found { get; set; }
}
