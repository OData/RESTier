using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Validation;
using Microsoft.Restier.Core;
using Microsoft.Restier.EntityFramework.Tests.Models.Library;
using Xunit;

namespace Microsoft.Restier.EntityFramework.Tests
{
    public class ModelBuilderTest
    {
        [Fact]
        public void ComplexTypeShoudWork()
        {
            var model = Domain.GetModelAsync(new LibraryDomain().Context).Result;
            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));
            Console.WriteLine(errors.Count());

            var address = model.FindDeclaredType("Microsoft.Restier.EntityFramework.Tests.Models.Library.Address")
             as IEdmComplexType;
            Assert.NotNull(address);
            Assert.Equal(2, address.Properties().Count());
        }
    }

}
