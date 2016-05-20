// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.OData.Service.Sample.Tests
{
    public class TrippinServiceFixture
    {
        private const string EigenString = "ODataEndToEnd";

        private const string IISExpressProcessName = "iisexpress";

        private static readonly string IISExpressPath =
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\IIS Express\iisexpress.exe");

        private static readonly Dictionary<string, int> Services = new Dictionary<string, int>
        {
            {"Microsoft.OData.Service.Sample.Trippin"           , 18384 },
            {"Microsoft.OData.Service.Sample.TrippinInMemory"   , 21248 }
        };

        static TrippinServiceFixture()
        {
            KillServices();
            foreach (var service in Services)
            {
                StartService(service.Key, service.Value);
                System.Threading.Thread.Sleep(2000);
            }
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

        private static void StartService(string serviceName, int port)
        {
            string root = GetTrippinWebRoot(serviceName);
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = IISExpressPath,
                Arguments = string.Format("/path:\"{0}\" /port:{1}", root, port)
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start service:" + serviceName);
            }
        }

        private static string GetTrippinWebRoot(string serviceName)
        {
            var codeBase = new Uri(typeof(TrippinServiceFixture).Assembly.CodeBase).LocalPath;
            var parentPathLength = codeBase.IndexOf(EigenString) + EigenString.Length;
            return Path.Combine(codeBase.Substring(0, parentPathLength), serviceName);
        }
    }
}
