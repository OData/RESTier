// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents context for a query expression that
    /// is used during query expression processing.
    /// </summary>
    public class QueryExpressionContext
    {
        private const string MethodNameOfDataSourceStubValue = "GetPropertyValue";
        private const string MethodNameOfType = "OfType";

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

        /// <summary>
        /// This method is called by method call like Where/OfType/SelectMany and so on
        /// to create a model reference for whole function call.
        /// </summary>
        /// <param name="methodCall">
        /// An method call expression node.
        /// </param>
        /// <param name="source">
        /// The parameter model reference.
        /// </param>
        /// <param name="model">
        /// The edm model.
        /// </param>
        /// <returns>
        /// A reference to the model element
        /// that represents the expression node.
        /// </returns>
        private static QueryModelReference ComputeQueryModelReference(
            MethodCallExpression methodCall, QueryModelReference source, IEdmModel model)
        {
            var method = methodCall.Method;

            // source is a sequence of T and output is also a sequence of T
            var sourceType = method.GetParameters()[0].ParameterType.FindGenericType(typeof(IEnumerable<>));
            var resultType = method.ReturnType.FindGenericType(typeof(IEnumerable<>));
            if (sourceType == resultType)
            {
                return new QueryModelReference(source.EntitySet, source.Type);
            }

            Type resultElementType = null;
            if (resultType != null)
            {
                resultElementType = resultType.GenericTypeArguments[0];
            }

            // In case sourceType IEnumerable<Person> and resultType is
            // IEnumerable <SelectExpandBinder.SelectAllAndExpand<Person>>
            // or IEnumerable<SelectExpandBinder.SelectAll<Person>>
            // or IEnumerable<SelectExpandBinder.SelectSome<Person>>
            // or IEnumerable<SelectExpandBinder.SelectSomeAndInheritance<Person>>
            if (sourceType != null && resultType != null)
            {
                var resultGenericType = resultElementType;
                if (resultGenericType.IsGenericType)
                {
                    var resultFinalElementType = resultGenericType.GenericTypeArguments[0];
                    var sourceElementType = sourceType.GenericTypeArguments[0];

                    // Handle source is type of sub class and result is a base class
                    if (resultFinalElementType.IsAssignableFrom(sourceElementType))
                    {
                        return new QueryModelReference(source.EntitySet, source.Type);
                    }
                }
            }

            // In this case, the sourceType is null
            if (method.Name.Equals(MethodNameOfType))
            {
                // Did not consider multiple namespaces have same entity type case or customized namespace
                var edmEntityType = model.FindDeclaredType(resultElementType.FullName);
                var collectionType = new EdmCollectionType(
                    new EdmEntityTypeReference((IEdmEntityType)edmEntityType, false));
                return new QueryModelReference(source.EntitySet, collectionType);
            }

            // Till here, it means the result is not part of previous result and entity set will be null
            // This mean result is a collection as resultType is IEnumerable<>
            if (resultType != null)
            {
                // Did not consider multiple namespaces have same entity type case or customized namespace
                var edmElementType = model.FindDeclaredType(resultElementType.FullName);

                // This means result is collection of Entity/Complex/Enum
                IEdmTypeReference edmTypeReference = null;
                if (edmElementType != null)
                {
                    var edmType = edmElementType as IEdmType;
                    edmTypeReference = edmType.GetTypeReference();

                    if (edmTypeReference != null)
                    {
                        var collectionType = new EdmCollectionType(edmTypeReference);
                        return new QueryModelReference(null, collectionType);
                    }
                }

                // TODO Here means a collection of primitive type
            }

            // TODO Need to handle single result case
            // TODO GitHubIssue#29 : Handle projection operators in query expression
            return null;
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
                        var model = this.QueryContext.Model;
                        modelReference = ComputeQueryModelReference(methodCall, thisModelReference, model);
                    }
                }

                return modelReference;
            }

            var parameter = this.VisitedNode as ParameterExpression;
            if (parameter != null)
            {
                return ComputeParameterModelReference(parameter);
            }

            var member = this.VisitedNode as MemberExpression;
            if (member != null)
            {
                return ComputeMemberModelReference(member);
            }

            return null;
        }

        private QueryModelReference ComputeParameterModelReference(ParameterExpression parameter)
        {
            QueryModelReference modelReference = null;
            foreach (var node in this.GetExpressionTrail())
            {
                var methodCall = node as MethodCallExpression;
                if (methodCall == null)
                {
                    continue;
                }

                modelReference = this.GetModelReferenceForNode(node);
                if (modelReference == null)
                {
                    continue;
                }

                var method = methodCall.Method;
                var sourceType = method.GetParameters()[0].ParameterType.FindGenericType(typeof(IEnumerable<>));
                var resultType = method.ReturnType.FindGenericType(typeof(IEnumerable<>));
                if (sourceType != resultType)
                {
                    // In case sourceType IEnumerable<Person> and resultType is
                    // IEnumerable <SelectExpandBinder.SelectAllAndExpand<Person>>
                    // or IEnumerable<SelectExpandBinder.SelectAll<Person>>
                    // or IEnumerable<SelectExpandBinder.SelectSome<Person>>
                    // or IEnumerable<SelectExpandBinder.SelectSomeAndInheritance<Person>>
                    if (sourceType == null || resultType == null)
                    {
                        continue;
                    }

                    var resultGenericType = resultType.GenericTypeArguments[0];
                    if (!resultGenericType.IsGenericType ||
                        resultGenericType.GenericTypeArguments[0] != sourceType.GenericTypeArguments[0])
                    {
                        continue;
                    }
                }

                var typeOfT = sourceType.GenericTypeArguments[0];
                if (parameter.Type == typeOfT)
                {
                    var collectionType = modelReference.Type as IEdmCollectionType;
                    if (collectionType != null)
                    {
                        modelReference = new ParameterModelReference(
                            modelReference.EntitySet,
                            collectionType.ElementType.Definition);
                        return modelReference;
                    }
                }
            }

            return modelReference;
        }

        private QueryModelReference ComputeMemberModelReference(MemberExpression member)
        {
            QueryModelReference modelReference = null;
            var memberExp = member.Expression;
            if (memberExp.NodeType == ExpressionType.Parameter)
            {
                modelReference = this.GetModelReferenceForNode(memberExp);
            }
            else if (memberExp.NodeType == ExpressionType.TypeAs)
            {
                var resultType = memberExp.Type;
                var parameterExpression = (memberExp as UnaryExpression).Operand;

                // Handle result is employee, and get person's property case
                // member expression will be "Param_0 As Person"
                if (parameterExpression.Type.IsSubclassOf(resultType))
                {
                    modelReference = this.GetModelReferenceForNode(parameterExpression);
                }
                else
                {
                    // member expression will be "Param_0 As Employee"
                    var emdEntityType = this.QueryContext.Model.FindDeclaredType(resultType.FullName);

                    var structuredType = emdEntityType as IEdmStructuredType;
                    if (structuredType != null)
                    {
                        var property = structuredType.FindProperty(member.Member.Name);
                        modelReference = this.GetModelReferenceForNode(parameterExpression);
                        modelReference = new PropertyModelReference(member.Member.Name, property, modelReference);
                        return modelReference;
                    }
                }
            }

            if (modelReference != null)
            {
                modelReference = new PropertyModelReference(
                    modelReference, member.Member.Name);
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
