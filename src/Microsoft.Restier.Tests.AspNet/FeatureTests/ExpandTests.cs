using System.Net.Http;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Xunit;

namespace Microsoft.Restier.Tests.AspNet.FeatureTests
{
    public class ExpandTests
    {

        [Fact]
        public async Task CountPlusExpandShouldntThrowExceptions()
        {
            var client = await RestierTestHelpers.GetTestableHttpClient<LibraryApi>();
            var response = await client.ExecuteTestRequest(HttpMethod.Get, resource: "/Publishers?$expand=Books");
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("A Clockwork Orange");
        }

    }
}
