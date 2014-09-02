using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NorthwindService.Tests
{
    [TestClass]
    public class AssemblyTests
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            // The app.config file contains connections strings that are in reference to the DataDirectory, therefore the
            // data directory needs to be initialized since the tests are not running within ASP.NET
            // (http://msdn.microsoft.com/en-us/library/vstudio/cc716756(v=vs.100).aspx).  The use of 
            // DataDirectory avoids the need of an absolute path within the config file.
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(Directory.GetCurrentDirectory(), "App_Data"));
        }
    }
}
