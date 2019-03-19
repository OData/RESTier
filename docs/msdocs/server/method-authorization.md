# Method Authorization

Method Authorization allows you to have fine-grain control over how different types of API requests can be executed.
Since most of RESTier uses built-in convention over repetitive boiler-plate Controllers, you can't just add security attributes
to the controller methods, like you can with Web API.

However, there are two different methods for defining per-request security. One, like the rest of RESTier, is
convention-based, and the other executes before every request, allowing you to centralize your authorization logic.
This allows you to pick the approach that works best for your architecture.

No matter what approach you chose, the concept is simple. Either technique uses a function that returns boolean. 
Return `true`, and processing continues normally. Return `false`, and RESTier returns a 403 Unauthorized to the client.

## Convention-Based Authorization
Users can control if one of the four submit operations is allowed on some EntitySet or Action by putting some 
`protected internal` methods into the `Api` class. The method name must conform to the convention
`Can{Operation}{TargetName}`.

<table style="width: 100%;">
    <tr>
        <td>The possible values for <code>{Operation}</code> are:</td>
        <td>The possible values for <code>{TargetName}</code> are:</td>
    </tr>
    <tr>
        <td>
            <ul style="margin-bottom: 0;">
                <li>Insert</li>
                <li>Update</li>
                <li>Delete</li>
                <li>Execute</li>
            </ul>
        </td>
        <td style="vertical-align: text-top;">
            <ul style="margin-bottom: 0;">
                <li><i>EntitySetName</i></li>
                <li><i>ActionName</i></li>
            </ul>
        </td>
    </tr>
</table>

### Example

The example below demonstrates how both types of `{TargetName}` can be used. 

- The first method shows a simple way to prevent *any*  user from deleting a particular EntitySet.
- The second method shows how you can integrate role-based security using multiple techniques.
- The third method shows how to prevent execution a custom Action.

```cs
using Microsoft.Restier.Providers.EntityFramework;
using System;
using System.Security.Claims;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    ///<summary>
    /// Customizations to the EntityFrameworkApi for the TripPin service.
    ///</summary>
    ///<example>
    /// Add the following line in WebApiConfig.cs to register this code:
    /// await config.MapRestierRoute<TrippinApi>("Trippin", "api", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
    ///</example>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {
        
        ///<summary>
        /// Specifies whether or not a Trip can be deleted from an EntitySet.
        ///</summary>
        protected internal bool CanDeleteTrips()
        {
            return false;
        }

        ///<summary>
        /// User role-based security to specifies whether or not a updated Trip can be sent to an EntitySet.
        ///</summary>
        protected internal bool CanUpdateTrips()
        {
            // Use claims-based security
            return ClaimsPrincipal.Current.IsInRole("admin");

            // You can also use legacy role-based security, though it's harder to test.
            //return HttpContext.Current.User.IsInRole("admin");
        }
        
        ///<summary>
        /// Specifies whether or not an Action called ResetDataSource can be executed through the API.
        ///</summary>
        protected internal bool CanExecuteResetDataSource()
        {
            return false;
        }

    }

}
```

## Centralized Authorization

In addition to the more granular convention-based approach, you can also centralize processing into one location. This is
useful if 

User can use interface `IChangeSetItemAuthorizer` to define any customize authorize logic to see whether user is 
authorized for the specified submit, if this method return false, then the related query will get error code 403 (Forbidden).

There are two steps to plug in the centralized authorization logic.

- Create a class that implements `IChangeSetItemAuthorizer`.
- Register that class with RESTier through Dependency Injection (DI).

### Example

```cs
using Microsoft.OData.Core;
using Microsoft.Restier.Providers.EntityFramework;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    ///<summary>
    /// Provides global ChangeSet Authorization for a RESTier API. 
    ///</summary>
    public class CustomAuthorizer : IChangeSetItemAuthorizer
    {

        ///<summary>
        ///
        ///</summary>
        public Task<bool> AuthorizeAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
	        // TODO: RWM: Provide legitimate samples here, along with parameter documentation.
        }

    }

    ///<summary>
    /// Customizations to the EntityFrameworkApi for the TripPin service.
    ///</summary>
    ///<example>
    /// Add the following line in WebApiConfig.cs to register this code:
    /// await config.MapRestierRoute<TrippinApi>("Trippin", "api", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
    ///</example>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {

        ///<summary>
        /// Allows us to leverage DI to inject additional capabilities into RESTier.
        ///</summary>
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .AddService<IChangeSetItemAuthorizer, CustomizedAuthorizer>();
        }

    }

}
```

## Leveraging Both Techniques

There may be certain situations where you want to have a global interceptor, and then pass requests off to the individual
convention-based interceptors. For example, if you need to authenticate a Bearer token. The example below shows you
exactly how this type of scenario would work.

### Example

