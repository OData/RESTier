RESTier
===============

## What is RESTier
RESTier is a RESTful API development framework for building standardized, OData V4 based REST services on .NET. It can be seen as a middle-ware on top of Web API OData. RESTier can provide convenience to bootstrap an OData service and add business logic like what WCF Data Services does as well as flexibily and easy customization like what Web API OData does.

Please be noted that currently RESTier is still a preview version.

## How to build

You can build the project either from command line with ./build.cmd, or from Visual Studio.

If you see strong name validation error when running tests or sample services, you may need to skip strong name validation for RESTier DLLs.

To enable skip strong name validation. please the try following command:

> .\build.cmd EnableSkipStrongNames

To disable it, please try:

> .\build.cmd DisableSkipStrongNames

## Documentation / Tutorials

Please refer to the [RESTier pages](http://odata.github.io/RESTier/).

## Call to action

RESTier is fully open sourced (the source code will be opened pretty soon), please following the [contribution guide](https://github.com/OData/RESTier/wiki/Contribute-to-RESTier) to contribute / provide feedback to RESTier. 
