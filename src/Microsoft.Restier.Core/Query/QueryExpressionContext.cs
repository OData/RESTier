// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents context for a query expression that
    /// is used during query expression processing.
    /// </summary>
    public class QueryExpressionContext
    {
        private const string MethodNameOfDataSourceStubValue = "GetPropertyValue";

        private Stack<Expression> visitedNodes = new Stack<Expression>();
        private IDictionary<Expression, QueryModelReference> modelReferences =
            new Dictionary<Expression, QueryModelReference>();

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryExpressionContext" /> class.
        /// </summary>
        /// <param name="queryContext">
        /// A query context.
        /// </param>
        public QueryExpressionContext(QueryContext queryContext)
        {
            Ensure.NotNull(queryContext, "queryContext");
            this.QueryContext = queryContext;
        }

        /// <summary>
        /// Gets the query context associated with this context.
        /// </summary>
        public QueryContext QueryContext { get; private set; }

        /// <summary>
        /// Gets the expression node that is being visited.
        /// </summary>
        public Expression VisitedNode
        {
            get
            {
                if (this.visitedNodes.Count == 0)
                {
                    return null;
                }

                return this.visitedNodes.Peek();
            }
        }

        /// <summary>
        /// Gets a reference to the model element
        /// that represents the visited node.
        /// </summary>
        public QueryModelReference ModelReference
        {
            get
            {
                return this.GetModelReferenceForNode(this.VisitedNode);
            }
        }

        /// <summary>
        /// Gets or sets an action that is invoked after an
        /// expanded or filtered expression has been visited.
        /// </summary>
        public Action AfterNestedVisitCallback { get; set; }

        /// <summary>
        /// Pushes a visited node.
        /// </summary>
        /// <param name="visitedNode">
        /// A visited node.
        /// </param>
        public void PushVisitedNode(Expression visitedNode)
        {
            this.visitedNodes.Push(visitedNode);
            this.UpdateModelReference();
        }

        /// <summary>
        /// Replaces the visited node.
        /// </summary>
        /// <param name="visitedNode">
        /// A new visited node.
        /// </param>
        public void ReplaceVisitedNode(Expression visitedNode)
        {
            this.visitedNodes.Pop();
            this.visitedNodes.Push(visitedNode);
            this.UpdateModelReference();
        }

        /// <summary>
        /// Pops a visited node.
        /// </summary>
        public void PopVisitedNode()
        {
            this.visitedNodes.Pop();
            this.UpdateModelReference();
        }

        /// <summary>
        /// This method is called by method call like Where/OfType/SelectMany and so on 
        /// to create a model reference for whole function call.
        /// </summary>
        private static QueryModelReference ComputeQueryModelReference(
            MethodCallExpression methodCall, QueryModelReference source)
        {
            var method = methodCall.Method;

            // source is a sequence of T and output is also a sequence of T
            var sourceType = method.GetParameters()[0].ParameterType.FindGenericType(typeof(IEnumerable<>));
            var resultType = method.ReturnType.FindGenericType(typeof(IEnumerable<>));
            if (sourceType == resultType)
            {
                return new QueryModelReference(source.EntitySet,source.Type);
            }

            // source is a sequence of T1 and output is a sequence of T2
            // Like query People(key)/Trips or People/NS.DerivedPeople
            // TODO Null is return in these cases now, need return correct value

            // TODO GitHubIssue#29 : Handle projection operators in query expression
            return null;
        }

        /// <summary>
        /// Gets a reference to the model element
        /// that represents an expression node.
        /// </summary>
        /// <param name="node">
        /// An expression node.
        /// </param>
        /// <returns>
        /// A reference to the model element
        /// that represents the expression node.
        /// </returns>
        public QueryModelReference GetModelReferenceForNode(Expression node)
        {
            QueryModelReference modelReference = null;
            if (node != null)
            {
                this.modelReferences.TryGetValue(node, out modelReference);
            }

            return modelReference;
        }

        private void UpdateModelReference()
        {
            if (this.VisitedNode != null &&
                !this.modelReferences.ContainsKey(this.VisitedNode))
            {
                var modelReference = this.ComputeModelReference();
                if (modelReference != null)
                {
                    this.modelReferences.Add(
                        this.VisitedNode, modelReference);
                }
            }
        }

        private QueryModelReference ComputeModelReference()
        {
            QueryModelReference modelReference = null;

            var methodCall = this.VisitedNode as MethodCallExpression;
            var parameter = this.VisitedNode as ParameterExpression;
            var member = this.VisitedNode as MemberExpression;

            if (methodCall != null)
            {
                var method = methodCall.Method;
                if (method.DeclaringType == typeof(DataSourceStub) &&
                    method.Name != MethodNameOfDataSourceStubValue)
                {
                    modelReference = ComputeDataSourceStubReference(methodCall);
                }
                else if (method.GetCustomAttributes<ExtensionAttribute>().Any())
                {
                    var thisModelReference = this.GetModelReferenceForNode(methodCall.Arguments[0]);
                    if (thisModelReference != null)
                    {
                        modelReference = ComputeQueryModelReference(methodCall, thisModelReference);
                    }
                }
            }
            else if (parameter != null)
            {
                foreach (var node in this.GetExpressionTrail())
                {
                    methodCall = node as MethodCallExpression;
                    if (methodCall != null)
                    {
                        modelReference = this.GetModelReferenceForNode(node);
                        if (modelReference != null)
                        {
                            var method = methodCall.Method;
                            var sourceType = method.GetParameters()[0]
                                .ParameterType.FindGenericType(typeof(IEnumerable<>));
                            var resultType = method.ReturnType
                                .FindGenericType(typeof(IEnumerable<>));
                            if (sourceType == resultType)
                            {
                                var typeOfT = sourceType.GetGenericArguments()[0];
                                if (parameter.Type == typeOfT)
                                {
                                    var collectionType = modelReference.Type as IEdmCollectionType;
                                    if (collectionType != null)
                                    {
                                        modelReference = new QueryModelReference(modelReference.EntitySet, collectionType.ElementType.Definition);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if (member != null)
            {
                modelReference = this.GetModelReferenceForNode(member.Expression);
                if (modelReference != null)
                {
                    modelReference = new PropertyModelReference(
                        modelReference, member.Member.Name);
                }
            }

            return modelReference;
        }

        private DataSourceStubModelReference ComputeDataSourceStubReference(
            MethodCallExpression methodCall)
        {
            DataSourceStubModelReference modelReference = null;
            ConstantExpression namespaceName = null;
            ConstantExpression name = null;
            var argumentIndex = 0;
            if (methodCall.Method.GetParameters().Length > 2)
            {
                namespaceName = methodCall.Arguments[argumentIndex++] as ConstantExpression;
            }

            name = methodCall.Arguments[argumentIndex++] as ConstantExpression;
            if ((argumentIndex == 1 || namespaceName != null) && name != null)
            {
                var nameValue = name.Value as string;
                if (nameValue != null)
                {
                    if (namespaceName == null)
                    {
                        modelReference = new DataSourceStubModelReference(
                            this.QueryContext, nameValue);
                    }
                    else
                    {
                        modelReference = new DataSourceStubModelReference(
                            this.QueryContext,
                            namespaceName.Value as string,
                            nameValue);
                    }
                }
            }

            return modelReference;
        }

        private IEnumerable<Expression> GetExpressionTrail()
        {
            return this.visitedNodes.TakeWhile(node => node != null);
        }
    }
}
