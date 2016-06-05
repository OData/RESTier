# Method Authorization

Method Authorization allows you to have fine-grain control over how different types of API requests can be executed.
Since most of RESTier uses built-in convention over repetitive boiler-plate Controllers, you can't just add security attributes
to the controller methods, like you can with Web API.

However, there are two different methods for defining per-request security. One, like the rest of RESTier, is
convention-based, and the other executes before every request, allowing you to centralize your authorization logic.
This allows you to pick the approach that works best for your architecture.

No matter what approach you chose, the concept is simple. Either technique uses a function that returns boolean. 
Return `true`, and processing continues normally. Return `false`, and RESTier returns a 403 Unauthorized to the client.

## Convention-Based Authorization
Users can control if one of the four submit operations is allowed on some entity set or action by putting some 
`protected internal` methods into the `Api` class. The method signatures must exactly match the following examples. The 
method name must conform to the convention `Can{Operation}{TargetName}`.

<table style="width: 100%;">
    <tr>
        <td>The possible values for <code>{Operation}</code> are:</td>
        <td>The possible values for <code>{TargetName}</code> are:</td>
    </tr>
    <tr>
        <td>
            <ul style="margin-bottom: 0;">
                <li>Insert</li>
                <li>Update</li>
                <li>Delete</li>
                <li>Execute</li>
            </ul>
        </td>
        <td style="vertical-align: text-top;">
            <ul style="margin-bottom: 0;">
                <li><i>EntitySetName</i></li>
                <li><i>ActionName</i></li>
            </ul>
        </td>
    </tr>
</table>

### Example

The example below demonstrates how both types of `{TargetName}` can be used. 

- The first method shows a simple way to prevent *any*  user from deleting a particular EntitySet.
- The second method shows how you can integrate role-based security using multiple techniques.
- The third method shows how to prevent execution a custom Action.

```cs
namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    ///<summary>
    /// Customizations to the EntityFrameworkApi for the TripPin service.
    ///</summary>
    ///<example>
    /// Add the following line in WebApiConfig.cs to register this code:
    /// await config.MapRestierRoute<TrippinApi>("Trippin", "api", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
    ///</example>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        
        ///<summary>
        /// Specifies whether or not a Trip can be deleted from an EntitySet.
        ///</summary>
        protected internal bool CanDeleteTrips()
        {
            return false;
        }

        ///<summary>
        /// User role-based security to specifies whether or not a updated Trip can be sent to an EntitySet.
        ///</summary>
        protected internal bool CanUpdateTrips()
        {
            // Use legacy role-based security
            return HttpContext.Current.User.IsInRole("admin");

            // You can also use claims-based security.
            // return ClaimsPrincipal.Current.IsInRole("admin");
        }
        
        ///<summary>
        /// Specifies whether or not an Action called ResetDataSource can be executed through the API.
        ///</summary>
        protected internal bool CanExecuteResetDataSource()
        {
            return false;
        }

    }

}
```

## Centralized Authorization

In addition to the more granular convention-based approach, you can also centralize processing into one location. This is
useful if 

User can use interface `IChangeSetItemAuthorizer` to define any customize authorize logic to see whether user is 
authorized for the specified submit, if this method return false, then the related query will get error code 403 (Forbidden).

There are two steps to plug in the centralized authorization logic.

- Create a class that implements `IChangeSetItemAuthorizer`.
- Register that class with RESTier through Dependency Injection (DI).

### Example

```cs
namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    ///<summary>
    ///
    ///</summary>
    public class CustomAuthorizer : IChangeSetItemAuthorizer
    {

        // The inner handler will call CanUpdate/Insert/Delete<EntitySet> method
        private IChangeSetItemProcessor Inner { get; set; }

        ///<summary>
        ///
        ///</summary>
        public Task<bool> AuthorizeAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
	        // TODO: RWM: Provide legitimate samples here, along with parameter documentation.
        }

    }

    ///<summary>
    /// Customizations to the EntityFrameworkApi for the TripPin service.
    ///</summary>
    ///<example>
    /// Add the following line in WebApiConfig.cs to register this code:
    /// await config.MapRestierRoute<TrippinApi>("Trippin", "api", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
    ///</example>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {

        ///<summary>
        /// Allows us to leverage DI to inject additional capabilities into RESTier.
        ///</summary>
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .AddService<IChangeSetItemAuthorizer, CustomizedAuthorizer>();
        }

    }

}
```

In CustomizedAuthorizer, user can decide whether to call the RESTier logic, if user decide to call the RESTier logic,
user can defined a property like "private IChangeSetItemAuthorizer Inner {get; set;}" in class CustomizedAuthorizer,
then call Inner.Inspect() to call RESTier logic which call Authorize part logic defined in section 2.3.

## Unit Testing Considerations

Because both of these methods are de-coupled from the code that interacts with the database, the Authorization
logic is easily testable, without having to fire up the entire Web API + RESTier pipeline.

### Setting up your Unit Test

If you don't have a unit test project for your API project already, start by creating one.