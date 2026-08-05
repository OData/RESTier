// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

// This file is only compiled for EF6. EF Core is not affected by this issue.
#if !EFCore

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Entity;
using Microsoft.Restier.Core.Query;

namespace Microsoft.Restier.EntityFramework
{
    /// <summary>
    /// Workaround for <see href="https://github.com/OData/AspNetCoreOData/issues/367"/>.
    /// OData v9's SelectExpandBinder injects IEdmModel constants into LINQ expression trees
    /// when processing $expand/$select. The resulting expression tree is not EF6 compatible
    /// because EF6 cannot translate IEdmModel instances to SQL. EF Core is not affected.
    /// This helper detects the SelectExpand projection, strips it before EF6 execution,
    /// adds Include() calls to eagerly load navigation properties, executes against EF6,
    /// then re-applies the projection in-memory on the materialized results.
    /// </summary>
    internal static class SelectExpandHelper
    {
        private const string InterfaceNameISelectExpandWrapper = "ISelectExpandWrapper";

        /// <summary>
        /// Checks whether TElement is an OData SelectExpandWrapper type.
        /// </summary>
        public static bool HasSelectExpandProjection<TElement>()
        {
            return typeof(TElement).GetInterface(InterfaceNameISelectExpandWrapper) is not null;
        }

