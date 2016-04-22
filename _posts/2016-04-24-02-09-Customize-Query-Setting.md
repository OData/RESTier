---
layout: post
title: "2.9 Customize Query Setting [>=0.5.0]"
description: ""
category: "2. Features"
---

RESTier supports to customize kinds of query setting like AllowedLogicalOperators, AllowedQueryOptions, MaxExpansionDepth, MaxAnyAllExpressionDepth and so on. Refer to [class](https://github.com/OData/WebApi/blob/master/OData/src/System.Web.OData/OData/Query/ODataValidationSettings.cs) for full list of settings.

This is an example on how to customize MaxExpansionDepth from default value 2 to 3 which means allowing two level nested expand now, refer to this [**link**](https://github.com/OData/RESTier/blob/master/test/ODataEndToEndTests/Microsoft.Restier.WebApi.Test.Services.Trippin/Api/TrippinApi.cs) to see the end to end samples,

First create a factory delegate which will create a new instance of ODataValidationSettings, then registering it into RESTier Depenedncy Injection framework as a service via overriding the ConfigureApi method in your Api class.

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