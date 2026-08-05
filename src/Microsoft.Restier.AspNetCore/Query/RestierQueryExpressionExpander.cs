// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.AspNetCore.Model;
using Microsoft.Restier.Core.Query;
using System;
using System.Linq.Expressions;

namespace Microsoft.Restier.AspNetCore.Query;

/// <summary>
/// A Query expression expander for Restier Api.
/// </summary>
public class RestierQueryExpressionExpander : IQueryExpressionExpander
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestierQueryExpressionExpander"/> class.
    /// </summary>
    /// <param name="modelExtender">The model extender.</param>
    public RestierQueryExpressionExpander(RestierWebApiModelExtender modelExtender) => ModelExtender = modelExtender;

    /// <summary>
    /// Gets or sets the inner expander.
    /// </summary>
    public IQueryExpressionExpander Inner { get; set; }

    private RestierWebApiModelExtender ModelExtender { get; set; }

    /// <inheritdoc/>
    public Expression Expand(QueryExpressionContext context)
    {
        Ensure.NotNull(context, nameof(context));

        var result = CallInner(context);
        if (result is not null)
        {
            return result;
        }

        // Ensure this query constructs from DataSourceStub.
        if (context.ModelReference is DataSourceStubModelReference)
        {
            // Only expand entity set query which returns IQueryable<T>.
            var query = ModelExtender.GetEntitySetQuery(context);
            if (query is not null)
            {
                return query.Expression;
            }
        }

        // No expansion happened just return the node itself.
        return context.VisitedNode;
    }

    private Expression CallInner(QueryExpressionContext context)
    {
        return Inner?.Expand(context);
    }
}