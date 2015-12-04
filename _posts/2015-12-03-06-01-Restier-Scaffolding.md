---
layout: post
title: "6.1 Restier Scaffolding"
description: ""
category: "6. Tooling"
---

### Introduction
This tool is used to modify the config class to simplifies the process of building the OData service with EF by Restier(>=0.4.0-rc) in visual studio. The scaffolding item will appear in the scaffolding list by right click on any folder in project and select "Add" -> "New Scaffolded Item"

### Install Visual Studio Extension of Scaffolding
The installer of Restier scaffolding can be downloaded from Visual Studio Gallery: [Restier Scaffolding](https://visualstudiogallery.msdn.microsoft.com/6b18599d-34d5-4123-a586-cdf411728d23/). Double click vsix to install, the extension supports the VS2013 and VS2015, now.

### Using Scaffolding Tool
[Here](http://odata.github.io/RESTier/#11-40-Bootstrap) is the process of building an OData V4 endpoint using RESTier. With scaffolding tool, you only need to "Create a project and a web app", then "Generate the model classes". The project will looks like:
![]({{site.baseurl}}/images/ScaffoldingBefore.PNG)

1. Right click the APP_Start folder->Add->New Scaffolded items
2. Select "Microsoft OData Restier Config" under Common\Web API node
3. Select the "Data context class" needed and "WebApi config class" which will be modified to add the code as following:
![]({{site.baseurl}}/images/Scaffolding.PNG)
4. Click "Change". Scaffolding tool will add the code in "WebApiConfig.cs". And add Restier assembly as reference
5. Reopen the "WebApiConfig.cs" to view the code added:
![]({{site.baseurl}}/images/ScaffoldingAfter.PNG)
6. Rebuld the project and start:
![]({{site.baseurl}}/images/ScaffoldingResult.PNG)


Notice: The alpha version of tool may contain an issue: during the step 5 and 6, visual studio may need to be restarted. 





