# RESTier
<img src="https://identitydivision.visualstudio.com/_apis/public/build/definitions/2cfe7ec3-b94f-4ab9-85ab-2ebff928f3fd/100/badge"/>

## 1. Introduction
[OData](http://www.odata.org/ "OData") stands for the Open Data Protocol. It was initiated by Microsoft and is now an ISO and OASIS standard. OData enables the creation and consumption of RESTful APIs, which allow resources, defined in a data model and identified by using URLs, to be published and edited by Web clients using simple HTTP requests.

RESTier is a RESTful API development framework for building standardized, OData V4 based RESTful services on .NET platform. It can be seen as a middle-ware on top of Web API OData. RESTier provides facilities to bootstrap an OData service like what WCF Data Services (which is sunset) does, beside this, it supports to add business logic in several simple steps, has flexibility and easy customization like what Web API OData do. It also supports to add additional publishers to support other protocols and additional providers to support other data sources.

For more information about OData, please refer to the following resources:
- [OData.org](http://www.odata.org/)
- [OASIS Open Data Protocol (OData) Technical Committee](https://www.oasis-open.org/committees/tc_home.php?wg_abbrev=odata)

**For how to adopt this library to build OData service, please refer to the following resources:**
- [Build an OData v4 Service with RESTier Library](http://odata.github.io/RESTier/#01-01-Introduction)

**For how to adopt .NET OData Client to consume OData service, please refer to the following resources:**
- [OData .Net Client](http://odata.github.io/odata.net/#04-01-basic-crud-operations)

Please be noted that currently RESTier is still a preview version.

## 2. Project structure
The project currently has two branches: [master](https://github.com/OData/RESTier/tree/master), [gh-pages](https://github.com/OData/RESTier/tree/gh-pages).

**master branch**

The master branch has the following libraries:
- [RESTier Core](https://www.nuget.org/packages/Microsoft.Restier.Core/) (namespace `Microsoft.Restier.Core`):<br />The RESTier Core contains framework classes like API-related logic, query inspector/filter/expander/sourcer/executor, convention-based logic like model builder.
- [RESTier OData Publisher](https://www.nuget.org/packages/Microsoft.Restier.Publishers.Odata/) (namespace `Microsoft.Restier.Publishers.Odata`):<br />The RESTier OData Publisher contains classes to publish the data source as an OData service based on Web API OData.
- [RESTier EntityFramework Provider](https://www.nuget.org/packages/Microsoft.Restier.Providers.EntityFramework/) (namespace `Microsoft.Restier.Providers.EntityFramework`):<br />The RESTier EntityFramework Provider contains classes to access data sources exposed with Entity Framework library.
- [RESTier Security](https://www.nuget.org/packages/Microsoft.Restier.Security/) (namespace `Microsoft.Restier.Security`):<br />The RESTier Security contains classes and methods for security control, it is not in active development state and will not be part of first GA release.

For these libraries, we accept bug reports, feature requirements and pull requests. 


**gh-pages branch**

The gh-pages branch contains documentation source for RESTier - tutorials, guides, etc.  The documentation source is in Markdown format. It is hosted at [RESTier Pages](http://odata.github.io/RESTier "RESTier Pages").

## 3. Building, Testing, Debugging and Release
LocalDB v12.0 or above will be used which is part of VS2015 and no additional installation is needed. The Database will be automatically initialized by the test code if it doesn't exist.

### 3.1 Building and Testing in Visual Studio
Simply open the solution files in root folder and build them in Visual Studio 2015.

Here is the usage of each solution file:
- RESTier.sln - Product source and all tests. It uses EntityFramework 6.x and built with .Net Framework version 4.5.1.
- RESTier.EF7.sln - Product source and all tests. It uses EntityFramework 7.x and built with .Net Framework version 4.5.1.

### 3.2 One-click build in command line
Open Command Line Window, cd to the root folder and run following command:

```
build.cmd
```

The build will take about 4 minutes. Tests are recommended to run with Visual Studio.

### 3.3 Debug
Please refer to the [How to debug](http://odata.github.io/WebApi/10-01-debug-webapi-source).

### 3.4 Official Release
The release of the component binaries is carried out regularly through [Nuget](http://www.nuget.org/).

## 4. Documentation
Please visit the [RESTier pages](http://odata.github.io/RESTier). It has detailed descriptions on each feature provided by RESTier.

## 5. Sample services
Refer to [sample service github](https://github.com/OData/ODataSamples/tree/master/RESTier) for end to end sample service. The source code also contains end to end service for end to end test purpose. All the sample service can be run with visual studio 2015.

## 6. Community
### 6.1 Contribution
There are many ways for you to contribute to RESTier. The easiest way is to participate in discussion of features and issues. You can also contribute by sending pull requests of features or bug fixes to us. Contribution to the documentations is also highly welcomed. Please refer to the [CONTRIBUTING.md](https://github.com/OData/RESTier/blob/master/.github/CONTRIBUTING.md) for more details.

### 6.2 Support
- Issues<br />Report issues on [Github issues](https://github.com/OData/RESTier/issues).
- Questions<br />Ask questions on [Stack Overflow](http://stackoverflow.com/questions/ask?tags=odata).
- Feedback<br />Please send mails to [odatafeedback@microsoft.com](mailto:odatafeedback@microsoft.com).
- Team blog<br />Please visit [http://blogs.msdn.com/b/odatateam/](http://blogs.msdn.com/b/odatateam/) and [http://www.odata.org/blog/](http://www.odata.org/blog/).

## Thank You!

We’re using NDepend to analyze and increase code quality.

[![NDepend](images/ndependlogo.png)](http://www.ndepend.com)

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
