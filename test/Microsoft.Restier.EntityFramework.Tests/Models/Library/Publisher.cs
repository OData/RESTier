using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Restier.EntityFramework.Tests.Models.Library
{
    class Publisher
    {
        public string Id { get; set; }

        public Address Addr { get; set; }

        public virtual ICollection<Book> Books { get; set; }
    }
}