```cs
using Microsoft.OData.Core;
using Microsoft.Restier.Providers.EntityFramework;

namespace Microsoft.OData.Service.Sample.Trippin.Api
{

    ///<summary>
    /// Provides global ChangeSet Authorization for a RESTier API. 
    ///</summary>
    public class CustomAuthorizer : IChangeSetItemAuthorizer
    {

        ///<summary>
        /// The built-in ChangeSetItemAuthorizer instance that will be set by RESTier.
        ///<summary>
        private IChangeSetItemAuthorizer InnerAuthorizer {get; set;}

        ///<summary>
        ///
        ///</summary>
        public Task<bool> AuthorizeAsync(SubmitContext context, ChangeSetItem item, CancellationToken cancellationToken)
        {
	        // TODO: RWM: Provide legitimate samples here, along with parameter documentation.

            // Hand off processing to the appropriate convention-based function.
            await InnerAuthorizer.AuthorizeAsync(context, item, cancellationToken);
        }

    }

    ///<summary>
    /// Customizations to the EntityFrameworkApi for the TripPin service.
    ///</summary>
    ///<example>
    /// Add the following line in WebApiConfig.cs to register this code:
    /// await config.MapRestierRoute<TrippinApi>("Trippin", "api", new RestierBatchHandler(GlobalConfiguration.DefaultServer));
    ///</example>
    public class TrippinApi : EntityFrameworkApi<TrippinModel>
    {

        ///<summary>
        /// Allows us to leverage DI to inject additional capabilities into RESTier.
        ///</summary>
        protected override IServiceCollection ConfigureApi(IServiceCollection services)
        {
            return base.ConfigureApi(services)
                .AddService<IChangeSetItemAuthorizer, CustomizedAuthorizer>();
        }

    }

}
```

## Unit Testing Considerations

Because both of these methods are de-coupled from the code that interacts with the database, the Authorization
logic is easily testable, without having to fire up the entire Web API + RESTier pipeline.

### Setting up your Unit Test

If you don't have a unit test project for your API project already, start by creating one. Repeat the process
outlined in "Getting Started" to install the RESTier packages into your Unit Test project. The add the FluentAssertions
package.

Next, go back to your API project. Expand the "Properties" node, double-click AssemblyInfo.cs, and add the following line
to the very end of the file: `[assembly: InternalsVisibleTo("{TestProjectAssembly}")]`, making sure you replace 
{TestProjectAssembly} with the actual assembly name. This is important, because otherwise the tests won't be able to see 
the `protected internal` methods the authorization conventions use.

### Example

Given the [Convention-Based Authorization](#convention-based-authorization) example, the tests below should have 100% code 
coverage, and should pass without any required changes.

```cs
using FluentAssertions;
using Microsoft.OData.Core;
using Microsoft.OData.Service.Sample.Trippin.Api;
using Microsoft.Restier.Providers.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Security.Claims;

namespace Trippin.Tests.Api
{

    /// <summary>
    /// Test cases for the RESTier Method Authorizers.
    /// </summary>
    [TestClass]
    public class TrippinApiTests
    {

        #region Trips EntitySet

        /// <summary>
        /// Tests if the Trips EntitySet is properly configured to reject delete requests.
        /// </summary>
        [TestMethod]
        public void TrippinApi_Trips_CanDelete_IsConfigured()
        {
            var api = new TrippinApi();
            api.CanDeleteTrips.Should().BeFalse();
        }

        /// <summary>
        /// Tests if the Trips EntitySet is properly configured to accept Admin update requests.
        /// </summary>
        [TestMethod]
        public void TrippinApi_Trips_CanUpdate_IsAdmin()
        {
            var api = new TrippinApi();

            // We won't be testing HttpContext-related security here, because that requires mocking,
            // which is outside the scope of this document.
            AuthenticateAsAdmin();
            api.CanUpdateTrips.Should().BeTrue();
        }

        /// <summary>
        /// Tests if the Trips EntitySet is properly configured to reject non-Admin update requests.
        /// </summary>
        [TestMethod]
        public void TrippinApi_Trips_CanUpdate_IsNotAdmin()
        {
            var api = new TrippinApi();
            // We won't be testing HttpContext-related security here, because that requires mocking,
            // which is outside the scope of this document.
            AuthenticateAsNonAdmin();
            api.CanUpdateTrips.Should().BeFalse();
        }

        #endregion

        #region Actions

        /// <summary>
        /// Tests if the Trips EntitySet is properly configured to reject delete requests.
        /// </summary>
        [TestMethod]
        public void TrippinApi_CanExecuteResetDataSource_IsConfigured()
        {
            var api = new TrippinApi();
            api.CanExecuteResetDataSource.Should().BeFalse();
        }

        #endregion

        #region Test Helpers

        /// <summary>
        /// Sets the Thread.CurrentPrincipal to a test user with an "admin" Role Claim.
        /// </summary>
        internal static void AuthenticateAsAdmin()
        {
            var claimsCollection = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "admin")
            };
            var claimsIdentity = new ClaimsIdentity(claimsCollection, "Test User");
            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
        }

        /// <summary>
        /// Sets the Thread.CurrentPrincipal to a test user without an "admin" Role Claim.
        /// </summary>
        internal static void AuthenticateAsNonAdmin()
        {
            var claimsCollection = new List<Claim>();
            var claimsIdentity = new ClaimsIdentity(claimsCollection, "Test User");
            Thread.CurrentPrincipal = new ClaimsPrincipal(claimsIdentity);
        }

        #endregion

    }

}

```