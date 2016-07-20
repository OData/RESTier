---
layout: post
title: "2.8 Customize Query"
description: ""
category: "2. Features"
---

RESTier supports to customize the query setting and query process logic.

**1. Customize Query Setting**

RESTier supports to customize kinds of query setting like AllowedLogicalOperators, AllowedQueryOptions, MaxExpansionDepth, MaxAnyAllExpressionDepth and so on. Refer to [class](https://github.com/OData/WebApi/blob/master/OData/src/System.Web.OData/OData/Query/ODataValidationSettings.cs) for full list of settings.

This is an example on how to customize MaxExpansionDepth from default value 2 to 3 which means allowing two level nested expand now, refer to this [**link**](https://github.com/OData/RESTier/blob/master/test/ODataEndToEndTests/Microsoft.Restier.WebApi.Test.Services.Trippin/Api/TrippinApi.cs) to see the end to end samples,

First create a factory delegate which will create a new instance of ODataValidationSettings, then registering it into RESTier Dependency Injection framework as a service via overriding the ConfigureApi method in your Api class.

{% highlight csharp %}
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            // Add OData Query Settings and valiadtion settings
            Func<IServiceProvider, ODataValidationSettings> validationSettingFactory = (sp) => new ODataValidationSettings
            {
                MaxAnyAllExpressionDepth =3,
                MaxExpansionDepth = 3
            };

            return base.ConfigureApi(services)
                .AddSingleton<ODataValidationSettings>(validationSettingFactory);
        }
{% endhighlight %}

Then $expand with supported with max two nested $expand via only max one nested $expand is supported by default before we apply this customization.


**2. Customize Query Logic**

RESTier supports built in convention based query customized logic (refer to [section 2.2](http://odata.github.io/RESTier/#02-02-entity-set-filters-new)), besides this, RESTier has two interfaces IQueryExpressionAuthorizer and IQueryExpressionProcessor for end user to further customize the query process logic.

**Customized Authorize Logic**

User can use interface IQueryExpressionAuthorizer to define any customize authorize logic to see whether user is authorized for the specified query, if this method returns false, then the related query will get error code 403 (forbidden).

There are two steps to plug in customized process logic,

First create a class CustomizedAuthorizer implement IQueryExpressionAuthorizer, and add any process logic needed.

Second, registering it into RESTier Dependency Injection framework as a service via overriding the ConfigureApi method in your Api class.

{% highlight csharp %}
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {

            return base.ConfigureApi(services)
                .AddService<IQueryExpressionInspector, CustomizedInspector>();
        }
{% endhighlight %}

In CustomizedAuthorizer, user can decide whether to call the RESTier logic, if user decide to call the RESTier logic, user can defined a property like "private IQueryExpressionAuthorizer InnerAuthorizer {get; set;}" in class CustomizedAuthorizer, then call InnerAuthorizer.Authorize() to call RESTier logic.


**Customized Process Logic**

User can create class implementing interface IQueryExpressionProcessor to customize the LINQ query expression build process like to replace part of expression, remove part of expression or append part of expression. Then registered the customized class as DI service.The steps to plugin is same as above.

The logic [OnFilter[entity set name]](http://odata.github.io/RESTier/#02-02-entity-set-filters-new) is been processed by RESTier default expression processor which add a where clause after entity set. The way to call default logic is same as above.

