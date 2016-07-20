---
layout: post
title: "2.10 Customize Payload Converter"
description: ""
category: "2. Features"
---

RESTier supports to customize the payload to be read or written (a.k.a serialize and deserialize), user can extend the class RestierPayloadValueConverter to overwrite method ConvertToPayloadValue for payload writing and ConvertFromPayloadValue for payload reading.

This is an example on how to customize a specified string value to add some prefix and write into response, refer to this [**link**](https://github.com/OData/RESTier/blob/master/test/ODataEndToEndTests/Microsoft.Restier.WebApi.Test.Services.Trippin/Models/CustomizedPayloadValueConverter.cs) to see the end to end samples,

**1.** Create a class to have the customized converter logic
{% highlight csharp %}
    public class CustomizedPayloadValueConverter : RestierPayloadValueConverter
    {
        public override object ConvertToPayloadValue(object value, IEdmTypeReference edmTypeReference)
        {
            if (edmTypeReference != null)
            {
                if (value is string)
                {
                    var stringValue = (string) value;

                    // Make a single string value "Russell" converted to have additional suffix
                    if (stringValue == "Russell")
                    {
                        return stringValue + "Converter";
                    }
                }
            }

            return base.ConvertToPayloadValue(value, edmTypeReference);
        }
    }
{% endhighlight %}

**2.** Register customized converter into RESTier Dependency Injection framework as a service via overriding the ConfigureApi method in your Api class.

{% highlight csharp %}
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .AddSingleton<ODataPayloadValueConverter, CustomizedPayloadValueConverter>();
        }
{% endhighlight %}

Then when writting payload for response, any string which has value "Russell" will become "RussellConverter".