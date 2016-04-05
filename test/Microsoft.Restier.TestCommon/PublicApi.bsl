public abstract class Microsoft.Restier.Core.ApiBase : IDisposable {
	protected ApiBase ()

	Microsoft.Restier.Core.ApiConfiguration Configuration  { protected get; }
	Microsoft.Restier.Core.ApiContext Context  { public get; }
	bool IsDisposed  { [CompilerGeneratedAttribute(),]protected get; }

	[
	CLSCompliantAttribute(),
	]
	protected virtual Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureApi (Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	[
	CLSCompliantAttribute(),
	]
	protected virtual Microsoft.Restier.Core.ApiConfiguration CreateApiConfiguration (Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	protected virtual Microsoft.Restier.Core.ApiContext CreateApiContext (Microsoft.Restier.Core.ApiConfiguration configuration)
	public virtual void Dispose ()
	protected virtual void Dispose (bool disposing)
	protected virtual void Finalize ()
}

[
AttributeUsageAttribute(),
SerializableAttribute(),
]
public abstract class Microsoft.Restier.Core.ApiConfiguratorAttribute : System.Attribute, _Attribute {
	protected ApiConfiguratorAttribute ()

	[
	CLSCompliantAttribute(),
	]
	public static void ApplyApiServices (System.Type type, Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	public static void ApplyConfiguration (System.Type type, Microsoft.Restier.Core.ApiConfiguration configuration)
	public static void ApplyDisposal (System.Type type, object instance, Microsoft.Restier.Core.ApiContext context)
	public static void ApplyInitialization (System.Type type, object instance, Microsoft.Restier.Core.ApiContext context)
	public virtual void Configure (Microsoft.Restier.Core.ApiConfiguration configuration, System.Type type)
	[
	CLSCompliantAttribute(),
	]
	public virtual void ConfigureApi (Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type type)

	public virtual void Dispose (Microsoft.Restier.Core.ApiContext context, System.Type type, object instance)
	public virtual void Initialize (Microsoft.Restier.Core.ApiContext context, System.Type type, object instance)
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Core.ApiBaseExtensions {
	[
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.OData.Edm.IEdmModel]] GetModelAsync (Microsoft.Restier.Core.ApiBase api, params System.Threading.CancellationToken cancellationToken)

	[
	AsyncStateMachineAttribute(),
	ExtensionAttribute(),
	]
	public static Task`1 QueryAsync (Microsoft.Restier.Core.ApiBase api, IQueryable`1 query, params System.Threading.CancellationToken cancellationToken)

	[
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] QueryAsync (Microsoft.Restier.Core.ApiBase api, Microsoft.Restier.Core.Query.QueryRequest request, params System.Threading.CancellationToken cancellationToken)

	[
	ExtensionAttribute(),
	]
	public static System.Linq.IQueryable Source (Microsoft.Restier.Core.ApiBase api, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static IQueryable`1 Source (Microsoft.Restier.Core.ApiBase api, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static System.Linq.IQueryable Source (Microsoft.Restier.Core.ApiBase api, string namespaceName, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static IQueryable`1 Source (Microsoft.Restier.Core.ApiBase api, string namespaceName, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Submit.SubmitResult]] SubmitAsync (Microsoft.Restier.Core.ApiBase api, params Microsoft.Restier.Core.Submit.ChangeSet changeSet, params System.Threading.CancellationToken cancellationToken)
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Core.ApiConfigurationExtensions {
	[
	ExtensionAttribute(),
	]
	public static void ClearProperty (Microsoft.Restier.Core.ApiConfiguration configuration, string name)

	[
	ExtensionAttribute(),
	]
	public static T GetApiService (Microsoft.Restier.Core.ApiConfiguration configuration)

	[
	ExtensionAttribute(),
	]
	public static object GetProperty (Microsoft.Restier.Core.ApiConfiguration configuration, string name)

	[
	ExtensionAttribute(),
	]
	public static T GetProperty (Microsoft.Restier.Core.ApiConfiguration configuration, string name)

	[
	ExtensionAttribute(),
	]
	public static bool HasProperty (Microsoft.Restier.Core.ApiConfiguration configuration, string name)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Restier.Core.ApiConfiguration IgnoreProperty (Microsoft.Restier.Core.ApiConfiguration configuration, string propertyName)

	[
	ExtensionAttribute(),
	]
	public static void SetProperty (Microsoft.Restier.Core.ApiConfiguration configuration, string name, object value)
}

[
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.Core.ApiContextExtensions {
	[
	ExtensionAttribute(),
	]
	public static void ClearProperty (Microsoft.Restier.Core.ApiContext context, string name)

	[
	ExtensionAttribute(),
	]
	public static T GetApiService (Microsoft.Restier.Core.ApiContext context)

	[
	ExtensionAttribute(),
	]
	public static IEnumerable`1 GetApiServices (Microsoft.Restier.Core.ApiContext context)

	[
	AsyncStateMachineAttribute(),
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.OData.Edm.IEdmModel]] GetModelAsync (Microsoft.Restier.Core.ApiContext context, params System.Threading.CancellationToken cancellationToken)

	[
	ExtensionAttribute(),
	]
	public static object GetProperty (Microsoft.Restier.Core.ApiContext context, string name)

	[
	ExtensionAttribute(),
	]
	public static T GetProperty (Microsoft.Restier.Core.ApiContext context, string name)

	[
	ExtensionAttribute(),
	]
	public static bool HasProperty (Microsoft.Restier.Core.ApiContext context, string name)

	[
	AsyncStateMachineAttribute(),
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] QueryAsync (Microsoft.Restier.Core.ApiContext context, Microsoft.Restier.Core.Query.QueryRequest request, params System.Threading.CancellationToken cancellationToken)

	[
	ExtensionAttribute(),
	]
	public static void SetProperty (Microsoft.Restier.Core.ApiContext context, string name, object value)

	[
	ExtensionAttribute(),
	]
	public static System.Linq.IQueryable Source (Microsoft.Restier.Core.ApiContext context, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static IQueryable`1 Source (Microsoft.Restier.Core.ApiContext context, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static System.Linq.IQueryable Source (Microsoft.Restier.Core.ApiContext context, string namespaceName, string name, object[] arguments)

	[
	ExtensionAttribute(),
	]
	public static IQueryable`1 Source (Microsoft.Restier.Core.ApiContext context, string namespaceName, string name, object[] arguments)

	[
	AsyncStateMachineAttribute(),
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Submit.SubmitResult]] SubmitAsync (Microsoft.Restier.Core.ApiContext context, params Microsoft.Restier.Core.Submit.ChangeSet changeSet, params System.Threading.CancellationToken cancellationToken)
}

public sealed class Microsoft.Restier.Core.DataSourceStubs {
	public static TResult Result (string name, object[] arguments)
	public static TResult Result (string namespaceName, string name, object[] arguments)
	public static IEnumerable`1 Results (string name, object[] arguments)
	public static IEnumerable`1 Results (string namespaceName, string name, object[] arguments)
	public static IQueryable`1 Source (string name, object[] arguments)
	public static IQueryable`1 Source (string namespaceName, string name, object[] arguments)
	public static TResult Value (object source, string propertyName)
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
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddContributor (Microsoft.Extensions.DependencyInjection.IServiceCollection obj, ApiServiceContributor`1 contributor)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Restier.Core.ApiConfiguration BuildApiConfiguration (Microsoft.Extensions.DependencyInjection.IServiceCollection obj)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Restier.Core.ApiConfiguration BuildApiConfiguration (Microsoft.Extensions.DependencyInjection.IServiceCollection obj, System.Func`2[[Microsoft.Extensions.DependencyInjection.IServiceCollection],[System.IServiceProvider]] serviceProviderFactory)

	[
	ExtensionAttribute(),
	]
	public static T BuildApiServiceChain (System.IServiceProvider obj)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection ChainPrevious (Microsoft.Extensions.DependencyInjection.IServiceCollection obj)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection ChainPrevious (Microsoft.Extensions.DependencyInjection.IServiceCollection obj, Func`2 factory)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection ChainPrevious (Microsoft.Extensions.DependencyInjection.IServiceCollection obj, Func`3 factory)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection CutoffPrevious (Microsoft.Extensions.DependencyInjection.IServiceCollection obj)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection CutoffPrevious (Microsoft.Extensions.DependencyInjection.IServiceCollection obj, T handler)

	[
	ExtensionAttribute(),
	]
	public static bool HasService (Microsoft.Extensions.DependencyInjection.IServiceCollection obj)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection MakeScoped (Microsoft.Extensions.DependencyInjection.IServiceCollection obj)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection MakeSingleton (Microsoft.Extensions.DependencyInjection.IServiceCollection obj)

	[
	ExtensionAttribute(),
	]
	public static Microsoft.Extensions.DependencyInjection.IServiceCollection MakeTransient (Microsoft.Extensions.DependencyInjection.IServiceCollection obj)
}

public class Microsoft.Restier.Core.ApiConfiguration {
	public ApiConfiguration (System.IServiceProvider serviceProvider)

	System.IServiceProvider ServiceProvider  { public get; }

	[
	CLSCompliantAttribute(),
	]
	public static void Configure (System.Action`1[[Microsoft.Extensions.DependencyInjection.IServiceCollection]] configurationCallback)
}

public class Microsoft.Restier.Core.ApiContext {
	public ApiContext (Microsoft.Restier.Core.ApiConfiguration configuration)

	Microsoft.Restier.Core.ApiConfiguration Configuration  { [CompilerGeneratedAttribute(),]public get; }
	System.IServiceProvider ServiceProvider  { public get; }
}

public class Microsoft.Restier.Core.InvocationContext {
	public InvocationContext (Microsoft.Restier.Core.ApiContext apiContext)

	Microsoft.Restier.Core.ApiContext ApiContext  { [CompilerGeneratedAttribute(),]public get; }
	System.IServiceProvider ServiceProvider  { public get; }
}

public sealed class Microsoft.Restier.Core.ApiServiceContributor`1 : System.MulticastDelegate, ICloneable, ISerializable {
	public ApiServiceContributor`1 (object object, System.IntPtr method)

	public virtual System.IAsyncResult BeginInvoke (System.IServiceProvider serviceProvider, Func`1 next, System.AsyncCallback callback, object object)
	public virtual T EndInvoke (System.IAsyncResult result)
	public virtual T Invoke (System.IServiceProvider serviceProvider, Func`1 next)
}

public class Microsoft.Restier.EntityFramework.DbApi`1 : Microsoft.Restier.Core.ApiBase, IDisposable {
	public DbApi`1 ()

	T DbContext  { protected get; }

	[
	CLSCompliantAttribute(),
	]
	protected virtual Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureApi (Microsoft.Extensions.DependencyInjection.IServiceCollection services)

	protected virtual T CreateDbContext (System.IServiceProvider serviceProvider)
}

[
EditorBrowsableAttribute(),
ExtensionAttribute(),
]
public sealed class Microsoft.Restier.WebApi.HttpConfigurationExtensions {
	[
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[System.Web.OData.Routing.ODataRoute]] MapRestierRoute (System.Web.Http.HttpConfiguration config, string routeName, string routePrefix, params Microsoft.Restier.WebApi.Batch.RestierBatchHandler batchHandler)

	[
	ExtensionAttribute(),
	]
	public static System.Threading.Tasks.Task`1[[System.Web.OData.Routing.ODataRoute]] MapRestierRoute (System.Web.Http.HttpConfiguration config, string routeName, string routePrefix, System.Func`1[[Microsoft.Restier.Core.ApiBase]] apiFactory, params Microsoft.Restier.WebApi.Batch.RestierBatchHandler batchHandler)
}

[
RestierFormattingAttribute(),
RestierExceptionFilterAttribute(),
]
public class Microsoft.Restier.WebApi.RestierController : System.Web.OData.ODataController, IDisposable, IHttpController {
	public RestierController ()

	Microsoft.Restier.Core.ApiBase Api  { public get; }

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Web.Http.IHttpActionResult]] Delete (System.Threading.CancellationToken cancellationToken)

	protected virtual void Dispose (bool disposing)
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
	public System.Threading.Tasks.Task`1[[System.Web.Http.IHttpActionResult]] PostAction (System.Threading.CancellationToken cancellationToken)

	[
	AsyncStateMachineAttribute(),
	]
	public System.Threading.Tasks.Task`1[[System.Web.Http.IHttpActionResult]] Put (System.Web.OData.EdmEntityObject edmEntityObject, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.WebApi.RestierPayloadValueConverter : Microsoft.OData.Core.ODataPayloadValueConverter {
	public RestierPayloadValueConverter ()

	public virtual object ConvertToPayloadValue (object value, Microsoft.OData.Edm.IEdmTypeReference edmTypeReference)
}

public interface Microsoft.Restier.Core.Model.IModelBuilder {
	System.Threading.Tasks.Task`1[[Microsoft.OData.Edm.IEdmModel]] GetModelAsync (Microsoft.Restier.Core.InvocationContext context, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Model.IModelMapper {
	bool TryGetRelevantType (Microsoft.Restier.Core.ApiContext context, string name, out System.Type& relevantType)
	bool TryGetRelevantType (Microsoft.Restier.Core.ApiContext context, string namespaceName, string name, out System.Type& relevantType)
}

[
AttributeUsageAttribute(),
]
public sealed class Microsoft.Restier.Core.Model.ActionAttribute : System.Attribute, _Attribute {
	public ActionAttribute ()

	string EntitySet  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string Name  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string Namespace  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
}

[
AttributeUsageAttribute(),
]
public sealed class Microsoft.Restier.Core.Model.FunctionAttribute : System.Attribute, _Attribute {
	public FunctionAttribute ()

	string EntitySet  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	bool IsComposable  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string Name  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string Namespace  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
}

public interface Microsoft.Restier.Core.Query.IQueryExecutor {
	System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] ExecuteQueryAsync (Microsoft.Restier.Core.Query.QueryContext context, IQueryable`1 query, System.Threading.CancellationToken cancellationToken)
	System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] ExecuteSingleAsync (Microsoft.Restier.Core.Query.QueryContext context, System.Linq.IQueryable query, System.Linq.Expressions.Expression expression, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionExpander {
	System.Linq.Expressions.Expression Expand (Microsoft.Restier.Core.Query.QueryExpressionContext context)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionFilter {
	System.Linq.Expressions.Expression Filter (Microsoft.Restier.Core.Query.QueryExpressionContext context)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionInspector {
	bool Inspect (Microsoft.Restier.Core.Query.QueryExpressionContext context)
}

public interface Microsoft.Restier.Core.Query.IQueryExpressionSourcer {
	System.Linq.Expressions.Expression Source (Microsoft.Restier.Core.Query.QueryExpressionContext context, bool embedded)
}

public abstract class Microsoft.Restier.Core.Query.QueryModelReference {
	Microsoft.OData.Edm.IEdmEntitySet EntitySet  { public abstract get; }
	Microsoft.OData.Edm.IEdmType Type  { public abstract get; }
}

public class Microsoft.Restier.Core.Query.CollectionElementReference : Microsoft.Restier.Core.Query.DerivedDataReference {
	public CollectionElementReference (Microsoft.Restier.Core.Query.QueryModelReference source)

	Microsoft.OData.Edm.IEdmType Type  { public virtual get; }
}

public class Microsoft.Restier.Core.Query.DataSourceStubReference : Microsoft.Restier.Core.Query.QueryModelReference {
	public DataSourceStubReference (Microsoft.Restier.Core.Query.QueryContext context, string name)
	public DataSourceStubReference (Microsoft.Restier.Core.Query.QueryContext context, string namespaceName, string name)

	Microsoft.OData.Edm.IEdmElement Element  { public get; }
	Microsoft.OData.Edm.IEdmEntitySet EntitySet  { public virtual get; }
	Microsoft.OData.Edm.IEdmType Type  { public virtual get; }
}

public class Microsoft.Restier.Core.Query.DefaultQueryExecutor : IQueryExecutor {
	Microsoft.Restier.Core.Query.DefaultQueryExecutor Instance  { [CompilerGeneratedAttribute(),]public static get; }

	public virtual System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] ExecuteQueryAsync (Microsoft.Restier.Core.Query.QueryContext context, IQueryable`1 query, System.Threading.CancellationToken cancellationToken)
	public virtual System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Query.QueryResult]] ExecuteSingleAsync (Microsoft.Restier.Core.Query.QueryContext context, System.Linq.IQueryable query, System.Linq.Expressions.Expression expression, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.Core.Query.DerivedDataReference : Microsoft.Restier.Core.Query.QueryModelReference {
	public DerivedDataReference (Microsoft.Restier.Core.Query.QueryModelReference source)

	Microsoft.OData.Edm.IEdmEntitySet EntitySet  { public virtual get; }
	Microsoft.Restier.Core.Query.QueryModelReference Source  { [CompilerGeneratedAttribute(),]public get; }
	Microsoft.OData.Edm.IEdmType Type  { public virtual get; }
}

public class Microsoft.Restier.Core.Query.PropertyDataReference : Microsoft.Restier.Core.Query.DerivedDataReference {
	public PropertyDataReference (Microsoft.Restier.Core.Query.QueryModelReference source, string propertyName)

	Microsoft.OData.Edm.IEdmProperty Property  { public get; }
	Microsoft.OData.Edm.IEdmType Type  { public virtual get; }
}

public class Microsoft.Restier.Core.Query.QueryContext : Microsoft.Restier.Core.InvocationContext {
	public QueryContext (Microsoft.Restier.Core.ApiContext apiContext, Microsoft.Restier.Core.Query.QueryRequest request)

	Microsoft.OData.Edm.IEdmModel Model  { [CompilerGeneratedAttribute(),]public get; }
	Microsoft.Restier.Core.Query.QueryRequest Request  { [CompilerGeneratedAttribute(),]public get; }
}

public class Microsoft.Restier.Core.Query.QueryExpressionContext {
	public QueryExpressionContext (Microsoft.Restier.Core.Query.QueryContext queryContext)

	System.Action AfterNestedVisitCallback  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	Microsoft.Restier.Core.Query.QueryModelReference ModelReference  { public get; }
	Microsoft.Restier.Core.Query.QueryContext QueryContext  { [CompilerGeneratedAttribute(),]public get; }
	System.Linq.Expressions.Expression VisitedNode  { public get; }

	public void PopVisitedNode ()
	public void PushVisitedNode (System.Linq.Expressions.Expression visitedNode)
	public void ReplaceVisitedNode (System.Linq.Expressions.Expression visitedNode)
}

public class Microsoft.Restier.Core.Query.QueryRequest {
	public QueryRequest (System.Linq.IQueryable query)

	System.Linq.Expressions.Expression Expression  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	bool ShouldReturnCount  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
}

public class Microsoft.Restier.Core.Query.QueryResult {
	public QueryResult (System.Collections.IEnumerable results)
	public QueryResult (System.Exception error)

	System.Exception Error  { public get; public set; }
	System.Collections.IEnumerable Results  { public get; public set; }
	Microsoft.OData.Edm.IEdmEntitySet ResultsSource  { public get; public set; }
}

public enum Microsoft.Restier.Core.Submit.AddAction : int {
	Inserting = 2
	Removing = 3
	Undefined = 0
	Updating = 1
}

public enum Microsoft.Restier.Core.Submit.ChangeSetEntryType : int {
	ActionInvocation = 1
	DataModification = 0
}

public enum Microsoft.Restier.Core.Submit.DynamicChangeSetEntityState : int {
	Changed = 0
	ChangedWithinOwnPreEventing = 3
	PreEvented = 4
	PreEventing = 2
	Validated = 1
}

public enum Microsoft.Restier.Core.Submit.ValidationSeverity : int {
	Error = 0
	Informational = 2
	Warning = 1
}

public interface Microsoft.Restier.Core.Submit.IChangeSetEntryAuthorizer {
	System.Threading.Tasks.Task`1[[System.Boolean]] AuthorizeAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetEntry entry, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.IChangeSetEntryFilter {
	System.Threading.Tasks.Task OnExecutedEntryAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetEntry entry, System.Threading.CancellationToken cancellationToken)
	System.Threading.Tasks.Task OnExecutingEntryAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetEntry entry, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.IChangeSetEntryValidator {
	System.Threading.Tasks.Task ValidateEntityAsync (Microsoft.Restier.Core.Submit.SubmitContext context, Microsoft.Restier.Core.Submit.ChangeSetEntry entry, Microsoft.Restier.Core.Submit.ValidationResults validationResults, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.IChangeSetPreparer {
	System.Threading.Tasks.Task PrepareAsync (Microsoft.Restier.Core.Submit.SubmitContext context, System.Threading.CancellationToken cancellationToken)
}

public interface Microsoft.Restier.Core.Submit.ISubmitExecutor {
	System.Threading.Tasks.Task`1[[Microsoft.Restier.Core.Submit.SubmitResult]] ExecuteSubmitAsync (Microsoft.Restier.Core.Submit.SubmitContext context, System.Threading.CancellationToken cancellationToken)
}

public abstract class Microsoft.Restier.Core.Submit.ChangeSetEntry {
	Microsoft.Restier.Core.Submit.DynamicChangeSetEntityState ChangeSetEntityState  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	Microsoft.Restier.Core.Submit.ChangeSetEntryType Type  { [CompilerGeneratedAttribute(),]public get; }

	public bool HasChanged ()
}

public class Microsoft.Restier.Core.Submit.ActionInvocationEntry : Microsoft.Restier.Core.Submit.ChangeSetEntry {
	public ActionInvocationEntry (string actionName, System.Collections.Generic.IDictionary`2[[System.String],[System.Object]] arguments)

	string ActionName  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	System.Collections.Generic.IDictionary`2[[System.String],[System.Object]] Arguments  { [CompilerGeneratedAttribute(),]public get; }
	object Result  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }

	public object[] GetArgumentArray ()
}

public class Microsoft.Restier.Core.Submit.ChangeSet {
	public ChangeSet ()
	public ChangeSet (System.Collections.Generic.IEnumerable`1[[Microsoft.Restier.Core.Submit.ChangeSetEntry]] entries)

	System.Collections.Generic.IList`1[[Microsoft.Restier.Core.Submit.ChangeSetEntry]] Entries  { public get; }
}

public class Microsoft.Restier.Core.Submit.DataModificationEntry : Microsoft.Restier.Core.Submit.ChangeSetEntry {
	public DataModificationEntry (string entitySetName, string entityTypeName, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] entityKey, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] originalValues, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] localValues)

	Microsoft.Restier.Core.Submit.AddAction AddAction  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	object Entity  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] EntityKey  { [CompilerGeneratedAttribute(),]public get; }
	string EntitySetName  { [CompilerGeneratedAttribute(),]public get; }
	string EntityTypeName  { [CompilerGeneratedAttribute(),]public get; }
	bool IsDelete  { public get; }
	bool IsFullReplaceUpdate  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	bool IsNew  { public get; }
	bool IsUpdate  { public get; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] LocalValues  { [CompilerGeneratedAttribute(),]public get; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] OriginalValues  { [CompilerGeneratedAttribute(),]public get; }
	System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] ServerValues  { [CompilerGeneratedAttribute(),]public get; }

	public System.Linq.IQueryable ApplyTo (System.Linq.IQueryable query)
}

public class Microsoft.Restier.Core.Submit.DataModificationEntry`1 : Microsoft.Restier.Core.Submit.DataModificationEntry {
	public DataModificationEntry`1 (string entitySetName, string entityTypeName, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] entityKey, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] originalValues, System.Collections.Generic.IReadOnlyDictionary`2[[System.String],[System.Object]] localValues)

	T Entity  { public get; public set; }
}

public class Microsoft.Restier.Core.Submit.SubmitContext : Microsoft.Restier.Core.InvocationContext {
	public SubmitContext (Microsoft.Restier.Core.ApiContext apiContext, Microsoft.Restier.Core.Submit.ChangeSet changeSet)

	Microsoft.Restier.Core.Submit.ChangeSet ChangeSet  { public get; public set; }
	Microsoft.OData.Edm.IEdmModel Model  { [CompilerGeneratedAttribute(),]public get; }
	Microsoft.Restier.Core.Submit.SubmitResult Result  { public get; public set; }
}

public class Microsoft.Restier.Core.Submit.SubmitResult {
	public SubmitResult (Microsoft.Restier.Core.Submit.ChangeSet completedChangeSet)
	public SubmitResult (System.Exception error)

	Microsoft.Restier.Core.Submit.ChangeSet CompletedChangeSet  { public get; public set; }
	System.Exception Error  { public get; public set; }
}

public class Microsoft.Restier.Core.Submit.ValidationException : System.Exception, _Exception, ISerializable {
	public ValidationException (string message)
	public ValidationException (string message, System.Exception innerException)

	System.Collections.Generic.IEnumerable`1[[Microsoft.Restier.Core.Submit.ValidationResult]] ValidationResults  { public get; public set; }
}

public class Microsoft.Restier.Core.Submit.ValidationResult {
	public ValidationResult ()

	string Id  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string Message  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	string PropertyName  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	Microsoft.Restier.Core.Submit.ValidationSeverity Severity  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }
	object Target  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }

	public virtual string ToString ()
}

public class Microsoft.Restier.Core.Submit.ValidationResults : System.Collections.ObjectModel.Collection`1[[Microsoft.Restier.Core.Submit.ValidationResult]], ICollection, IEnumerable, IList, ICollection`1, IEnumerable`1, IList`1, IReadOnlyCollection`1, IReadOnlyList`1 {
	public ValidationResults ()

	System.Collections.Generic.IEnumerable`1[[Microsoft.Restier.Core.Submit.ValidationResult]] Errors  { public get; }
	bool HasErrors  { public get; }
}

public class Microsoft.Restier.WebApi.Batch.RestierBatchHandler : System.Web.OData.Batch.DefaultODataBatchHandler, IDisposable {
	public RestierBatchHandler (System.Web.Http.HttpServer httpServer, params System.Func`1[[Microsoft.Restier.Core.ApiBase]] apiFactory)

	System.Func`1[[Microsoft.Restier.Core.ApiBase]] ApiFactory  { [CompilerGeneratedAttribute(),]public get; [CompilerGeneratedAttribute(),]public set; }

	protected virtual System.Web.OData.Batch.ChangeSetRequestItem CreateChangeSetRequestItem (System.Collections.Generic.IList`1[[System.Net.Http.HttpRequestMessage]] changeSetRequests)
	[
	AsyncStateMachineAttribute(),
	]
	public virtual System.Threading.Tasks.Task`1[[System.Collections.Generic.IList`1[[System.Web.OData.Batch.ODataBatchRequestItem]]]] ParseBatchRequestsAsync (System.Net.Http.HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.WebApi.Batch.RestierChangeSetRequestItem : System.Web.OData.Batch.ChangeSetRequestItem, IDisposable {
	public RestierChangeSetRequestItem (System.Collections.Generic.IEnumerable`1[[System.Net.Http.HttpRequestMessage]] requests, System.Func`1[[Microsoft.Restier.Core.ApiBase]] apiFactory)

	[
	AsyncStateMachineAttribute(),
	]
	public virtual System.Threading.Tasks.Task`1[[System.Web.OData.Batch.ODataBatchResponseItem]] SendRequestAsync (System.Net.Http.HttpMessageInvoker invoker, System.Threading.CancellationToken cancellationToken)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.DefaultRestierSerializerProvider : System.Web.OData.Formatter.Serialization.DefaultODataSerializerProvider {
	public DefaultRestierSerializerProvider ()

	public virtual System.Web.OData.Formatter.Serialization.ODataEdmTypeSerializer GetEdmTypeSerializer (Microsoft.OData.Edm.IEdmTypeReference edmType)
	public virtual System.Web.OData.Formatter.Serialization.ODataSerializer GetODataPayloadSerializer (Microsoft.OData.Edm.IEdmModel model, System.Type type, System.Net.Http.HttpRequestMessage request)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.RestierCollectionSerializer : System.Web.OData.Formatter.Serialization.ODataCollectionSerializer {
	public RestierCollectionSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.Core.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.RestierComplexTypeSerializer : System.Web.OData.Formatter.Serialization.ODataComplexTypeSerializer {
	public RestierComplexTypeSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.Core.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.RestierEntityTypeSerializer : System.Web.OData.Formatter.Serialization.ODataEntityTypeSerializer {
	public RestierEntityTypeSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual string CreateETag (System.Web.OData.EntityInstanceContext entityInstanceContext)
	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.Core.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.RestierEnumSerializer : System.Web.OData.Formatter.Serialization.ODataEnumSerializer {
	public RestierEnumSerializer ()

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.Core.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.RestierFeedSerializer : System.Web.OData.Formatter.Serialization.ODataFeedSerializer {
	public RestierFeedSerializer (System.Web.OData.Formatter.Serialization.ODataSerializerProvider provider)

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.Core.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.RestierPrimitiveSerializer : System.Web.OData.Formatter.Serialization.ODataPrimitiveSerializer {
	public RestierPrimitiveSerializer ()

	public virtual Microsoft.OData.Core.ODataPrimitiveValue CreateODataPrimitiveValue (object graph, Microsoft.OData.Edm.IEdmPrimitiveTypeReference primitiveType, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.Core.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

public class Microsoft.Restier.WebApi.Formatter.Serialization.RestierRawSerializer : System.Web.OData.Formatter.Serialization.ODataRawValueSerializer {
	public RestierRawSerializer ()

	public virtual void WriteObject (object graph, System.Type type, Microsoft.OData.Core.ODataMessageWriter messageWriter, System.Web.OData.Formatter.Serialization.ODataSerializerContext writeContext)
}

