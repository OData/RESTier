// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

#region Permanent Exclusions
[assembly: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ValidationException", Justification = "We do not intend to support serialization of this exception yet")]
[assembly: SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Scope = "member", Target = "Microsoft.Restier.WebApi.Batch.ODataDomainChangeSetRequestItem.#DisposeResponses(System.Collections.Generic.IEnumerable`1<System.Net.Http.HttpResponseMessage>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.DomainBase.#Dispose()", Justification = "Need to do some clean up before the virtual Dispose(disposing) method gets called.")]
[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "These assemblies are delay-signed.")]
[assembly: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope = "member", Target = "Microsoft.Restier.EntityFramework.Model.ModelProducer.#ProduceModelAsync(Microsoft.Restier.Core.Model.ModelContext,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ValidationResults")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "Microsoft.Restier.Security.DomainPermission")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Microsoft.Restier.Core.Submit.ChangeSetEntry.#Type")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryModelReference.#Type")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "itemValue", Scope = "member", Target = "Microsoft.Restier.Core.Submit.DataModificationEntry.#ApplyPredicate(System.Linq.Expressions.ParameterExpression,System.Linq.Expressions.Expression,System.Collections.Generic.KeyValuePair`2<System.String,System.Object>)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "Microsoft.Restier.EntityFramework.Model.ModelProducer.#.cctor()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)", Justification = "Instance is disposed elsewhere")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ValidationException", Justification = "We do not intend to support serialization of this exception yet")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly", Justification = "AssemblyInformationalVersion could be string.")]

#region CA1004 Generic method with type parameter
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.Query.IQueryExecutor.#ExecuteSingleAsync`1(Microsoft.Restier.Core.Query.QueryContext,System.Linq.IQueryable,System.Linq.Expressions.Expression,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapODataDomainRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,System.Func`1<Microsoft.Restier.Core.IDomain>,Microsoft.Restier.WebApi.Batch.ODataDomainBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapODataDomainRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,Microsoft.Restier.WebApi.Batch.ODataDomainBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#CreateODataDomainRoutingConventions`1(System.Web.Http.HttpConfiguration,Microsoft.OData.Edm.IEdmModel)")]
#endregion

#region CA1020 Few types in namespace
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.EntityFramework.Model")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.EntityFramework.Query")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.EntityFramework.Submit")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.EntityFramework")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.WebApi.Results")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.WebApi.Formatter.Serialization")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.WebApi.Routing")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.WebApi.Filters")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.WebApi.Batch")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Core.Model")]
#endregion

#region CA1026 Default Parameter
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.Batch.ODataDomainBatchHandler.#.ctor(System.Web.Http.HttpServer,System.Func`1<Microsoft.Restier.Core.IDomain>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#GetModelAsync(Microsoft.Restier.Core.IDomain,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#GetModelAsync(Microsoft.Restier.Core.DomainContext,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#QueryAsync`1(Microsoft.Restier.Core.IDomain,System.Linq.IQueryable`1<!!0>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#QueryAsync`2(Microsoft.Restier.Core.IDomain,System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#QueryAsync(Microsoft.Restier.Core.IDomain,Microsoft.Restier.Core.Query.QueryRequest,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#QueryAsync(Microsoft.Restier.Core.DomainContext,Microsoft.Restier.Core.Query.QueryRequest,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#SubmitAsync(Microsoft.Restier.Core.IDomain,Microsoft.Restier.Core.Submit.ChangeSet,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#SubmitAsync(Microsoft.Restier.Core.DomainContext,Microsoft.Restier.Core.Submit.ChangeSet,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#.ctor(System.Linq.IQueryable,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryResult.#.ctor(System.Collections.IEnumerable,System.Nullable`1<System.Int64>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Security.DomainPermission.#CreateGrant(System.String,System.String,System.String,System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Security.DomainPermission.#CreateDeny(System.String,System.String,System.String,System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapODataDomainRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,System.Func`1<Microsoft.Restier.Core.IDomain>,Microsoft.Restier.WebApi.Batch.ODataDomainBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapODataDomainRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,Microsoft.Restier.WebApi.Batch.ODataDomainBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.Batch.ODataDomainBatchHandler.#.ctor(System.Web.Http.HttpServer,System.Func`1<Microsoft.Restier.Core.DomainContext>)")]
#endregion

