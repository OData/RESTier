---
layout: post
title: "Features supported in RESTier preview"
description: ""
category: Announcements
---

Below are the features supported in the RESTier preview, as well as the limitations of the current version.

### Easily build an OData V4 service

**Features directly supported**

Just create one `ODataDomainController<>` and all of the features below are automatically enabled:

 - Basic queries for metadata and top level entities.
 - System query options `$select`, `$expand`, `$filter`, `$orderby`, `$top`,
  `$skip`, and `$format`.
 - Ability to request related entities.
 - Create, Update and Delete top-level entities.
 - Batch requests.

**Leverage attribute routing to fall back to [Web API OData](http://www.asp.net/web-api/overview/odata-support-in-aspnet-web-api/odata-v4/create-an-odata-v4-endpoint) for features not directly supported by RESTier**

 - Request entity references with `$ref`.
 - Create, Update and Delete entities not on the top-level.
 - Modify relationships between entities.
 - etc.

**Use `EdmModelExtender` to support features currently not directly supported by RESTier.** 

 - OData functions.
 - OData actions

### Rich domain logic

 - Role-based security

    You can easily set restrictions for different entity sets. For example, you can provide users with READ permission on some entity sets, and INSPECT (only provides access to $metadata) on others.

 - Imperative views 

   Customized entity sets which are not in the data model can be easily added. Currently, these entity sets are read-only, and do not support CUD (Create, Update, Delete) operations.

 - Entity set filters

    With entity set filters, you can easily set filters *before* entity data is retrieved. For example, if you want users to only see part of Customers based on their UserID, you can use entity set filters to pre-filter the results.

 - Submit logic

    With submit logic, you can add custom business logic that fires during or after a specific operation is performed on an entity set (e.g., `OnInsertedProducts`).

### Limitations

- Only supports OData V4.
- Only supports Entity Framework as data providers.

These are the two primary limitations currently, and we are looking at mitigating them in future releases. In the meanwhile, we'd like to hear your feedback and suggestions on how to improve RESTier.