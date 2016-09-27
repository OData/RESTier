// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TrippinE2EOnFilterTestCases : TrippinE2ETestBase
    {
        [Fact]
        public void TestFilteredEntity()
        {
            TestGetStatusCodeIs("Staffs(1)", 404);
        }

        [Theory]
        // Path segment cases
        [InlineData("Staffs", "OnfilterPathSeg1")]
        [InlineData("Staffs(2)", "OnfilterPathSeg3")]
        [InlineData("Staffs(2)/PeerStaffs", "OnfilterPathSeg4")]
        [InlineData("Staffs(2)/Conferences", "OnfilterPathSeg5")]
        [InlineData("Staffs/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff", "OnfilterPathSeg6")]
        // Simple expand cases
        [InlineData("Staffs?$expand=PeerStaffs", "OnfilterSimpleExpand1")]
        [InlineData("Staffs?$expand=Conferences", "OnfilterSimpleExpand2")]
        [InlineData("Staffs?$expand=PeerStaffs,Conferences", "OnfilterSimpleExpand3")]
        // Navigation property with expand clause cases
        [InlineData("Staffs(2)/PeerStaffs?$expand=PeerStaffs", "OnFilterPathExpand1")]
        [InlineData("Staffs(2)/PeerStaffs?$expand=Conferences", "OnFilterPathExpand2")]
        [InlineData("Staffs(2)/PeerStaffs?$expand=PeerStaffs,Conferences", "OnFilterPathExpand3")]
        [InlineData("Staffs(2)/Conferences?$expand=Sponsors", "OnFilterPathExpand4")]
        //Type cast in path with expand clause cases
        [InlineData("Staffs/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff?$expand=PeerStaffs", "OnFilterCastPath1")]
        [InlineData("Staffs/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff?$expand=Conferences", "OnFilterCastPath2")]
        [InlineData("Staffs/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff?$expand=PeerSeniorStaffs", "OnFilterCastPath3")]
        [InlineData("Staffs/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff?$expand=HighEndConferences", "OnFilterCastPath4")]
        [InlineData("Staffs/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff?$expand=PeerStaffs, Conferences, PeerSeniorStaffs, HighEndConferences", "OnFilterCastPath5")]
        [InlineData("Staffs(2)/Conferences/Microsoft.OData.Service.Sample.Trippin.Models.HighEndConference?$expand=Sponsors, GlodSponsors", "OnFilterCastPath6")]
        // Type cast in expand clause cases
        [InlineData("Staffs?$expand=Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerStaffs", "OnFilterCastExpand1")]
        [InlineData("Staffs?$expand=Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/Conferences", "OnFilterCastExpand2")]
        [InlineData("Staffs?$expand=Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerSeniorStaffs", "OnFilterCastExpand3")]
        [InlineData("Staffs?$expand=Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/HighEndConferences", "OnFilterCastExpand4")]
        [InlineData("Staffs?$expand=PeerStaffs, Conferences, Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerSeniorStaffs, Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/HighEndConferences", "OnFilterCastExpand5")]
        // Type cast in path property with expand clause cases
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerStaffs?$expand=PeerStaffs", "OnFilterCastPropExpand1")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerStaffs?$expand=Conferences", "OnFilterCastPropExpand2")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerSeniorStaffs?$expand=PeerStaffs", "OnFilterCastPropExpand3")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerSeniorStaffs?$expand=Conferences", "OnFilterCastPropExpand4")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerSeniorStaffs?$expand=PeerSeniorStaffs", "OnFilterCastPropExpand5")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/PeerSeniorStaffs?$expand=HighEndConferences", "OnFilterCastPropExpand6")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/Conferences?$expand=Sponsors", "OnFilterCastPropExpand7")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/HighEndConferences?$expand=Sponsors", "OnFilterCastPropExpand8")]
        [InlineData("Staffs(6)/Microsoft.OData.Service.Sample.Trippin.Models.SeniorStaff/HighEndConferences?$expand=GlodSponsors", "OnFilterCastPropExpand9")]
        // Query options with onfilter case
        // These two are place holder, need new version of ODL
        //[InlineData("Staffs?$expand=PeerStaffs/$count ln 5", "OnFilterQueryOption1")]
        //[InlineData("Staffs?$expand=Conferences/$count ln 5", "OnFilterQueryOption2")]
        // Staff with FirstName 'Ronald' is filtered, so the result is empty
        [InlineData("Staffs?$filter=PeerStaffs/any(d:d/FirstName eq 'Ronald')&$expand=PeerStaffs", "OnFilterQueryOption3")]
        [InlineData("Staffs?$filter=PeerStaffs/any(d:d/FirstName eq 'Vincent')&$expand=Conferences", "OnFilterQueryOption4")]
        // Conference with Name 'conference4' is filtered, so the result is empty
        [InlineData("Staffs?$filter=Conferences/any(d:d/Name eq 'conference4')&$expand=PeerStaffs", "OnFilterQueryOption5")]
        [InlineData("Staffs?$filter=Conferences/any(d:d/Name eq 'conference5')&$expand=PeerStaffs", "OnFilterQueryOption6")]
        // Nested Expand case
        [InlineData("Staffs?$expand=PeerStaffs($expand=Conferences)", "OnfilterNestedExpand1")]
        [InlineData("Staffs?$expand=Conferences($expand=Sponsors)", "OnfilterNestedExpand2")]
        public void OnFilterQueryTest(string uriStringAfterServiceRoot, string baselineFileName)
        {
            Action<string> validationAction = content => VerifyBaseline(baselineFileName, content);
            this.TestGetPayload(uriStringAfterServiceRoot, validationAction);
        }

        private static void VerifyBaseline(string baselinePath, string actualContent)
        {
            string expectedContentPath = GetExpectedContentPath(baselinePath);
            string expectedContent = null;
            try
            {
                expectedContent = File.ReadAllText(expectedContentPath);
            }
            catch (Exception)
            {
                // Some file does not exist as the expected content is empty
            }

            if (!string.Equals(expectedContent, actualContent))
            {
                string actualContentRootFolder = Path.GetFullPath("ActualBaselines");
                string actualContentPath = Path.Combine(actualContentRootFolder, baselinePath + ".txt");

                // Recompute the actual folder in case baselinePath contained directories.
                string actualContentCompleteFolder = Path.GetDirectoryName(actualContentPath);
                if (!Directory.Exists(actualContentCompleteFolder))
                {
                    Directory.CreateDirectory(actualContentCompleteFolder);
                }

                File.WriteAllText(actualContentPath, actualContent);

                Assert.True(false, string.Format(
                    "The Response.Content is not correct. \r\nExpected:\r\n{0}\r\n\r\nActual:\r\n{1}\r\n\r\n" +
                        "Run the following command to update the baselines:  \r\nCopy /y {2} {3}\r\n",
                    expectedContent,
                    actualContent,
                    actualContentPath,
                    GetExpectedContentPathInSourceControl(baselinePath)));
            }
        }

        private static string GetExpectedContentPath(string baselinePath)
        {
            return Path.GetFullPath(
                Path.Combine("Baselines", baselinePath + ".txt"));
        }

        private static string GetExpectedContentPathInSourceControl(string baselinePath)
        {
            return Path.GetFullPath(
                Path.Combine("..\\..\\Baselines", baselinePath + ".txt"));
        }
    }
}
