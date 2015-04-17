---
layout: post
title: "4.1 RESTier infrastructure"
description: ""
category: "4. Deep in RESTier"
---

![]({{site.baseurl}}/images/04-01-01-RESTier Architecture.png)

Restier provides a connection between various data sources and existing clients. The framework contains 4 components: Core, Module, Provider and Publisher:
 * The core component provides functionalities for building up domain specific metadata, and logic for data CRUD processing flow. It also includes some extensible interfaces which allows pluggable modules.
 * The module component provides the common service elements such as authorization, logging, and conventions that allow users to set up a service more quickly.
 * The provider component includes data source adapters which provide functionalities for building up metadata and conduct data exchange with external data sources.
 * The publisher component provides functionalities for exposing the domain specific data via a new service interface, which could be understand by existing clients.
