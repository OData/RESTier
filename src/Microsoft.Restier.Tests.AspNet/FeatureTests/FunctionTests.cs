using System;
using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{

    [TestClass]
    public class FunctionTests : RestierTestBase
    {

        //[Ignore]
        [TestMethod]
        public async Task FunctionParameters_BooleanParameter ()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/PublishBook(IsActive=true)");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("in the Hat");
        }

        //[Ignore]
        [TestMethod]
        public async Task FunctionParameters_IntParameter()
        {
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: "/PublishBooks(Count=5)");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain("Comes Back");
        }

        //[Ignore]
        [TestMethod]
        public async Task FunctionParameters_GuidParameter()
        {
            var testGuid = Guid.NewGuid();
            var response = await RestierTestHelpers.ExecuteTestRequest<LibraryApi>(HttpMethod.Get, resource: $"/SubmitTransaction(Id={testGuid})");
            var content = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine(content);
            response.IsSuccessStatusCode.Should().BeTrue();
            content.Should().Contain(testGuid.ToString());
            content.Should().Contain("Shrugged");
        }

    }

}