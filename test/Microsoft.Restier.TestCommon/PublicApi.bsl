public abstract class Microsoft.Restier.Core.ApiBase : IDisposable {
	protected ApiBase (System.IServiceProvider serviceProvider)

	bool IsDisposed  { [CompilerGeneratedAttribute(),]public get; }
	System.IServiceProvider ServiceProvider  { public get; }

	[
	CLSCompliantAttribute(),
	]
	public static void AddPublisherServices (System.Type apiType, System.Action`1[[Microsoft.Extensions.DependencyInjection.IServiceCollection]] configurationCallback)

	[
	CLSCompliantAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureApi (System.Type apiType, Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	public virtual void Dispose ()
	[
	CLSCompliantAttribute(),
	]
	public static System.Action`1[[Microsoft.Extensions.DependencyInjection.IServiceCollection]] GetPublisherServiceCallback (System.Type apiType)
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Core.ApiBaseExtensions {
	[
	ExtensionAttribute(),
	]
	public static T GetApiService (Microsoft.Restier.Core.ApiBase api)

	[
	ExtensionAttribute(),
	]
	public static IEnumerable`1 GetApiServices (Microsoft.Restier.Core.ApiBase api)

	[
	AsyncStateMachineAttribute(),
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.OData.Edm.IEdmModel]] GetModelAsync (Microsoft.Restier.Core.ApiBase api, params System.Threading.CancellationToken cancellationToken)

	[
	ExtensionAttribute(),
	]
	public static object GetProperty (Microsoft.Restier.Core.ApiBase api, string name)

	[
	ExtensionAttribute(),
	]
	public static T GetProperty (Microsoft.Restier.Core.ApiBase api, string name)

	[
	ExtensionAttribute(),
	]
	public static System.Linq.IQueryable GetQueryableSource (Microsoft.Restier.Core.ApiBase api, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static IQueryable`1 GetQueryableSource (Microsoft.Restier.Core.ApiBase api, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static System.Linq.IQueryable GetQueryableSource (Microsoft.Restier.Core.ApiBase api, string namespaceName, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static IQueryable`1 GetQueryableSource (Microsoft.Restier.Core.ApiBase api, string namespaceName, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static bool HasProperty (Microsoft.Restier.Core.ApiBase api, string name)

	[
	AsyncStateMachineAttribute(),
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] QueryAsync (Microsoft.Restier.Core.ApiBase api, Microsoft.Restier.Core.Query.QueryRequest request, params System.Threading.CancellationToken cancellationToken)

	[
	ExtensionAttribute(),
	]
	public static void RemoveProperty (Microsoft.Restier.Core.ApiBase api, string name)

	[
	ExtensionAttribute(),
	]
	public static void SetProperty (Microsoft.Restier.Core.ApiBase api, string name, object value)

	[
	AsyncStateMachineAttribute(),
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Submit.SubmitResult]] SubmitAsync (Microsoft.Restier.Core.ApiBase api, params Microsoft.Restier.Core.Submit.ChangeSet changeSet, params System.Threading.CancellationToken cancellationToken)
}

public sealed class Microsoft.Restier.Core.DataSourceStub {
	public static TResult GetPropertyValue (object source, string propertyName)
	public static IQueryable`1 GetQueryableSource (string name, object[] arguments)
	public static IQueryable`1 GetQueryableSource (string namespaceName, string name, object[] arguments)
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Core.InvocationContextExtensions {
	[
	ExtensionAttribute(),
	]
	public static T GetApiService (Microsoft.Restier.Core.InvocationContext context)

	[
	ExtensionAttribute(),
	]
	public static IEnumerable`1 GetApiServices (Microsoft.Restier.Core.InvocationContext context)
}

[
CLSCompliantAttribute(),
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Core.ServiceCollectionExtensions {
	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddConventionBasedServices (Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type apiType)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddCoreServices (Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type apiType)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddService (Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddService (Microsoft.Extensions.DependencyInjection.IServiceCollection services, Func`3 factory)

	[
	ExtensionAttribute(),
	]
	public static bool HasService (Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection MakeScoped (Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection MakeSingleton (Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection MakeTransient (Microsoft.Extensions.DependencyInjection.IServiceCollection services)
}

public class Microsoft.Restier.Core.InvocationContext {
	public InvocationContext (System.IServiceProvider provider)

	System.IServiceProvider ServiceProvider  { [CompilerGeneratedAttribute(),]public get; }
}

[
SerializableAttribute(),
]
public class Microsoft.Restier.Core.PreconditionFailedException : System.Exception, _Exception, ISerializable {
	public PreconditionFailedException ()
	public PreconditionFailedException (string message)
	public PreconditionFailedException (string message, System.Exception innerException)
}

[
SerializableAttribute(),
]
public class Microsoft.Restier.Core.PreconditionRequiredException : System.Exception, _Exception, ISerializable {
	public PreconditionRequiredException ()
	public PreconditionRequiredException (string message)
	public PreconditionRequiredException (string message, System.Exception innerException)
}

[
SerializableAttribute(),
]
public class Microsoft.Restier.Core.ResourceNotFoundException : System.Exception, _Exception, ISerializable {
	public ResourceNotFoundException ()
	public ResourceNotFoundException (string message)
	public ResourceNotFoundException (string message, System.Exception innerException)
}

public class Microsoft.Restier.Core.RestierContainerBuilder : IContainerBuilder {
	public RestierContainerBuilder (System.Type apiType)

	public virtual Microsoft.OData.IContainerBuilder AddService (Microsoft.OData.ServiceLifetime lifetime, System.Type serviceType, System.Func`2[[System.IServiceProvider],[System.Object]] implementationFactory)
	public virtual Microsoft.OData.IContainerBuilder AddService (Microsoft.OData.ServiceLifetime lifetime, System.Type serviceType, System.Type implementationType)
	public virtual System.IServiceProvider BuildContainer ()
}

public interface Microsoft.Restier.Core.Model.IModelBuilder {
	System.Threading.Tasks.Task`1[[Microsoft.OData.Edm.IEdmModel]] GetModelAsync (Microsoft.Restier.Core.Model.ModelContext context, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Model.IModelMapper {
	bool TryGetRelevantType (Microsoft.Restier.Core.Model.ModelContext context, string name, out System.Type& relevantType)
	bool TryGetRelevantType (Microsoft.Restier.Core.Model.ModelContext context, string namespaceName, string name, out System.Type& relevantType)
}

public class Microsoft.Restier.Core.Model.ModelContext : Microsoft.Restier.Core.InvocationContext {
	public ModelContext (System.IServiceProvider provider)

	System.Collections.Generic.IDictionary`2[[System.String],[System.Type]] ResourceSetTypeMap  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	System.Collections.Generic.IDictionary`2[[System.Type],[System.Collections.Generic.ICollection`1[[System.Reflection.PropertyInfo]]]] ResourceTypeKeyPropertiesMap  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
}

public interface Microsoft.Restier.Core.Operation.IOperationAuthorizer {
	System.Threading.Tasks.Task`1[[System.Boolean]] AuthorizeAsync (Microsoft.Restier.Core.Operation.OperationContext context, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Operation.IOperationExecutor {
	System.Threading.Tasks.Task`1[[System.Linq.IQueryable]] ExecuteOperationAsync (Microsoft.Restier.Core.Operation.OperationContext context, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Operation.IOperationFilter {
	System.Threading.Tasks.Task OnOperationExecutedAsync (Microsoft.Restier.Core.Operation.OperationContext context, System.Threading.CancellationToken cancellationToken)
	System.Threading.Tasks.Task OnOperationExecutingAsync (Microsoft.Restier.Core.Operation.OperationContext context, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.Core.Operation.OperationContext : Microsoft.Restier.Core.InvocationContext {
	public OperationContext (System.Func`2[[System.String],[System.Object]] getParameterValueFunc, string operationName, object implementInstance, bool isFunction, System.Collections.IEnumerable bindingParameterValue, System.IServiceProvider provider)

	System.Collections.IEnumerable BindingParameterValue  { public get; }
	System.Func`2[[System.String],[System.Object]] GetParameterValueFunc  { public get; }
	object ImplementInstance  { public get; }
	bool IsFunction  { public get; }
	string OperationName  { public get; }
	System.Collections.Generic.ICollection`1[[System.Object]] ParameterValues  { public get; public set; }
	System.Net.Http.HttpRequestMessage Request  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
}

public interface Microsoft.Restier.Core.Query.IQueryExecutor {
	System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] ExecuteExpressionAsync (Microsoft.Restier.Core.Query.QueryContext context, System.Linq.IQueryProvider queryProvider, System.Linq.Expressions.Expression expression, System.Threading.CancellationToken cancellationToken)
	System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] ExecuteQueryAsync (Microsoft.Restier.Core.Query.QueryContext context, IQueryable`1 query, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionAuthorizer {
	bool Authorize (Microsoft.Restier.Core.Query.QueryExpressionContext context)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionExpander {
	System.Linq.Expressions.Expression Expand (Microsoft.Restier.Core.Query.QueryExpressionContext context)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionProcessor {
	System.Linq.Expressions.Expression Process (Microsoft.Restier.Core.Query.QueryExpressionContext context)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionSourcer {
	System.Linq.Expressions.Expression ReplaceQueryableSource (Microsoft.Restier.Core.Query.QueryExpressionContext context, bool embedded)
}

public class Microsoft.Restier.Core.Query.DataSourceStubModelReference : Microsoft.Restier.Core.Query.QueryModelReference {
	Microsoft.OData.Edm.IEdmElement Element  { public get; }
	Microsoft.OData.Edm.IEdmEntitySet EntitySet  { public virtual get; }
	Microsoft.OData.Edm.IEdmType Type  { public virtual get; }
}

public class Microsoft.Restier.Core.Query.ParameterModelReference : Microsoft.Restier.Core.Query.QueryModelReference {
}

public class Microsoft.Restier.Core.Query.PropertyModelReference : Microsoft.Restier.Core.Query.QueryModelReference {
	Microsoft.OData.Edm.IEdmEntitySet EntitySet  { public virtual get; }
	Microsoft.OData.Edm.IEdmProperty Property  { public get; }
	Microsoft.Restier.Core.Query.QueryModelReference Source  { [CompilerGeneratedAttribute(),]public get; }
	Microsoft.OData.Edm.IEdmType Type  { public virtual get; }
}

public class Microsoft.Restier.Core.Query.QueryContext : Microsoft.Restier.Core.InvocationContext {
	public QueryContext (System.IServiceProvider serviceProvider, Microsoft.Restier.Core.Query.QueryRequest request)

	Microsoft.OData.Edm.IEdmModel Model  { [CompilerGeneratedAttribute(),]public get; }
	Microsoft.Restier.Core.Query.QueryRequest Request  { [CompilerGeneratedAttribute(),]public get; }
}

public class Microsoft.Restier.Core.Query.QueryExpressionContext {
	public QueryExpressionContext (Microsoft.Restier.Core.Query.QueryContext queryContext)

	System.Action AfterNestedVisitCallback  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	Microsoft.Restier.Core.Query.QueryModelReference ModelReference  { public get; }
	Microsoft.Restier.Core.Query.QueryContext QueryContext  { [CompilerGeneratedAttribute(),]public get; }
	System.Linq.Expressions.Expression VisitedNode  { public get; }

	public Microsoft.Restier.Core.Query.QueryModelReference GetModelReferenceForNode (System.Linq.Expressions.Expression node)
	public void PopVisitedNode ()
	public void PushVisitedNode (System.Linq.Expressions.Expression visitedNode)
	public void ReplaceVisitedNode (System.Linq.Expressions.Expression visitedNode)
}

public class Microsoft.Restier.Core.Query.QueryModelReference {
	Microsoft.OData.Edm.IEdmEntitySet EntitySet  { public virtual get; }
	Microsoft.OData.Edm.IEdmType Type  { public virtual get; }
}

public class Microsoft.Restier.Core.Query.QueryRequest {
	public QueryRequest (System.Linq.IQueryable query)

	System.Linq.Expressions.Expression Expression  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	bool ShouldReturnCount  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
}

public class Microsoft.Restier.Core.Query.QueryResult {
	public QueryResult (System.Collections.IEnumerable results)
	public QueryResult (System.Exception exception)

	System.Exception Exception  { public get; public set; }
	System.Collections.IEnumerable Results  { public get; public set; }
	Microsoft.OData.Edm.IEdmEntitySet ResultsSource  { public get; public set; }
}

public enum Microsoft.Restier.Core.Submit.DataModificationItemAction : int {
	Insert = 2
	Remove = 3
	Undefined = 0
	Update = 1
}

public interface Microsoft.Restier.Core.Submit.IChangeSetInitializer {
	System.Threading.Tasks.Task InitializeAsync (Microsoft.Restier.Core.Submit.SubmitContext context, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.IChangeSetItemAuthorizer {
	System.Threading.Tasks.Task`1[[System.Boolean]] AuthorizeAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetItem item, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.IChangeSetItemFilter {
	System.Threading.Tasks.Task OnChangeSetItemProcessedAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetItem item, System.Threading.CancellationToken cancellationToken)
	System.Threading.Tasks.Task OnChangeSetItemProcessingAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetItem item, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.IChangeSetItemValidator {
	System.Threading.Tasks.Task ValidateChangeSetItemAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetItem item, System.Collections.ObjectModel.Collection`1[[Microsoft.Restier.Core.Submit.ChangeSetItemValidationResult]] validationResults, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.ISubmitExecutor {
	System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Submit.SubmitResult]] ExecuteSubmitAsync (Microsoft.Restier.Core.Submit.SubmitContext context, System.Threading.CancellationToken cancellationToken)
}

public abstract class Microsoft.Restier.Core.Submit.ChangeSetItem {
	public bool HasChanged ()
}

public class Microsoft.Restier.Core.Submit.ChangeSet {
	public ChangeSet ()
	public ChangeSet (System.Collections.Generic.IEnumerable`1[[Microsoft.Restier.Core.Submit.ChangeSetItem]] entries)

	System.Collections.Generic.IList`1[[Microsoft.Restier.Core.Submit.ChangeSetItem]] Entries  { public get; }
}

public class Microsoft.Restier.Core.Submit.ChangeSetItemValidationResult {
	public ChangeSetItemValidationResult ()

	string Id  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string Message  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string PropertyName  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	System.Diagnostics.Tracing.EventLevel Severity  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	object Target  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }

	public virtual string ToString ()
}

public class Microsoft.Restier.Core.Submit.ChangeSetValidationException : System.Exception, _Exception, ISerializable {
	public ChangeSetValidationException (string message)
	public ChangeSetValidationException (string message, System.Exception innerException)

	System.Collections.Generic.IEnumerable`1[[Microsoft.Restier.Core.Submit.ChangeSetItemValidationResult]] ValidationResults  { public get; public set; }
}

public class Microsoft.Restier.Core.Submit.DataModificationItem : Microsoft.Restier.Core.Submit.ChangeSetItem {
	public DataModificationItem (string resourceSetName, System.Type expectedResourceType, System.Type actualResourceType, Microsoft.Restier.Core.Submit.DataModificationItemAction action, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] resourceKey, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] originalValues, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] localValues)

	System.Type ActualResourceType  { [CompilerGeneratedAttribute(),]public get; }
	Microsoft.Restier.Core.Submit.DataModificationItemAction DataModificationItemAction  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	System.Type ExpectedResourceType  { [CompilerGeneratedAttribute(),]public get; }
	bool IsFullReplaceUpdateRequest  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] LocalValues  { [CompilerGeneratedAttribute(),]public get; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] OriginalValues  { [CompilerGeneratedAttribute(),]public get; }
	object Resource  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] ResourceKey  { [CompilerGeneratedAttribute(),]public get; }
	string ResourceSetName  { [CompilerGeneratedAttribute(),]public get; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] ServerValues  { [CompilerGeneratedAttribute(),]public get; }

	public System.Linq.IQueryable ApplyTo (System.Linq.IQueryable query)
	public object ValidateEtag (System.Linq.IQueryable query)
}

public class Microsoft.Restier.Core.Submit.DataModificationItem`1 : Microsoft.Restier.Core.Submit.DataModificationItem {
	public DataModificationItem`1 (string resourceSetName, System.Type expectedResourceType, System.Type actualResourceType, Microsoft.Restier.Core.Submit.DataModificationItemAction action, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] resourceKey, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] originalValues, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] localValues)

	T Resource  { public get; public set; }
}

public class Microsoft.Restier.Core.Submit.SubmitContext : Microsoft.Restier.Core.InvocationContext {
	public SubmitContext (System.IServiceProvider provider, Microsoft.Restier.Core.Submit.ChangeSet changeSet)

	Microsoft.Restier.Core.Submit.ChangeSet ChangeSet  { public get; public set; }
	Microsoft.Restier.Core.Submit.SubmitResult Result  { public get; public set; }
}

public class Microsoft.Restier.Core.Submit.SubmitResult {
	public SubmitResult (Microsoft.Restier.Core.Submit.ChangeSet completedChangeSet)
	public SubmitResult (System.Exception exception)

	Microsoft.Restier.Core.Submit.ChangeSet CompletedChangeSet  { public get; public set; }
	System.Exception Exception  { public get; public set; }
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Providers.EntityFramework.GeographyConverter {
	[
	ExtensionAttribute(),
	]
	public static DbGeography ToDbGeography (Microsoft.Spatial.GeographyLineString lineString)

	[
	ExtensionAttribute(),
	]
	public static DbGeography ToDbGeography (Microsoft.Spatial.GeographyPoint point)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Spatial.GeographyLineString ToGeographyLineString (DbGeography geography)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Spatial.GeographyPoint ToGeographyPoint (DbGeography geography)
}

[
CLSCompliantAttribute(),
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Providers.EntityFramework.ServiceCollectionExtensions {
	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEfProviderServices (Microsoft.Extensions.DependencyInjection.IServiceCollection services)
}

public class Microsoft.Restier.Providers.EntityFramework.ChangeSetInitializer : IChangeSetInitializer {
	public ChangeSetInitializer ()

	public virtual object ConvertToEfValue (System.Type type, object value)
	[
	AsyncStateMachineAttribute(),
	]
	public virtual System.Threading.Tasks.Task InitializeAsync (Microsoft.Restier.Core.Submit.SubmitContext context, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.Providers.EntityFramework.EntityFrameworkApi`1 : Microsoft.Restier.Core.ApiBase, IDisposable {
	public EntityFrameworkApi`1 (System.IServiceProvider serviceProvider)

	T DbContext  { protected get; }

	[
	CLSCompliantAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureApi (System.Type apiType, Microsoft.Extensions.DependencyInjection.IServiceCollection services)
}

[
EditorBrowsableAttribute(),
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Publishers.OData.HttpConfigurationExtensions {
	[
	ExtensionAttribute(),
	]
	public static bool GetUseVerboseErrors (System.Web.Http.HttpConfiguration configuration)

	[
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[System.Web.OData.Routing.ODataRoute]] MapRestierRoute (System.Web.Http.HttpConfiguration config, string routeName, string routePrefix, params Microsoft.Restier.Publishers.OData.Batch.RestierBatchHandler batchHandler)

	[
	ExtensionAttribute(),
	]
	public static void SetUseVerboseErrors (System.Web.Http.HttpConfiguration configuration, bool useVerboseErrors)
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Publishers.OData.ServiceCollectionExtensions {
	[
	CLSCompliantAttribute(),
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddODataServices (Microsoft.Extensions.DependencyInjection.IServiceCollection services)
}

[
ODataFormattingAttribute(),
RestierExceptionFilterAttribute(),
]
public class Microsoft.Restier.Publishers.OData.RestierController : System.Web.OData.ODataController, IDisposable, IHttpController {
	public RestierController ()

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Web.Http.IHttpActionResult]] Delete (System.Threading.CancellationToken cancellationToken)

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Net.Http.HttpResponseMessage]] Get (System.Threading.CancellationToken cancellationToken)

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Web.Http.IHttpActionResult]] Patch (System.Web.OData.EdmEntityObject edmEntityObject, System.Threading.CancellationToken cancellationToken)

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Web.Http.IHttpActionResult]] Post (System.Web.OData.EdmEntityObject edmEntityObject, System.Threading.CancellationToken cancellationToken)

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Net.Http.HttpResponseMessage]] PostAction (System.Web.OData.ODataActionParameters parameters, System.Threading.CancellationToken cancellationToken)

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Web.Http.IHttpActionResult]] Put (System.Web.OData.EdmEntityObject edmEntityObject, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.Publishers.OData.RestierPayloadValueConverter : Microsoft.OData.ODataPayloadValueConverter {
	public RestierPayloadValueConverter ()

	public virtual object ConvertToPayloadValue (object value, Microsoft.OData.Edm.IEdmTypeReference edmTypeReference)
}

public class Microsoft.Restier.Publishers.OData.Batch.RestierBatchChangeSetRequestItem : System.Web.OData.Batch.ChangeSetRequestItem, IDisposable {
	public RestierBatchChangeSetRequestItem (System.Collections.Generic.IEnumerable`1[[System.Net.Http.HttpRequestMessage]] requests)

	[
	AsyncStateMachineAttribute(),
	]
	public virtual System.Threading.Tasks.Task`1[[System.Web.OData.Batch.ODataBatchResponseItem]] SendRequestAsync (System.Net.Http.HttpMessageInvoker invoker, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.Publishers.OData.Batch.RestierBatchHandler : System.Web.OData.Batch.DefaultODataBatchHandler, IDisposable {
	public RestierBatchHandler (System.Web.Http.HttpServer httpServer)

	protected virtual Microsoft.Restier.Publishers.OData.Batch.RestierBatchChangeSetRequestItem CreateRestierBatchChangeSetRequestItem (System.Collections.Generic.IList`1[[System.Net.Http.HttpRequestMessage]] changeSetRequests)
	[
	AsyncStateMachineAttribute(),
	]
	public virtual System.Threading.Tasks.Task`1[[System.Collections.Generic.IList`1[[System.Web.OData.Batch.ODataBatchRequestItem]]]] ParseBatchRequestsAsync (System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.Publishers.OData.Formatter.DefaultRestierDeserializerProvider : System.Web.OData.Formatter.Deserialization.DefaultODataDeserializerProvider {
	public DefaultRestierDeserializerProvider (System.IServiceProvider rootContainer)

	public virtual System.Web.OData.Formatter.Deserialization.ODataEdmTypeDeserializer GetEdmTypeDeserializer (Microsoft.OData.Edm.IEdmTypeReference edmType)
}

public class Microsoft.Restier.Publishers.OData.Formatter.DefaultRestierSerializerProvider : System.Web.OData.Formatter.Serialization.DefaultODataSerializerProvider {
	public DefaultRestierSerializerProvider (System.IServiceProvider rootContainer)

	public virtual System.Web.OData.Formatter.Serialization.ODataEdmTypeSerializer GetEdmTypeSerializer (Microsoft.OData.Edm.IEdmTypeReference edmType)
	public virtual System.Web.OData.Formatter.Serialization.ODataSerializer GetODataPayloadSerializer (System.Type type, System.Net.Http.HttpRequestMessage request)
}

public class Microsoft.Restier.Publishers.OData.Formatter.RestierCollectionSerializer : System.Web.OData.Formatter.Serialization.ODataCollectionSerializer {
	public RestierCollectionSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.Publishers.OData.Formatter.RestierEnumSerializer : System.Web.OData.Formatter.Serialization.ODataEnumSerializer {
	public RestierEnumSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.Publishers.OData.Formatter.RestierPrimitiveSerializer : System.Web.OData.Formatter.Serialization.ODataPrimitiveSerializer {
	public RestierPrimitiveSerializer ()

	public virtual Microsoft.OData.ODataPrimitiveValue CreateODataPrimitiveValue (object graph, Microsoft.OData.Edm.IEdmPrimitiveTypeReference primitiveType, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.Publishers.OData.Formatter.RestierRawSerializer : System.Web.OData.Formatter.Serialization.ODataRawValueSerializer {
	public RestierRawSerializer ()

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.Publishers.OData.Formatter.RestierResourceSerializer : System.Web.OData.Formatter.Serialization.ODataResourceSerializer {
	public RestierResourceSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.Publishers.OData.Formatter.RestierResourceSetSerializer : System.Web.OData.Formatter.Serialization.ODataResourceSetSerializer {
	public RestierResourceSetSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Publishers.OData.Model.EdmHelpers {
	[
	ExtensionAttribute(),
	]
	public static System.Type GetClrType (Microsoft.OData.Edm.IEdmType edmType, System.IServiceProvider serviceProvider)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.OData.Edm.EdmTypeReference GetPrimitiveTypeReference (System.Type type)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.OData.Edm.IEdmTypeReference GetTypeReference (System.Type type, Microsoft.OData.Edm.IEdmModel model)
}

[
AttributeUsageAttribute(),
]
public sealed class Microsoft.Restier.Publishers.OData.Model.OperationAttribute : System.Attribute, _Attribute {
	public OperationAttribute ()

	string EntitySet  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	bool HasSideEffects  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	bool IsBound  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	bool IsComposable  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string Namespace  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
}

[
AttributeUsageAttribute(),
]
public sealed class Microsoft.Restier.Publishers.OData.Model.ResourceAttribute : System.Attribute, _Attribute {
	public ResourceAttribute ()
}

