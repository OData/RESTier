using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Data.Domain.Tests
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

                // TODO:  Figure out a better update baseline experience.  Currently the Test Explorer doesn't let you select
                // individual items from the test output.
                Assert.Fail(
                    "The Response.Content is not correct. \r\nExpected:\r\n{0}\r\n\r\nActual:\r\n{1}\r\n\r\n" +
                        "Run the following command to update the baselines:  \r\nCopy /y {2} {3}\r\n",
                    expectedContent,
                    actualContent,
                    actualContentPath,
                    BaselineHelpers.GetExpectedContentPathInSourceControl(baselinePath));
            }
        }

        public static void VerifyBaselineDoesNotExist(string baselinePath)
        {
            string expectedContentPath = GetExpectedContentPath(baselinePath);
            Assert.IsFalse(File.Exists(expectedContentPath));
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