        /// <summary>
        /// Executes a query that contains a SelectExpand projection by:
        /// 1. Finding and stripping the SelectExpand Select from the expression tree
        /// 2. Rebuilding outer LINQ operations (Take, Skip, etc.) with correct generic types
        /// 3. Executing the stripped query against EF to load entities (with navigation properties via the projection)
        /// 4. Re-applying the SelectExpand projection in-memory
        /// </summary>
        public static async Task<QueryResult> ExecuteWithClientProjectionAsync<TElement>(
            IQueryable<TElement> query,
            CancellationToken cancellationToken)
        {
            // Walk the expression tree to find the SelectExpand Select and any outer operations
            var (selectNode, outerOps) = FindSelectAndOuterOps(query.Expression);

            if (selectNode is null)
            {
                // Shouldn't happen since we checked HasSelectExpandProjection, but fallback
                return new QueryResult(await query.ToArrayAsync(cancellationToken).ConfigureAwait(false));
            }

            // Get the source expression (before the Select) and the projection lambda
            var sourceExpression = selectNode.Arguments[0];
            var selectArg = selectNode.Arguments[1];
            var selectLambda = selectArg is UnaryExpression unary
                ? unary.Operand as LambdaExpression
                : selectArg as LambdaExpression;

            // Get the source element type
            var sourceQueryableType = sourceExpression.Type.FindGenericType(typeof(IQueryable<>));
            if (sourceQueryableType is null || selectLambda is null)
            {
                return new QueryResult(await query.ToArrayAsync(cancellationToken).ConfigureAwait(false));
            }

            var sourceElementType = sourceQueryableType.GetGenericArguments()[0];

            // Use reflection to call the generic implementation
            var method = typeof(SelectExpandHelper)
                .GetMethod(nameof(ExecuteCoreAsync), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(sourceElementType, typeof(TElement));

            return await ((Task<QueryResult>)method.Invoke(null, new object[]
            {
                query.Provider, sourceExpression, selectLambda, outerOps, cancellationToken
            })).ConfigureAwait(false);
        }

        private static async Task<QueryResult> ExecuteCoreAsync<TSource, TElement>(
            IQueryProvider provider,
            Expression sourceExpression,
            LambdaExpression selectLambda,
            List<(string MethodName, object[] Args)> outerOps,
            CancellationToken cancellationToken)
        {
            // Rebuild the query with outer operations applied to the source (without the Select)
            var baseQuery = provider.CreateQuery<TSource>(sourceExpression);

            // Apply outer operations (Take, Skip, etc.) to the source query
            IQueryable<TSource> efQuery = baseQuery;
            foreach (var (methodName, args) in outerOps)
            {
                if (methodName == "Take" && args.Length == 1 && args[0] is int takeCount)
                {
                    efQuery = efQuery.Take(takeCount);
                }
                else if (methodName == "Skip" && args.Length == 1 && args[0] is int skipCount)
                {
                    efQuery = efQuery.Skip(skipCount);
                }
                // Other operations (Where, OrderBy, etc.) are already in the sourceExpression
            }

            // Add Include() calls for navigation properties referenced by the $expand projection
            // so EF eagerly loads the related data
            var navProperties = ExtractExpandedNavigationProperties(selectLambda);
            foreach (var navProp in navProperties)
            {
                efQuery = efQuery.Include(navProp);
            }

            // Execute against EF to load entities with navigation properties
            var materializedEntities = await efQuery.ToArrayAsync(cancellationToken).ConfigureAwait(false);

            // The OData SelectExpandBinder generates a SQL-style projection (no null guards)
            // for the EF6 LINQ provider, on the assumption that LEFT JOIN + SQL null propagation
            // will handle missing rows. We're running that lambda in-memory now, so a null
            // navigation property (e.g. an orphan Book with no Publisher) NREs on the first
            // nested member access (book.Publisher.Books). Rewrite the body to short-circuit
            // member access and instance calls on a null receiver before compiling.
            var nullSafeBody = NullSafeMemberAccessRewriter.Rewrite(selectLambda.Body, selectLambda.Parameters[0]);
            var nullSafeLambda = Expression.Lambda<Func<TSource, TElement>>(nullSafeBody, selectLambda.Parameters);
            var compiledSelect = nullSafeLambda.Compile();
            var projected = materializedEntities.Select(compiledSelect).ToArray();

            return new QueryResult(projected);
        }

        /// <summary>
        /// Walks the expression tree to find the SelectExpand Select node and collect
        /// any outer LINQ operations (Take, Skip) that were applied after the Select.
        /// </summary>
        private static (MethodCallExpression SelectNode, List<(string, object[])> OuterOps) FindSelectAndOuterOps(Expression expression)
        {
            var outerOps = new List<(string, object[])>();
            var current = expression;

            while (current is MethodCallExpression methodCall)
            {
                // Check if this is the SelectExpand Select
                if (methodCall.Method.Name == "Select" && methodCall.Arguments.Count == 2)
                {
                    var returnType = methodCall.Type;
                    if (returnType.IsGenericType)
                    {
                        var elementType = returnType.GetGenericArguments()[0];
                        if (elementType.GetInterface(InterfaceNameISelectExpandWrapper) is not null)
                        {
                            // Reverse outerOps so they're in application order
                            outerOps.Reverse();
                            return (methodCall, outerOps);
                        }
                    }
                }

                // This is an outer operation wrapping the Select - record it
                if (methodCall.Method.Name == "Take" || methodCall.Method.Name == "Skip")
                {
                    // Extract the constant argument
                    var constArg = methodCall.Arguments.Count > 1 ? ExtractConstantValue(methodCall.Arguments[1]) : null;
                    outerOps.Add((methodCall.Method.Name, constArg is not null ? new[] { constArg } : Array.Empty<object>()));
                }

                // Move to the source (first argument)
                current = methodCall.Arguments.Count > 0 ? methodCall.Arguments[0] : null;
            }

            return (null, outerOps);
        }

        /// <summary>
        /// Extracts a constant value from an expression (handles ConstantExpression directly
        /// and also LinqParameterContainer wrappers).
        /// </summary>
        private static object ExtractConstantValue(Expression expression)
        {
            if (expression is ConstantExpression constant)
            {
                return constant.Value;
            }

            // OData wraps constants in LinqParameterContainer
            if (expression is MemberExpression member && member.Expression is ConstantExpression containerConst)
            {
                try
                {
                    var container = containerConst.Value;
                    var property = container.GetType().GetProperty(member.Member.Name)
                        ?? (MemberInfo)container.GetType().GetField(member.Member.Name);

                    if (property is PropertyInfo pi)
                        return pi.GetValue(container);
                    if (property is FieldInfo fi)
                        return fi.GetValue(container);
                }
                catch
                {
                    // Ignore reflection errors
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the names of navigation properties from a SelectExpand projection lambda.
        /// The lambda body contains MemberAccess expressions like $it.Publisher that indicate
        /// which navigation properties should be loaded.
        /// </summary>
        private static List<string> ExtractExpandedNavigationProperties(LambdaExpression selectLambda)
        {
            var navProperties = new List<string>();
            var visitor = new NavigationPropertyFinder(selectLambda.Parameters[0], navProperties);
            visitor.Visit(selectLambda.Body);
            return navProperties;
        }

        /// <summary>
        /// An ExpressionVisitor that finds navigation property accesses on the lambda parameter.
        /// These are MemberAccess expressions like "param.Publisher" where the member type is
        /// a complex/entity type (not a primitive).
        /// </summary>
        private class NavigationPropertyFinder : ExpressionVisitor
        {
            private readonly ParameterExpression parameter;
            private readonly List<string> navProperties;

            public NavigationPropertyFinder(ParameterExpression parameter, List<string> navProperties)
            {
                this.parameter = parameter;
                this.navProperties = navProperties;
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                // Check if this is a property access on the lambda parameter
                if (node.Expression == parameter && node.Member is PropertyInfo propInfo)
                {
                    var propType = propInfo.PropertyType;
                    // Navigation properties are non-primitive, non-string types (entities or collections)
                    if (!propType.IsPrimitive && propType != typeof(string) && propType != typeof(decimal)
                        && propType != typeof(DateTime) && propType != typeof(DateTimeOffset)
                        && propType != typeof(Guid) && propType != typeof(byte[])
                        && !propType.IsEnum)
                    {
                        if (!navProperties.Contains(propInfo.Name))
                        {
                            navProperties.Add(propInfo.Name);
                        }
                    }
                }

                return base.VisitMember(node);
            }
        }

        /// <summary>
        /// Rewrites member access and instance method calls so that a null receiver short-circuits
        /// to a default value, matching SQL/EF semantics for the OData projection lambda when
        /// it's executed in-memory. The lambda parameter itself is never guarded — it's never null.
        /// </summary>
        private sealed class NullSafeMemberAccessRewriter : ExpressionVisitor
        {
            private readonly ParameterExpression rootParameter;

            private NullSafeMemberAccessRewriter(ParameterExpression rootParameter)
            {
                this.rootParameter = rootParameter;
            }

            public static Expression Rewrite(Expression body, ParameterExpression rootParameter)
                => new NullSafeMemberAccessRewriter(rootParameter).Visit(body);

            protected override Expression VisitMember(MemberExpression node)
            {
                var newSource = node.Expression is null ? null : Visit(node.Expression);
                var updated = node.Update(newSource);

                if (!NeedsGuard(newSource))
                {
                    return updated;
                }

                return BuildNullGuard(newSource, updated, node.Type);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var newObj = node.Object is null ? null : Visit(node.Object);
                var newArgs = new Expression[node.Arguments.Count];
                for (var i = 0; i < node.Arguments.Count; i++)
                {
                    newArgs[i] = Visit(node.Arguments[i]);
                }

                var updated = node.Update(newObj, newArgs);

                // Instance method on a potentially-null reference receiver.
                if (newObj is not null && NeedsGuard(newObj))
                {
                    return BuildNullGuard(newObj, updated, node.Type);
                }

                // Extension/static method whose first argument is the de-facto receiver
                // (e.g. Enumerable.Select(source, selector)) — guard on that source.
                if (newObj is null && node.Method.IsStatic && newArgs.Length > 0 && NeedsGuard(newArgs[0]))
                {
                    return BuildNullGuard(newArgs[0], updated, node.Type);
                }

                return updated;
            }

            private bool NeedsGuard(Expression expr)
            {
                if (expr is null)
                {
                    return false;
                }

                // The root lambda parameter is never null by construction.
                if (expr == rootParameter)
                {
                    return false;
                }

                // Value types (other than Nullable<T>) cannot be null.
                if (expr.Type.IsValueType && Nullable.GetUnderlyingType(expr.Type) is null)
                {
                    return false;
                }

                // Constants speak for themselves — let them flow through.
                if (expr is ConstantExpression)
                {
                    return false;
                }

                return true;
            }

            private static Expression BuildNullGuard(Expression receiver, Expression access, Type accessType)
            {
                var nullConst = Expression.Constant(null, receiver.Type);
                var isNull = Expression.Equal(receiver, nullConst);
                var defaultValue = Expression.Default(accessType);
                return Expression.Condition(isNull, defaultValue, access, accessType);
            }
        }

        /// <summary>
        /// Extension method to find a generic type in a type's hierarchy.
        /// </summary>
        internal static Type FindGenericType(this Type type, Type genericTypeDefinition)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
            {
                return type;
            }

            foreach (var iface in type.GetInterfaces())
            {
                var found = iface.FindGenericType(genericTypeDefinition);
                if (found is not null) return found;
            }

            return null;
        }
    }
}

#endif
