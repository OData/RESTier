// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

#region Permanent Exclusions
[assembly: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ValidationException", Justification = "We do not intend to support serialization of this exception yet")]
[assembly: SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Scope = "member", Target = "Microsoft.Restier.WebApi.Batch.RestierChangeSetRequestItem.#DisposeResponses(System.Collections.Generic.IEnumerable`1<System.Net.Http.HttpResponseMessage>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.ApiBase.#Dispose()", Justification = "Need to do some clean up before the virtual Dispose(disposing) method gets called.")]
[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "These assemblies are delay-signed.")]
[assembly: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope = "member", Target = "Microsoft.Restier.EntityFramework.Model.ModelProducer.#ProduceModelAsync(Microsoft.Restier.Core.Model.ModelContext,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ValidationResults")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "Microsoft.Restier.Security.ApiPermission")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Microsoft.Restier.Core.Submit.ChangeSetEntry.#Type")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryModelReference.#Type")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "itemValue", Scope = "member", Target = "Microsoft.Restier.Core.Submit.DataModificationEntry.#ApplyPredicate(System.Linq.Expressions.ParameterExpression,System.Linq.Expressions.Expression,System.Collections.Generic.KeyValuePair`2<System.String,System.Object>)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "Microsoft.Restier.EntityFramework.Model.ModelProducer.#.cctor()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)", Justification = "Instance is disposed elsewhere")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ValidationException", Justification = "We do not intend to support serialization of this exception yet")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly", Justification = "AssemblyInformationalVersion could be string.")]

#region CA1004 Generic method with type parameter
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ApiBuilderExtensions.#ChainPrevious`2(Microsoft.Restier.Core.ApiBuilder)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ApiBuilderExtensions.#CutoffPrevious`2(Microsoft.Restier.Core.ApiBuilder)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ApiBuilderExtensions.#HasService`1(Microsoft.Restier.Core.ApiBuilder)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ApiBuilderExtensions.#MakeSingleton`1(Microsoft.Restier.Core.ApiBuilder)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ApiBuilderExtensions.#MakeScoped`1(Microsoft.Restier.Core.ApiBuilder)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ApiBuilderExtensions.#MakeTransient`1(Microsoft.Restier.Core.ApiBuilder)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.Query.IQueryExecutor.#ExecuteSingleAsync`1(Microsoft.Restier.Core.Query.QueryContext,System.Linq.IQueryable,System.Linq.Expressions.Expression,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,System.Func`1<Microsoft.Restier.Core.IApi>,Microsoft.Restier.WebApi.Batch.RestierBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,Microsoft.Restier.WebApi.Batch.RestierBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#CreateRestierRoutingConventions`1(System.Web.Http.HttpConfiguration,Microsoft.OData.Edm.IEdmModel)")]
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
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.WebApi")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Core.Model")]
#endregion

#region CA1026 Default Parameter
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.Batch.RestierBatchHandler.#.ctor(System.Web.Http.HttpServer,System.Func`1<Microsoft.Restier.Core.IApi>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#GetModelAsync(Microsoft.Restier.Core.IApi,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#GetModelAsync(Microsoft.Restier.Core.ApiContext,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#QueryAsync`1(Microsoft.Restier.Core.IApi,System.Linq.IQueryable`1<!!0>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#QueryAsync`2(Microsoft.Restier.Core.IApi,System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#QueryAsync(Microsoft.Restier.Core.IApi,Microsoft.Restier.Core.Query.QueryRequest,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#QueryAsync(Microsoft.Restier.Core.ApiContext,Microsoft.Restier.Core.Query.QueryRequest,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#SubmitAsync(Microsoft.Restier.Core.IApi,Microsoft.Restier.Core.Submit.ChangeSet,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Api.#SubmitAsync(Microsoft.Restier.Core.ApiContext,Microsoft.Restier.Core.Submit.ChangeSet,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#.ctor(System.Linq.IQueryable,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryResult.#.ctor(System.Collections.IEnumerable,System.Nullable`1<System.Int64>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Security.ApiPermission.#CreateGrant(System.String,System.String,System.String,System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Security.ApiPermission.#CreateDeny(System.String,System.String,System.String,System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,System.Func`1<Microsoft.Restier.Core.IApi>,Microsoft.Restier.WebApi.Batch.RestierBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,Microsoft.Restier.WebApi.Batch.RestierBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.WebApi.Batch.RestierBatchHandler.#.ctor(System.Web.Http.HttpServer,System.Func`1<Microsoft.Restier.Core.ApiContext>)")]
#endregion

#region CA1006 Nested Generic Type
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Api.#QueryAsync`1(Microsoft.Restier.Core.IApi,System.Linq.IQueryable`1<!!0>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Api.#QueryAsync`2(Microsoft.Restier.Core.IApi,System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
#endregion

