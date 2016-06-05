# Method Authorization

Method Authorization allows you to have fine-grain control over how different types of API requests can be executed.


## Convention-Based Authorization
Users can control if one of the four submit operations is allowed on some entity set or action by putting some 
**protected** methods into the `Api` class. The method signatures must exactly match the following examples. The 
method name must conform to the convention `Can{Operation}{TargetName}`.

The possible values for {Operation} are:

+  Insert
+  Update
+  Delete
+  Execute

Possible values for {TargetName} are:

+ *EntitySetName*
+ *ActionName*

### Example

The example below demonstrates how both types of {TargetName} can be used. 

- The first method shows a simple way to prevent *any* user from deleting a particular EntitySet.
- The second example shows how you can integrate role-based security

```cs
namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    ///<summary>
    /// Specifies whether or not a Trip can be deleted through the API.
    ///</summary>
    ///<example>
    /// Add the following line in WebApiConfig.cs to register this code:
    /// await config.MapRestierRoute<TrippinApi>("Trippin", "api", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
    ///</example>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        
        ///<summary>
        /// Specifies whether or not a Trip can be deleted through the API.
        ///</summary>
        protected internal bool CanDeleteTrips()
        {
            return false;
        }

        ///<summary>
        /// User role-based security to specifies whether or not a Trip can be updated through the API.
        ///</summary>
        protected internal bool CanUpdateTrips()
        {
            // Use legacy role-based security
            return HttpContext.Current.User.IsInRole("admin");

            // You can also use claims-based security.
            // return ClaimsPrincipal.Current.IsInRole("admin");
        }
        
        ///<summary>
        /// Specifies whether or not an action called ResetDataSource can be executed through the API.
        ///</summary>
        protected internal bool CanExecuteResetDataSource()
        {
            return false;
        }

    }

}
```

### Centralized Authorization
[section 2.9](http://odata.github.io/RESTier/#02-09-Customize-Submit)