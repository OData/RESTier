using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.OData.Service.Sample.Northwind.Models
{
    internal class SqlLoader
    {
        private static readonly string[] possibleSqlCmdExePaths =
        {
            @"%ProgramFiles%\Microsoft SQL Server\110\Tools\Binn\SQLCMD.EXE",
            @"%ProgramW6432%\Microsoft SQL Server\110\Tools\Binn\SQLCMD.EXE",
            @"%ProgramFiles%\Microsoft SQL Server\120\Tools\Binn\SQLCMD.EXE",
            @"%ProgramW6432%\Microsoft SQL Server\120\Tools\Binn\SQLCMD.EXE"
        };

        private readonly IList<KeyValuePair<string, string>> _options = new List<KeyValuePair<string, string>>();

        public void SetDatabaseEngine(string dbName)
        {
            AddOption("-S", dbName);
        }

        public void AddScriptArgument(string argName, string argValue)
        {
            AddOption("-v", string.Format("{0}=\"{1}\"", argName, argValue));
        }

        public void AddScript(string scriptName)
        {
            AddOption("-i", scriptName);
        }

        public void Reset()
        {
            _options.Clear();
        }

        public void Execute(string workingDirectory)
        {
            var sqlCmdExePath = possibleSqlCmdExePaths.Select(Environment.ExpandEnvironmentVariables).First(File.Exists);

            var argumentStringBuilder = new StringBuilder();
            foreach (var option in _options)
            {
                argumentStringBuilder.Append(string.Format("{0} {1} ", option.Key, option.Value));
            }

            var start = new ProcessStartInfo()
            {
                FileName = sqlCmdExePath,
                WorkingDirectory = workingDirectory,
                Arguments = argumentStringBuilder.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(start).WaitForExit();
        }

        public static string GetDatabaseDirectory(string subDir)
        {
            var dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
            if (string.IsNullOrEmpty(dataDirectory))
            {
                dataDirectory = Environment.CurrentDirectory;
            }

            if (!string.IsNullOrEmpty(subDir))
            {
                dataDirectory = Path.Combine(dataDirectory, subDir);
            }

            return AddTrailingSlash(dataDirectory);
        }

        private void AddOption(string key, string value)
        {
            _options.Add(new KeyValuePair<string, string>(key, value));
        }

        private static string AddTrailingSlash(string path)
        {
            if (path.EndsWith("\\"))
            {
                return path;
            }

            return path + "\\";
        }
    }
}