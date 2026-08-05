// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Query;
using System.Linq.Expressions;

namespace Microsoft.Restier.AspNetCore.Query;

/// <summary>
/// Gets the source of the query.
/// </summary>
public class RestierQueryExpressionSourcer : IQueryExpressionSourcer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierQueryExpressionSourcer"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender.</param>
    public RestierQueryExpressionSourcer(RestierWebApiModelExtender modelExtender) => ModelExtender = modelExtender;

    /// <summary>
    /// Gets or sets the inner handler.
    /// </summary>
    public IQueryExpressionSourcer Inner { get; set; }

    private RestierWebApiModelExtender ModelExtender { get; set; }

    /// <inheritdoc/>
    public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
    {
        var result = CallInner(context, embedded);
        if (result is not null)
        {
            // Call the provider's sourcer to find the source of the query.
            return result;
        }

        // This sourcer ONLY deals with queries that cannot be addressed by the provider
        // such as a singleton query that cannot be sourced by the EF provider, etc.
        var query = ModelExtender.GetEntitySetQuery(context) ?? ModelExtender.GetSingletonQuery(context);
        if (query is not null)
        {
            return Expression.Constant(query);
        }

        return null;
    }

    private Expression CallInner(QueryExpressionContext context, bool embedded)
    {
        if (Inner is not null)
        {
            return Inner.ReplaceQueryableSource(context, embedded);
        }

        return null;
    }
}