using Microsoft.Restier.Core.Submit;
using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class NamingTests
    {

        [Fact]
        public void ChangeSetItemFilter_CorrectPluralization()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), DataModificationItemAction.Insert, null, null, null);
            var name = ConventionBasedChangeSetItemFilter.GetMethodName(item, ConventionBasedChangeSetConstants.FilterMethodNamePreFilterSuffix);
            Assert.Equal("OnInsertingString", name);
        }

        [Fact]
        public void ChangeSetItemFilter_IncorrectPluralization()
        {
            var item = new DataModificationItem("TestItems", typeof(string), typeof(string), DataModificationItemAction.Insert, null, null, null);
            var name = ConventionBasedChangeSetItemFilter.GetMethodName(item, ConventionBasedChangeSetConstants.FilterMethodNamePreFilterSuffix, true);
            Assert.Equal("OnInsertingTestItems", name);
        }

    }
}
