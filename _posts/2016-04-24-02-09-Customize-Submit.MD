---
layout: post
title: "2.9 Customize Submit [>=0.5.0]"
description: ""
category: "2. Features"
---

RESTier supports built in conversion based logic (refer to [section 2.3](http://odata.github.io/RESTier/#02-02-Submit-Logic)) for submit, besides this, RESTier has three interfaces IChangeSetItemAuthorizer, IChangeSetItemValidator and IChangeSetItemProcessor for end user to customize the logic.

**Customized Authorize Logic**

User can use interface IChangeSetItemAuthorizer to define any customize authorize logic to see whether user is authorized for the specified submit, if this method return false, then the related query will get error code 403 (forbidden).

There are two steps to plug in customized process logic,

First create a class CustomizedAuthorizer implement IChangeSetItemAuthorizer, and add any process logic needed.

Second, registering it into RESTier Dependency Injection framework as a service via overriding the ConfigureApi method in your Api class.

{% highlight csharp %}
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {

            return base.ConfigureApi(services)
                .AddService<IChangeSetItemAuthorizer, CustomizedAuthorizer>();
        }
{% endhighlight %}

In CustomizedAuthorizer, user can decide whether to call the RESTier logic, if user decide to call the RESTier logic, user can defined a property like "private IChangeSetItemAuthorizer Inner {get; set;}" in class CustomizedAuthorizer, then call Inner.Inspect() to call RESTier logic which call Authorize part logic defined in section 2.3.


**Customized Validation Logic**

User can use interface IChangeSetItemValidator to customize validation logic for submit, and if validate fails, add a error validation result to validation results, then the request will get 400(bad request) return code, here is a sample customize validation logic,

{% highlight csharp %}

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

{% endhighlight %}

The steps to plugin the logic is same as above.

**Customized Process Logic**

User can use interface IChangeSetItemProcessor to customize logic before or after submit, PreProcess logic is called before submit and PostProcess logic is called after submit, RESTier default logic is defined in section 2.3 plugin user logic part. Default logic can be called via defined a property with type IChangeSetItemProcessor like "private IChangeSetItemProcessor Inner {get; set;}", and user call Inner.PreProcess or PostProcess to call RESTier logic, if in CustomizedProcessor, there is no such property defined or inner is not used, then RESTier logic will not be called.

The steps to plugin the logic is same as above.