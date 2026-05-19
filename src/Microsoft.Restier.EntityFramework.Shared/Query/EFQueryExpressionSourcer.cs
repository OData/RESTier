// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
#if !EFCore
using System.Data.Entity;
#endif
using System.Linq;
using System.Linq.Expressions;
#if EFCore
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Query;

#if EFCore
namespace Microsoft.Restier.EntityFrameworkCore
#else
namespace Microsoft.Restier.EntityFramework
#endif
{
    /// <summary>
    /// Represents a query expression sourcer that uses a DbContext.
    /// </summary>
    internal class EFQueryExpressionSourcer : IQueryExpressionSourcer
    {
        private readonly RestierEFOptions options;

        /// <summary>
        /// Parameterless constructor — uses default <see cref="RestierEFOptions"/>.
        /// Retained so tests and code paths that instantiate the sourcer
        /// directly continue to work; the DI registration uses the
        /// <see cref="EFQueryExpressionSourcer(RestierEFOptions)"/> overload.
        /// </summary>
        public EFQueryExpressionSourcer()
            : this(new RestierEFOptions())
        {
        }

        /// <summary>
        /// Constructor used by DI — receives the per-API
        /// <see cref="RestierEFOptions"/> singleton.
        /// </summary>
        public EFQueryExpressionSourcer(RestierEFOptions options)
        {
            this.options = options ?? new RestierEFOptions();
        }

        /// <summary>
        /// Gets or sets the inner handler.
        /// </summary>
        public IQueryExpressionSourcer Inner { get; set; }

        /// <summary>
        /// Sources an expression.
        /// </summary>
        /// <param name="context">
        /// The query expression context.
        /// </param>
        /// <param name="embedded">
        /// Indicates if the sourcing is occurring on an embedded node.
        /// </param>
        /// <returns>
        /// A data source expression that represents the visited node.
        /// </returns>
        public Expression ReplaceQueryableSource(QueryExpressionContext context, bool embedded)
        {
            Ensure.NotNull(context, nameof(context));

            var result = Inner?.ReplaceQueryableSource(context, embedded);
            if (result != null)
            {
                // If the inner handler has produced a result, return it.
                return result;
            }

            if (context.ModelReference.EntitySet is null)
            {
                // EF provider can only source *ResourceSet*.
                return null;
            }


            if (!(context.QueryContext.Api is IEntityFrameworkApi frameworkApi))
            {
                // Not an EF Api.
                return null;
            }

            var dbContextType = frameworkApi.ContextType;
            var dbContext = frameworkApi.DbContext;

            var dbSetProperty = frameworkApi.ContextType.GetProperties()
                .FirstOrDefault(prop => prop.Name == context.ModelReference.EntitySet.Name);
            if (dbSetProperty is null)
            {
                // EF provider can only source EntitySet from *DbSet property*.
                return null;
            }

            if (!embedded)
            {
                var dbSet = (IQueryable)dbSetProperty.GetValue(dbContext);

                // Submit pipeline, deep-update classifier, ResourceExists checks,
                // and any direct api.QueryAsync call leave AllowNoTracking false;
                // those paths require tracked entities so EFChangeSetInitializer
                // can mutate them via dbContext.Entry(...). Only the controller's
                // HTTP read paths opt into the no-tracking transformation.
                if (!context.QueryContext.Request.AllowNoTracking)
                {
                    return Expression.Constant(dbSet);
                }

                var transformed = ApplyTracking(
                    dbSet,
                    options.TrackingBehavior,
                    context.QueryContext.Request.HasRecursiveExpand);

                return Expression.Constant(transformed);
            }
            else
            {
                return Expression.MakeMemberAccess(
                    Expression.Constant(dbContext),
                    dbSetProperty);
            }
        }

        private static IQueryable ApplyTracking(
            IQueryable dbSet,
            RestierEFTrackingBehavior behavior,
            bool hasRecursiveExpand)
        {
            switch (behavior)
            {
                case RestierEFTrackingBehavior.TrackAll:
                    return dbSet;

                case RestierEFTrackingBehavior.NoTracking:
                    return CallAsNoTracking(dbSet);

                case RestierEFTrackingBehavior.NoTrackingWithIdentityResolution:
#if EFCore
                    return CallAsNoTrackingWithIdentityResolution(dbSet);
#else
                    return CallAsNoTracking(dbSet);
#endif

                case RestierEFTrackingBehavior.Default:
                default:
#if EFCore
                    return CallAsNoTrackingWithIdentityResolution(dbSet);
#else
                    // EF6: AsNoTracking by default, but if the request shape has an expand
                    // cycle, fall back to tracked so identity resolution holds across the
                    // cycle. EFCore does not need this branch — identity resolution is
                    // always preserved by AsNoTrackingWithIdentityResolution.
                    return hasRecursiveExpand ? dbSet : CallAsNoTracking(dbSet);
#endif
            }
        }

        private static IQueryable CallAsNoTracking(IQueryable dbSet)
        {
            var elementType = dbSet.GetType().GetGenericArguments()[0];
#if EFCore
            var method = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking))
                .MakeGenericMethod(elementType);
#else
            var method = typeof(System.Data.Entity.QueryableExtensions)
                .GetMethods()
                .Single(m => m.Name == nameof(System.Data.Entity.QueryableExtensions.AsNoTracking)
                    && m.IsGenericMethodDefinition)
                .MakeGenericMethod(elementType);
#endif
            return (IQueryable)method.Invoke(null, new object[] { dbSet });
        }

#if EFCore
        private static IQueryable CallAsNoTrackingWithIdentityResolution(IQueryable dbSet)
        {
            var elementType = dbSet.GetType().GetGenericArguments()[0];
            var method = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                .GetMethod(nameof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .AsNoTrackingWithIdentityResolution))
                .MakeGenericMethod(elementType);
            return (IQueryable)method.Invoke(null, new object[] { dbSet });
        }
#endif
    }
}
