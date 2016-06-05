# Method Authorization

Submit logic convention allows users to authorize a submit operation or plug in user logic (such as logging) before 
and after a submit operation. Usually a submit operation can be inserting an entity, deleting an entity, updating 
an entity or executing an OData action.

### Convention-Based Authorization
Users can control if one of the four submit operations is allowed on some entity set or action by putting some 
**protected** methods into the `Api` class. The method signatures must exactly match the following examples. The 
method name must conform to `Can{Operation}{TargetName}`.

The possible values for {Operation} are:

+  Insert
+  Update
+  Delete
+  Execute

Possible values for {TargetName} are:

+ *EntitySetName*
+ *ActionName*

```cs
namespace Microsoft.OData.Service.Sample.Trippin.Api
{
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        ...
        // Can delete an entity from the entity set Trips?
        protected bool CanDeleteTrips()
        {
            return false;
        }
        
        // Can execute an action named ResetDataSource?
        protected bool CanExecuteResetDataSource()
        {
            return false;
        }
    }
}
```

### Centralized Authorization
[section 2.9](http://odata.github.io/RESTier/#02-09-Customize-Submit)