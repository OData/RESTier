---
layout: post
title: "2.9 Customize Submit [>=0.5.0]"
description: ""
category: "2. Features"
---

RESTier supports built in convention based logic (refer to [section 2.3](http://odata.github.io/RESTier/#02-02-Submit-Logic)) for submit, besides this, RESTier has three interfaces IChangeSetItemAuthorizer, IChangeSetItemValidator and IChangeSetItemProcessor for end user to customize the logic.

**Customized Authorize Logic**

User can use interface IChangeSetItemAuthorizer to define any customize authorize logic to see whether user is authorized for the specified submit, if this method return false, then the related query will get error code 403 (forbidden).

There are two steps to plug in customized process logic,

First create a class CustomizedAuthorizer implement IChangeSetItemAuthorizer, and add any process logic needed.
{% highlight csharp %}

public class CustomizedAuthorizer : IChangeSetItemAuthorizer
{
    // The inner handler will call CanUpdate/Insert/Delete<EntitySet> method
    private IChangeSetItemProcessor Inner { get; set; }

    public Task<bool> AuthorizeAsync(
        SubmitContext context,
        ChangeSetItem item,
        CancellationToken cancellationToken)
    {
	    // Add any customized logic here
    }
}

{% endhighlight %}

Second, registering it into RESTier Dependency Injection framework as a service via overriding the ConfigureApi method in your Api class.

{% highlight csharp %}
namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Api
{
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .AddService<IChangeSetItemAuthorizer, CustomizedAuthorizer>();
        }
    }
}
{% endhighlight %}

In CustomizedAuthorizer, user can decide whether to call the RESTier logic, if user decide to call the RESTier logic, user can defined a property like "private IChangeSetItemAuthorizer Inner {get; set;}" in class CustomizedAuthorizer, then call Inner.Inspect() to call RESTier logic which call Authorize part logic defined in section 2.3.


**Customized Validation Logic**

User can use interface IChangeSetItemValidator to customize validation logic for submit, and if validate fails, add a error validation result to validation results, then the request will get 400(bad request) return code, here is a sample customize validation logic,

{% highlight csharp %}

public class CustomizedValidator : IChangeSetItemValidator
{
	// Add any customized validation into this method
    public Task ValidateChangeSetItemAsync(
        SubmitContext context,
        ChangeSetItem item,
        Collection<ChangeSetItemValidationResult> validationResults,
        CancellationToken cancellationToken)
    {
	    DataModificationEntry dataModificationEntry = entry as DataModificationEntry;
	    var entity = dataModificationEntry.Entity;
	
	     // Customized validate logic and if there is error, add a validation result with error level.
	    validationResults.Add(new ChangeSetValidationResult()
	    {
	        Id = dataModificationEntry.EntitySetName+ dataModificationEntry.EntityKey,
	        Message = "Customized error message",
	        Severity = EventLevel.Error,
	        Target = entity
	    });
	}
}

{% endhighlight %}

The steps to plugin the logic is same as above.

**Customized Process Logic**

User can use interface IChangeSetItemProcessor to customize logic before or after submit, PreProcess logic is called before submit and PostProcess logic is called after submit, RESTier default logic is defined in section 2.3 plugin user logic part. Default logic can be called via defined a property with type IChangeSetItemProcessor like "private IChangeSetItemProcessor Inner {get; set;}", and user call Inner.PreProcess or PostProcess to call RESTier logic, if in CustomizedProcessor, there is no such property defined or inner is not used, then RESTier logic will not be called.



{% highlight csharp %}
namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Submit
{
    public class CustomizedSubmitProcessor : IChangeSetItemProcessor
    {
        private IChangeSetItemProcessor Inner { get; set; }

        // Any customized logic needed before persist called can be added here.
        // Inner call related OnUpdating|Inseting|Deleting<EntitySet> methods
        public Task OnProcessingChangeSetItemAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            return Inner.OnProcessingChangeSetItemAsync(context, item, cancellationToken);
        }

        // Any customized logic needed after persist called can be added here.
        // Inner call related OnUpdated|Inseted|Deleted<EntitySet> methods
        public Task OnProcessedChangeSetItemAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
            var dataModificationItem = item as DataModificationItem;
            if (dataModificationItem != null)
            {
                object myEntity = dataModificationItem.Entity;
                string entitySetName = dataModificationItem.EntitySetName;
                ChangeSetItemAction operation = dataModificationItem.ChangeSetItemAction;

                // In case of insert, the request URL has no key, and request body may not have key neither as the key may be generated by database
                var keyAttrbiutes = new Dictionary<string, object>();
                var keyConvention = new Dictionary<string, object>();

                var entityTypeName = myEntity.GetType().Name;
                PropertyInfo[] properties = myEntity.GetType().GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    var attribute = Attribute.GetCustomAttribute(property, typeof(KeyAttribute))
                        as KeyAttribute;
                    var propName = property.Name;
                    // This is getting key with Key attribute defined
                    if (attribute != null) // This property has a KeyAttribute
                    {
                        // Do something, to read from the property:
                        object val = property.GetValue(myEntity);
                        keyAttrbiutes.Add(propName, val);
                    }
                    // This is getting key based on convention
                    else if(propName.ToLower().Equals("id") || propName.ToLower().Equals(entityTypeName.ToLower()+"id"))
                    {
                        object val = property.GetValue(myEntity);
                        keyConvention.Add(propName, val);
                    }
                }
                if (keyAttrbiutes.Count > 0)
                {
                    // Use property with key attribute as keys    
                }
                else if(keyConvention.Count > 0)
                {
                    // Key is defined based on convention
                }
            }
            return Inner.OnProcessedChangeSetItemAsync(context, item, cancellationToken);
        }
    }
}
{% endhighlight %}

The steps to plugin the logic is same as above.