#region CA1033 Explicit interface implementation
[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Microsoft.Restier.Core.ApiBase.#Microsoft.Restier.Core.IApi.Context")]
#endregion

#region CA1704 Identifiers spelling
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sourcer", Scope = "type", Target = "Microsoft.Restier.Core.Query.IQueryExpressionSourcer")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sourcer", Scope = "type", Target = "Microsoft.Restier.EntityFramework.Query.QueryExpressionSourcer")]
#endregion

#endregion

#region Temporary Exclusions

[assembly: SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Net.Http.HttpRequestMessageExtensions.CreateErrorResponse(System.Net.Http.HttpRequestMessage,System.Net.HttpStatusCode,System.String)", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]

#region CA1811 Review uncalled private code
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeExtensions.#GetQualifiedMethod(System.Type,System.String)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelBuilder.#.ctor(Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelBuilder.#InnerModelBuilder")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelBuilder.#ModelCache")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelMapper.#ModelCache")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelMapper.#.ctor(Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelMapper.#InnerModelMapper")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Submit.DataModificationEntry.#ServerValues")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableSelectGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableSelectManyGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#Select(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#SelectMany(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#GetEnumerableItemType(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Ensure.#NotNull`1(System.Nullable`1<!!0>,System.String)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeExtensions.#TryGetElementType(System.Type,System.Type&)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.WebApi.Filters.ValidationResultDto.#Severity")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.WebApi.Filters.ValidationResultDto.#PropertyName")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.WebApi.Filters.ValidationResultDto.#Message")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.WebApi.Filters.ValidationResultDto.#Id")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableCountGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableWhereGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#Where(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#StripQueryMethod(System.Linq.Expressions.Expression,System.String)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#StripPagingOperators`1(System.Linq.IQueryable`1<!!0>)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#Count(System.Linq.Expressions.Expression,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#GetCountableQuery`1(System.Linq.IQueryable`1<!!0>)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#GetSelectExpandElementType(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.WebApi.Results.BaseResult.#EdmType")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.WebApi.Results.BaseResult.#Context")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#GetUnderlyingTypeOrSelf(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsEnum(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsDateTime(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsTimeSpan(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsDateTimeOffset(System.Type)")]
#endregion

#region CA2208 Add string message for exception
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.Api.#Source`1(Microsoft.Restier.Core.ApiContext,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.Api.#Source`1(Microsoft.Restier.Core.ApiContext,System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.ApiConfiguration.#SetHookPoint(System.Type,System.Object)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.ApiConfiguration.#AddHookPoint(System.Type,System.Object)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.ApiContext.#.ctor(Microsoft.Restier.Core.ApiConfiguration)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.QueryableSource.#System.Linq.IQueryProvider.CreateQuery`1(System.Linq.Expressions.Expression)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.QueryableSource.#System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)")]
#endregion

#region CA1801 Unused Parameters
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Source`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Source`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Source`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Source`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Source`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Results`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Results`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Result`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Result`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "source", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Value`1(System.Object,System.String)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "propertyName", Scope = "member", Target = "Microsoft.Restier.Core.ApiData.#Value`1(System.Object,System.String)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "odataProperties", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]
#endregion

#region CA2000 Dispose objects
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.Core.ApiBuilderExtensions+SharedApiScopeFactory.#CreateApiScope()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController`1.#GetSource(System.Web.OData.Routing.ODataPath,Microsoft.OData.Edm.IEdmEntityType&)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController`1.#CreateQueryResponse(System.Linq.IQueryable,Microsoft.OData.Edm.IEdmType)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController.#CreateQueryResponse(System.Linq.IQueryable,Microsoft.OData.Edm.IEdmType)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController.#GetSource(System.Web.OData.Routing.ODataPath,Microsoft.OData.Edm.IEdmEntityType&)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.RestierController.#GetQuery()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.Filters.RestierExceptionFilterAttribute.#Handler403(System.Web.Http.Filters.HttpActionExecutedContext,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.WebApi.Filters.RestierExceptionFilterAttribute.#Handler404(System.Web.Http.Filters.HttpActionExecutedContext,System.Threading.CancellationToken)")]
#endregion

#region CA1800 Unnecessary casts
[assembly: SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Scope = "member", Target = "Microsoft.Restier.WebApi.Routing.RestierRoutingConvention.#HasControllerForEntitySetOrSingleton(System.Web.OData.Routing.ODataPath,System.Net.Http.HttpRequestMessage)")]
#endregion

#region CA1812 Uninstantiated internal classes
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedChangeSetEntryValidator")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+QueryExpressionExpander")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+QueryExpressionSourcer")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelBuilder")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.Conventions.ConventionBasedApiModelBuilder+ModelMapper")]
#endregion

#endregion

