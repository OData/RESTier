// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Restier.TestCommon
{
    public class PublicApiTests
    {
        private const string BaselineFileName = "PublicApi.bsl";
        private const string OutputFileName = "PublicApi.out";

        [Fact]
        public void PublicApiTest()
        {
            string[] assemblyList =
            {
                "Microsoft.Restier.Core.dll",
                "Microsoft.Restier.Providers.EntityFramework.dll",
                "Microsoft.Restier.Publishers.OData.dll",
            };

            using (var fs = new FileStream(OutputFileName, FileMode.Create))
            {
                using (var sw = new StreamWriter(fs))
                {
                    Console.SetOut(sw);
                    new PublicApiDump().DumpApi(assemblyList);
                }
            }

            var baselineString = File.ReadAllText(BaselineFileName);
            var outputString = File.ReadAllText(OutputFileName);
            Assert.True(baselineString == outputString,
                "Public API changes detected. Please update " + BaselineFileName);
        }
    }
}
