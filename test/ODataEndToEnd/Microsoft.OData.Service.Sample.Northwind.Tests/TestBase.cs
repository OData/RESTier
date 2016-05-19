using System;
using System.IO;

namespace Microsoft.OData.Service.Sample.Northwind.Tests
{
    public class TestBase
    {
        public TestBase()
        {
            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(Directory.GetCurrentDirectory(), "App_Data"));
        }
    }
}
