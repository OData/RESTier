// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

#region Permanent Exclusions
[assembly: SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.EntityFrameworkApi`1.#ConfigureApi(System.Type,Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ChangeSetValidationException", Justification = "We do not intend to support serialization of this exception yet")]
[assembly: SuppressMessage("Microsoft.Design", "CA1061:DoNotHideBaseClassMethods", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Batch.RestierBatchChangeSetRequestItem.#DisposeResponses(System.Collections.Generic.IEnumerable`1<System.Net.Http.HttpResponseMessage>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.ApiBase.#Dispose()", Justification = "Need to do some clean up before the virtual Dispose(disposing) method gets called.")]
[assembly: SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "These assemblies are delay-signed.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ChangeSetValidationResults")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Scope = "type", Target = "Microsoft.Restier.Security.ApiPermission")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Microsoft.Restier.Core.Submit.ChangeSetEntry.#Type")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryModelReference.#Type")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ModelProducer.#.cctor()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.RestierController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)", Justification = "Instance is disposed elsewhere")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope = "type", Target = "Microsoft.Restier.Core.Submit.ChangeSetValidationException", Justification = "We do not intend to support serialization of this exception yet")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly", Justification = "AssemblyInformationalVersion could be string.")]

#region CA1004 Generic method with type parameter
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.Query.IQueryExecutor.#ExecuteExpressionAsync`1(Microsoft.Restier.Core.Query.QueryContext,System.Linq.IQueryProvider,System.Linq.Expressions.Expression,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ServiceCollectionExtensions.#HasService`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ServiceCollectionExtensions.#AddService`2(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ServiceCollectionExtensions.#MakeSingleton`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ServiceCollectionExtensions.#MakeScoped`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Core.ServiceCollectionExtensions.#MakeTransient`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ServiceCollectionExtensions.#AddEfProviderServices`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.ServiceCollectionExtensions.#AddODataServices`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,System.Func`1<Microsoft.Restier.Core.ApiBase>,Microsoft.Restier.Publishers.OData.Batch.RestierBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,Microsoft.Restier.Publishers.OData.Batch.RestierBatchHandler)")]
#endregion

#region CA1006 Nested Generic Type
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.ApiBaseExtensions.#QueryAsync`1(Microsoft.Restier.Core.ApiBase,System.Linq.IQueryable`1<!!0>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.ApiBaseExtensions.#QueryAsync`2(Microsoft.Restier.Core.ApiBase,System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Model.ModelContext.#ResourceTypeKeyPropertiesMap")]
[assembly: SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
#endregion

#region CA1020 Few types in namespace
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Core")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Core.Model")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Core.Operation")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Providers.EntityFramework")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Publishers.OData")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Publishers.OData.Batch")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Publishers.OData.Filters")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Publishers.OData.Model")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Restier.Publishers.OData.Results")]
#endregion

#region CA1026 Default Parameter
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Batch.RestierBatchHandler.#.ctor(System.Web.Http.HttpServer,System.Func`1<Microsoft.Restier.Core.ApiBase>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.ApiBaseExtensions.#GetModelAsync(Microsoft.Restier.Core.ApiBase,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.ApiBaseExtensions.#QueryAsync`1(Microsoft.Restier.Core.ApiBase,System.Linq.IQueryable`1<!!0>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.ApiBaseExtensions.#QueryAsync`2(Microsoft.Restier.Core.ApiBase,System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.ApiBaseExtensions.#QueryAsync(Microsoft.Restier.Core.ApiBase,Microsoft.Restier.Core.Query.QueryRequest,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.ApiBaseExtensions.#SubmitAsync(Microsoft.Restier.Core.ApiBase,Microsoft.Restier.Core.Submit.ChangeSet,System.Threading.CancellationToken)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#.ctor(System.Linq.IQueryable,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create`2(System.Linq.IQueryable`1<!!0>,System.Linq.Expressions.Expression`1<System.Func`2<System.Linq.IQueryable`1<!!0>,!!1>>,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryRequest.#Create(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Nullable`1<System.Boolean>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Core.Query.QueryResult.#.ctor(System.Collections.IEnumerable,System.Nullable`1<System.Int64>)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Security.ApiPermission.#CreateGrant(System.String,System.String,System.String,System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Security.ApiPermission.#CreateDeny(System.String,System.String,System.String,System.String,System.String)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,System.Func`1<Microsoft.Restier.Core.ApiBase>,Microsoft.Restier.Publishers.OData.Batch.RestierBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.HttpConfigurationExtensions.#MapRestierRoute`1(System.Web.Http.HttpConfiguration,System.String,System.String,Microsoft.Restier.Publishers.OData.Batch.RestierBatchHandler)")]
[assembly: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "Microsoft.Restier.Core.PreconditionFailedException")]
[assembly: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "Microsoft.Restier.Core.ResourceNotFoundException")]
[assembly: SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "Microsoft.Restier.Core.PreconditionRequiredException")]
#endregion

#region CA1704 Identifiers spelling
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sourcer", Scope = "type", Target = "Microsoft.Restier.Core.Query.IQueryExpressionSourcer")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sourcer", Scope = "type", Target = "Microsoft.Restier.Providers.EntityFramework.QueryExpressionSourcer")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ef", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ServiceCollectionExtensions.#AddEfProviderServices`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ef", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ChangeSetInitializer.#ConvertToEfValue(System.Type,System.Object)")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etag", Scope = "member", Target = "Microsoft.Restier.Core.Submit.DataModificationItem.#ValidateEtag(System.Linq.IQueryable)")]
#endregion

