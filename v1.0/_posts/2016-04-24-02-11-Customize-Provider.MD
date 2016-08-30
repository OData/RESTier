---
layout: post
title: "2.11 Customize Serializer and Deserializer Provider"
description: ""
category: "2. Features"
---

RESTier supports to customize serializer and deserializer provider for payload reading and writing, then in the provider, it can return customized serializer or deserializer for specified EdmType to customize the payload reading and writing.

This is an example on how to customize ODataComplexTypeSerializer to customize how complex type payload is serialized for response.

First create a class which extends ODataComplexTypeSerializer, and override method WriteObject.

Second create a class which extends DefaultRestierSerializerProvider, and override method GetODataPayloadSerializer and GetEdmTypeSerializer which will return the customized serializer, and this is sample code,

{% highlight csharp %}
        public override ODataSerializer GetODataPayloadSerializer(
            IEdmModel model,
            Type type,
            HttpRequestMessage request)
        {
            ODataSerializer serializer = null;
            if (type == typeof (ComplexResult))
            {
                serializer = customizerComplexSerialier;
            }
            else
            {
                serializer = base.GetODataPayloadSerializer(model, type, request);
            }

            return serializer;
        }

        public override ODataEdmTypeSerializer GetEdmTypeSerializer(IEdmTypeReference edmType)
        {
            if (edmType.IsEntity())
            {
                return this.entityTypeSerializer;
            }

            if (edmType.IsComplex())
            {
                return customizerComplexSerialier;
            }

            return base.GetEdmTypeSerializer(edmType);

        }
{% endhighlight %}

Third, register customized serializer provider as DI service in the Api ConfigureApi method
{% highlight csharp %}
        protected static new IServiceCollection ConfigureApi(Type apiType, IServiceCollection services)
        {
            return ApiBase.ConfigureApi(apiType, services)
                .AddSingleton<ODataSerializerProvider, CustomizedSerializerProvider>();
        }
{% endhighlight %}

With these customized code, the complex result will be serialized in the customized way.