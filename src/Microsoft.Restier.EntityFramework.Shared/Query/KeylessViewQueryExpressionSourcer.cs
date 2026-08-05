// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Chained <see cref="IQueryExpressionSourcer"/> that resolves keyless-view
    /// function-import references against the per-API <see cref="KeylessViewRegistry"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registered ahead of <see cref="EFQueryExpressionSourcer"/> in the chain so it
    /// gets first crack at <see cref="DataSourceStubModelReference"/> nodes whose
    /// <see cref="DataSourceStubModelReference.Element"/> is an
    /// <see cref="IEdmFunctionImport"/>. Regular entity-set references fall through
    /// unchanged to the next link in the chain.
    /// </para>
    /// <para>
    /// Lives in the shared EF project; functionally inert on EF6 — the registry is
    /// always empty there because the EF6 model-builder partial throws on keyless
    /// CLR types.
    /// </para>
    /// </remarks>
    internal sealed class KeylessViewQueryExpressionSourcer : IQueryExpressionSourcer
    {
        private readonly KeylessViewRegistry registry;
        private readonly RestierEFOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeylessViewQueryExpressionSourcer"/> class.
        /// </summary>
        /// <param name="registry">The per-API keyless-view registry.</param>
        /// <param name="options">The per-API EF options (used for tracking behavior).</param>
        public KeylessViewQueryExpressionSourcer(KeylessViewRegistry registry, RestierEFOptions options)
        {
            this.registry = registry;
            this.options = options ?? new RestierEFOptions();
        }

        /// <summary>
        /// Gets or sets the inner handler.
        /// </summary>
        public IQueryExpressionSourcer Inner { get; set; }

        /// <summary>
        /// Replaces the queryable source for a function-import reference if the
        /// registry has an entry for it; otherwise returns <c>null</c> so the
        /// chain can fall through.
        /// </summary>
        /// <param name="context">The query expression context.</param>
        /// <param name="embedded">Indicates whether the sourcing is occurring on an embedded node.</param>
        /// <returns>
        /// A <see cref="ConstantExpression"/> wrapping the registry-supplied
        /// <see cref="System.Linq.IQueryable"/> (with tracking applied when the
        /// caller has set <see cref="QueryRequest.AllowNoTracking"/>); or
        /// <c>null</c> when this sourcer is not the right handler.
        /// </returns>
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            Ensure.NotNull(context, nameof(context));

            var inner = Inner?.ReplaceQueryableSource(context, embedded);
            if (inner is not null)
            {
                return inner;
            }

            if (registry is null)
            {
                // EF6 wiring path before the registry has been threaded through;
                // also makes the type forgiving for direct unit-test instantiation.
                return null;
            }

            if (context.ModelReference is not DataSourceStubModelReference stub)
            {
                return null;
            }

            if (stub.Element is not IEdmFunctionImport functionImport)
            {
                return null;
            }

            if (!registry.TryGet(functionImport.Name, out var entry))
            {
                return null;
            }

            var underlying = entry.SourceFactory(context.QueryContext.Api);
            if (underlying is null)
            {
                return null;
            }

            if (!context.QueryContext.Request.AllowNoTracking)
            {
                return Expression.Constant(underlying);
            }

            var transformed = EFQueryExpressionSourcer.ApplyTracking(
                underlying,
                options.TrackingBehavior,
                context.QueryContext.Request.HasRecursiveExpand);
            return Expression.Constant(transformed);
        }
    }
}