#region CA1006 Nested Generic Type
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#QueryAsync`1(Microsoft.Restier.Core.IDomain,System.Linq.IQueryable`1<!!0>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#QueryAsync`2(Microsoft.Restier.Core.IDomain,System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
#endregion

#region CA1033 Explicit interface implementation
[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Microsoft.Restier.Core.DomainBase.#Microsoft.Restier.Core.IExpandableDomain.Configuration")]
[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Microsoft.Restier.Core.DomainBase.#Microsoft.Restier.Core.IExpandableDomain.IsInitialized")]
[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Microsoft.Restier.Core.DomainBase.#Microsoft.Restier.Core.IDomain.Context")]
[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Microsoft.Restier.Core.DomainBase.#Microsoft.Restier.Core.IExpandableDomain.Initialize(Microsoft.Restier.Core.DomainConfiguration)")]
#endregion

#region CA1704 Identifiers spelling
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sourcer", Scope = "type", Target = "Microsoft.Restier.Core.Query.IQueryExpressionSourcer")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sourcer", Scope = "type", Target = "Microsoft.Restier.EntityFramework.Query.QueryExpressionSourcer")]
#endregion

#endregion

#region Temporary Exclusions

[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Net.Http.HttpRequestMessageExtensions.CreateErrorResponse(System.Net.Http.HttpRequestMessage,System.Net.HttpStatusCode,System.String)", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]

#region CA1811 Review uncalled private code
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeExtensions.#GetQualifiedMethod(System.Type,System.String)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Submit.DataModificationEntry.#ServerValues")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableSelectGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableSelectManyGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#Select(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#SelectMany(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#GetEnumerableItemType(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Ensure.#NotNull`1(System.Nullable`1<!!0>,System.String)")]
#endregion

#region CA2208 Add string message for exception
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#Source`1(Microsoft.Restier.Core.DomainContext,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.Domain.#Source`1(Microsoft.Restier.Core.DomainContext,System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.DomainBase.#Microsoft.Restier.Core.IExpandableDomain.Initialize(Microsoft.Restier.Core.DomainConfiguration)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.DomainConfiguration.#SetHookPoint(System.Type,System.Object)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.DomainConfiguration.#AddHookPoint(System.Type,System.Object)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.DomainContext.#.ctor(Microsoft.Restier.Core.DomainConfiguration)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.QueryableSource.#System.Linq.IQueryProvider.CreateQuery`1(System.Linq.Expressions.Expression)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.QueryableSource.#System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)")]
#endregion

#region CA1801 Unused Parameters
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Source`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Source`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Source`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Source`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Source`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Results`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Results`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Result`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Result`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "source", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Value`1(System.Object,System.String)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "propertyName", Scope = "member", Target = "Microsoft.Restier.Core.DomainData.#Value`1(System.Object,System.String)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "odataProperties", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController`1.#GetSource(System.Web.OData.Routing.ODataPath,Microsoft.OData.Edm.IEdmEntityType&)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController`1.#CreateQueryResponse(System.Linq.IQueryable,Microsoft.OData.Edm.IEdmType)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.WebApi")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController.#CreateQueryResponse(System.Linq.IQueryable,Microsoft.OData.Edm.IEdmType)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]
[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Net.Http.HttpRequestMessageExtensions.CreateErrorResponse(System.Net.Http.HttpRequestMessage,System.Net.HttpStatusCode,System.String)", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController.#GetSource(System.Web.OData.Routing.ODataPath,Microsoft.OData.Edm.IEdmEntityType&)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController.#GetQuery()")]
[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Net.Http.HttpRequestMessageExtensions.CreateErrorResponse(System.Net.Http.HttpRequestMessage,System.Net.HttpStatusCode,System.String)", Scope = "member", Target = "Microsoft.Restier.WebApi.ODataDomainController.#GetQuery()")]
#endregion

#endregion

