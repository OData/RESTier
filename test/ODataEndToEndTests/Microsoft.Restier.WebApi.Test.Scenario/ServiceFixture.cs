// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Restier.WebApi.Test.Scenario
{
    public class ServiceFixture : IDisposable
    {
        private const string IISExpressProcessName = "iisexpress";

        private static readonly string IISExpressPath =
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%\IIS Express\iisexpress.exe");

        private static Process iisExpressInstance;
                
        public string WebRoot { get; private set; }

        public int Port { get; private set; }

        static ServiceFixture()
        {
            KillServices();
        }

        public ServiceFixture(string webRoot, int port)
        {
            WebRoot = webRoot;
            Port = port;

            StartService();
        }

        public void Dispose()
        {
            //KillService();
        }
        
        private void StartService()
        {
            if (iisExpressInstance == null)
            {
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = IISExpressPath,
                    Arguments = string.Format("/path:\"{0}\" /port:{1}", WebRoot, Port)
                };

                iisExpressInstance = Process.Start(startInfo);
            }
        }

        private void KillService()
        {
            if (iisExpressInstance != null)
            {
                iisExpressInstance.Kill();
            }
        }

        private static void KillServices()
        {
            var processes = Process.GetProcessesByName(IISExpressProcessName);
            foreach (var process in processes)
            {
                process.Kill();
            }
        }
    }
}
