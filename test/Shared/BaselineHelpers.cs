// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
