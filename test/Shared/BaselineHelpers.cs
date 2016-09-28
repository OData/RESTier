// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Restier.Tests
{
    public static class BaselineHelpers
    {
        public static async Task<string> GetFormattedContent(HttpResponseMessage response)
        {
            HttpContent content = response.Content;
            if (content == null)
            {
                return null;
            }

            return await content.ReadAsStringAsync();
        }

        public static void VerifyBaseline(string baselinePath, string actualContent)
        {
            string expectedContentPath = GetExpectedContentPath(baselinePath);
            string expectedContent = File.ReadAllText(expectedContentPath);

            if (!string.Equals(expectedContent.Replace("\r\n","").Replace(" ", ""), actualContent.Replace("\r\n", "").Replace(" ", "")))
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

                // TODO GitHubIssue#45 : Improve baseline test
                Assert.True(false, string.Format(
                    "The Response.Content is not correct. \r\nExpected:\r\n{0}\r\n\r\nActual:\r\n{1}\r\n\r\n" +
                        "Run the following command to update the baselines:  \r\nCopy /y {2} {3}\r\n",
                    expectedContent,
                    actualContent,
                    actualContentPath,
                    BaselineHelpers.GetExpectedContentPathInSourceControl(baselinePath)));
            }
        }

        public static void VerifyBaselineDoesNotExist(string baselinePath)
        {
            string expectedContentPath = GetExpectedContentPath(baselinePath);
            Assert.False(File.Exists(expectedContentPath));
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
