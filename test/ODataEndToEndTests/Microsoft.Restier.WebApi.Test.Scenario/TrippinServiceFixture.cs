// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class TrippinServiceFixture : ServiceFixture
    {
        private const string EigenString = "ODataEndToEndTests";

        private const string ServiceName = "Microsoft.Restier.WebApi.Test.Services.Trippin";

        private static readonly string TrippinWebRoot = GetTrippinWebRoot();

        public TrippinServiceFixture() : base(TrippinWebRoot, 18384)
        {
        }

        private static string GetTrippinWebRoot()
        {
            var codeBase = new Uri(typeof(TrippinServiceFixture).Assembly.CodeBase).LocalPath;
            var parentPathLength = codeBase.IndexOf(EigenString) + EigenString.Length;
            return Path.Combine(codeBase.Substring(0, parentPathLength), ServiceName);
        }
    }
}
