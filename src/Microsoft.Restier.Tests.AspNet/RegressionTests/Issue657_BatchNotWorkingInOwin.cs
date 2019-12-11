using System;
using System.Web.Http;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.RegressionTests
{

    /// <summary>
    /// Regression tests for https://github.com/OData/RESTier/issues/541.
    /// </summary>
    [TestClass]
    public class Issue657_BatchNotWorkingInOwin : RestierTestBase
    {

        [Ignore]
        [TestMethod]
        public void MapRestier_ThrowsExceptionOnOwinSelfHost()
        {
            //RWM: Need a way to make this test work.
            var config = new HttpConfiguration();
            Action mapRestier = () => { config.MapRestier<LibraryApi>("Restier", "v1/"); };
            mapRestier.Should().Throw<Exception>().WithMessage("*MapRestier*");
        }

        [TestMethod]
        public void MapRestier_ThrowsExceptionOnNullHttpServer()
        {
            var config = new HttpConfiguration();
            Action mapRestier = () => { config.MapRestier<LibraryApi>("Restier", "v1/", true, null); };
            mapRestier.Should().Throw<Exception>().WithMessage("*MapRestier*");
        }

    }

}