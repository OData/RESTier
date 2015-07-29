// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class TrippinServiceFixture
    {
        private const string EigenString = "ODataEndToEndTests";

        private const string ServiceName = "Microsoft.Restier.WebApi.Test.Services.Trippin";

        private const string IISExpressProcessName = "iisexpress";

        private const int TrippinPort = 18384;

        private static readonly string TrippinWebRoot = GetTrippinWebRoot();

        private static readonly string IISExpressPath = GetIISExpressPath();

        static TrippinServiceFixture()
        {
            KillServices();
            StartService();
        }

        private static string GetTrippinWebRoot()
        {
            var codeBase = new Uri(typeof(TrippinServiceFixture).Assembly.CodeBase).LocalPath;
            var parentPathLength = codeBase.IndexOf(EigenString) + EigenString.Length;
            return Path.Combine(codeBase.Substring(0, parentPathLength), ServiceName);
        }

        private static string GetIISExpressPath()
        {
            return Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\IIS Express\iisexpress.exe");
        }

        private static void KillServices()
        {
            var processes = Process.GetProcessesByName(IISExpressProcessName);
            foreach (var process in processes)
            {
                process.Kill();
                process.WaitForExit();
            }
        }

        private static void StartService()
        {
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = IISExpressPath,
                Arguments = string.Format("/path:\"{0}\" /port:{1}", TrippinWebRoot, TrippinPort)
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start Trippin service");
            }
        }
    }
}