#region CA1709 Identifiers case
[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ef", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ServiceCollectionExtensions.#AddEfProviderServices`1(Microsoft.Extensions.DependencyInjection.IServiceCollection)")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Ef", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ChangeSetInitializer.#ConvertToEfValue(System.Type,System.Object)")]
#endregion

#endregion

#region Temporary Exclusions

#region CA1811 Review uncalled private code
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Core.Submit.DataModificationItem.#ServerValues")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ModelProducer.#InnerModelBuilder")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.QueryExecutor.#Inner")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Query.RestierQueryExecutor.#Inner")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.ValidationResultDto.#Severity")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.ValidationResultDto.#PropertyName")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.ValidationResultDto.#Message")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.ValidationResultDto.#Id")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.ModelMapper.#InnerMapper")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelBuilder.#InnerModelBuilder")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+ModelBuilder.#.ctor(Microsoft.Restier.Publishers.OData.Model.RestierModelExtender)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+ModelBuilder.#InnerModelBuilder")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+ModelBuilder.#ModelCache")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+ModelMapper.#.ctor(Microsoft.Restier.Publishers.OData.Model.RestierModelExtender)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+ModelMapper.#ModelCache")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+ModelMapper.#InnerModelMapper")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.BaseResult.#EdmType")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.PropertyAttributes.#NoWritePermission")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.PropertyAttributes.#NoReadPermission")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.PropertyAttributes.#NoReadPermission")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.PropertyAttributes.#NoWritePermission")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Ensure.#NotNull`1(System.Nullable`1<!!0>,System.String)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#EnumerableCastGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#EnumerableToListGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#EnumerableToArrayGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableAsQueryable")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableAsQueryableGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableCountGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableOfTypeGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableSelectGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableSelectManyGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelperMethods.#QueryableWhereGeneric")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#Count(System.Linq.Expressions.Expression,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#CreateEmptyQueryable(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#GetCountableQuery`1(System.Linq.IQueryable`1<!!0>)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#GetEnumerableItemType(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#GetSelectExpandElementType(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#OfType(System.Linq.IQueryable,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#RemoveUnneededStatement(System.Linq.Expressions.MethodCallExpression)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#RemoveSelectExpandStatement(System.Linq.Expressions.MethodCallExpression)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#RemoveAppendWhereStatement(System.Linq.Expressions.Expression)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#Select(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#SelectMany(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#StripQueryMethod(System.Linq.Expressions.Expression,System.String)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#StripPagingOperators`1(System.Linq.IQueryable`1<!!0>)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.Linq.Expressions.ExpressionHelpers.#Where(System.Linq.IQueryable,System.Linq.Expressions.LambdaExpression,System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeExtensions.#GetQualifiedMethod(System.Type,System.String)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeExtensions.#TryGetElementType(System.Type,System.Type&)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#GetUnderlyingTypeOrSelf(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsDateTime(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsDateTimeOffset(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsEnum(System.Type)")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "System.TypeHelper.#IsTimeSpan(System.Type)")]
#endregion

#region CA2208 Add string message for exception
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.QueryableSource.#System.Linq.IQueryProvider.CreateQuery`1(System.Linq.Expressions.Expression)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Scope = "member", Target = "Microsoft.Restier.Core.QueryableSource.#System.Linq.IQueryProvider.CreateQuery(System.Linq.Expressions.Expression)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Restier.Core.Model.ModelContext.#ResourceSetTypeMap")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Restier.Core.Model.ModelContext.#ResourceTypeKeyPropertiesMap")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Scope = "member", Target = "Microsoft.Restier.Core.Operation.OperationContext.#ParameterValues")]
#endregion

#region CA1801 Unused Parameters
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#GetQueryableSource`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#GetQueryableSource`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#GetQueryableSource`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#GetQueryableSource`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#GetQueryableSource`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Results`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Results`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Result`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Result`1(System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Results`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "namespaceName", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "name", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "arguments", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#Result`1(System.String,System.String,System.Object[])")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "source", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#GetPropertyValue`1(System.Object,System.String)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "propertyName", Scope = "member", Target = "Microsoft.Restier.Core.DataSourceStub.#GetPropertyValue`1(System.Object,System.String)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "odataProperties", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.RestierController`1.#GetQuery(System.Web.OData.Extensions.HttpRequestMessageProperties)")]
[assembly: SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes", Scope = "member", Target = "Microsoft.Restier.Providers.EntityFramework.ModelProducer.#GetModelAsync(Microsoft.Restier.Core.Model.ModelContext,System.Threading.CancellationToken)")]
#endregion

#region CA2000 Dispose objects
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.RestierController.#CreateQueryResponse(System.Linq.IQueryable,Microsoft.OData.Edm.IEdmType,System.Web.OData.Formatter.ETag)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Scope = "member", Target = "Microsoft.Restier.Publishers.OData.RestierExceptionFilterAttribute.#HandleCommonException(System.Web.Http.Filters.HttpActionExecutedContext,System.Boolean,System.Threading.CancellationToken)")]
#endregion

#region CA1812 Uninstantiated internal classes
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.ApiConfiguration")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.ConventionBasedChangeSetItemValidator")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Core.PropertyBag")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Providers.EntityFramework.ModelProducer")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Providers.EntityFramework.QueryExpressionProcessor")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+QueryExpressionExpander")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Publishers.OData.Model.RestierModelExtender+QueryExpressionSourcer")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Restier.Publishers.OData.Query.RestierQueryExecutorOptions")]
#endregion

#endregion

