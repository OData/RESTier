using System;

namespace Microsoft.Restier.EntityFramework.Tests.Models.Library
{
    class Person
    {
        public Guid Id { get; set; }

        public string FullName { get; set; }

        public Address Addr { get; set; }
    }
}
