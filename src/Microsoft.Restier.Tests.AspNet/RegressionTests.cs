using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Tests.AspNet.Model;
using Xunit;

namespace Microsoft.Restier.Tests.AspNet
{
    public class RegressionTests
    {

        [Fact]
        public async Task GitHub541_CountShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>();
            var response = await client.GetStringAsync("http://localhost/api/test/Readers?$count=true");
            response.Should().Contain("\"@odata.count\":1,");
        }

        [Fact]
        public async Task GitHub542_CountPlusTopShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>();
            var response = await client.GetStringAsync("http://localhost/api/test/Readers?$top=5&$count=true");
            response.Should().Contain("\"@odata.count\":1,");
        }

        [Fact]
        public async Task GitHub542_CountPlusTopPlusFilterShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>();
            var response = await client.GetStringAsync("http://localhost/api/test/Readers?$top=5&$count=true&$filter=FullName eq 'p1'");
            response.Should().Contain("\"@odata.count\":1,");
        }


    }
}