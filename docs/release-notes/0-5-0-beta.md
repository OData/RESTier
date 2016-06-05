## Downloads

 - NuGet: `Install-Package Microsoft.Restier -Pre` [[Website](http://www.nuget.org/packages/Microsoft.Restier/0.5.0-beta)]
 - Source: [[Zip](https://github.com/OData/RESTier/archive/0.5.0-beta.zip)] [[Tarball](https://github.com/OData/RESTier/archive/0.5.0-beta.tar.gz)]

## New Features

 - [[Issue](https://github.com/OData/RESTier/issues/150)] [[PR](https://github.com/OData/RESTier/pull/286)] Integrate Microsoft Dependency Injection Framework into RESTier. [Tutorial](http://odata.github.io/RESTier/#04-04-Api-Service).
 - [[Issue](https://github.com/OData/RESTier/issues/273)] [[PR](https://github.com/OData/RESTier/pull/278)] Support temporal types in Restier.EF. [Tutorial](http://odata.github.io/RESTier/#03-07-Temporal).
 - [[Issue](https://github.com/OData/RESTier/issues/383)] [[PR](https://github.com/OData/RESTier/pull/402)] Adopt Web OData Conversion Model builder as default EF provider model builder. [Tutorial](http://odata.github.io/WebApi/#02-04-convention-model-builder).
 - [[Issue](https://github.com/OData/RESTier/issues/360)] [[PR](https://github.com/OData/RESTier/pull/399)] Support $apply in RESTier. [Tutorial](http://docs.oasis-open.org/odata/odata-data-aggregation-ext/v4.0/odata-data-aggregation-ext-v4.0.html).

## Enhancements

 - The concept of **hook handler** now becomes **API service** after DI integration.
 - The interface `IHookHandler` and `IDelegateHookHandler` are removed. The implementation of any custom API service (previously known as hook handler) should also change accordingly. But this should not be big change. Please see [Tutorial](http://odata.github.io/RESTier/#04-04-Api-Service) for details.
 - `AddHookHandler` is now replaced with `AddService` from DI. Please see [Tutorial](http://odata.github.io/RESTier/#04-04-Api-Service) for details.
 - `GetHookHandler` is now replaced with `GetApiService` and `GetService` from DI. Please see [Tutorial](http://odata.github.io/RESTier/#04-04-Api-Service) for details.
 - All the serializers and `DefaultRestierSerializerProvider` are now public. But we still need to address [#301](https://github.com/OData/RESTier/issues/301) to allow users to override the serializers.
 - The interface `IApi` is now removed. Use `ApiBase` instead. We never expect users to directly implement their API classes from `IApi` anyway. The `Context` property in `IApi` now becomes a public property in `ApiBase`.
 - Previously the `ApiData` class is very confusing. Now we have given it a more meaningful name `DataSourceStubs` which accurately describes the usage. Along with this change, we also rename `ApiDataReference` to `DataSourceStubReference` accordingly.
 - `ApiBase.ApiConfiguration` is renamed to `ApiBase.Configuration` to keep consistent with `ApiBase.Context`.
 - The static `Api` class is now separated into two classes `ApiBaseExtensions` and `ApiContextExtensions` to eliminate the ambiguity regarding the previous `Api` class.
## Bug Fixes

 - [[Issue](https://github.com/OData/RESTier/issues/123)] [[PR](https://github.com/OData/RESTier/pull/294)] Fix a bug that prevents using `Edm.Int64` as entity key.
 - [[Issue](https://github.com/OData/RESTier/issues/269)] [[PR](https://github.com/OData/RESTier/pull/271)] Fix a bug that `NullReferenceException` is thrown when POST/PATCH/PUT with null property values.
 - [[Issue](https://github.com/OData/RESTier/issues/287)] [[PR](https://github.com/OData/RESTier/pull/314)] Fix a bug that $count does not work correctly when there is $expand.
 - [[Issue](https://github.com/OData/RESTier/issues/304)] [[PR](https://github.com/OData/RESTier/pull/306)] Fix a bug that `GetModelAsync` is not thread-safe.
 - [[Issue](https://github.com/OData/RESTier/issues/304)] [[PR](https://github.com/OData/RESTier/pull/322)] Fix a bug that if `GetModelAsync` takes too long to complete, any subsequent request will fail.
 - [[Issue](https://github.com/OData/RESTier/issues/308)] [[PR](https://github.com/OData/RESTier/pull/313)] Fix a bug that `NullReferenceException` is thrown when `ColumnTypeAttribute` does not have a `TypeName` property specified.
 - [[Issue](https://github.com/OData/RESTier/issues/309)][[Issue](https://github.com/OData/RESTier/issues/310)][[Issue](https://github.com/OData/RESTier/issues/311)][[Issue](https://github.com/OData/RESTier/issues/312)] [[PR](https://github.com/OData/RESTier/pull/313)] Fix various bugs in the RESTier query pipeline.