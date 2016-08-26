// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Validation;
using Microsoft.Restier.Core;
using Xunit;

namespace Microsoft.Restier.Publishers.OData.Test.Model
{
    public class RestierModelBuilderTests
    {
        [Fact]
        public void ComplexTypeShoudWork()
        {
            var container = new RestierContainerBuilder(typeof(LibraryApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();

            var model = api.GetModelAsync().Result;

            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));
            Assert.Empty(errors);

            var address = model.FindDeclaredType("Microsoft.Restier.Publishers.OData.Test.Model.Address")
             as IEdmComplexType;
            Assert.NotNull(address);
            Assert.Equal(2, address.Properties().Count());
        }

        [Fact]
        public void PrimitiveTypesShouldWork()
        {
            var container = new RestierContainerBuilder(typeof(LibraryApi));
            var provider = container.BuildContainer();
            var api = provider.GetService<ApiBase>();
            var model = api.GetModelAsync().Result;

            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));
            Assert.Empty(errors);

            var universe = model.FindDeclaredType("Microsoft.Restier.Publishers.OData.Test.Model.Universe")
             as IEdmComplexType;
            Assert.NotNull(universe);

            var propertyArray = universe.Properties().ToArray();
            int i = 0;
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsBinary());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsBoolean());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsByte());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsDate());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsDateTimeOffset());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsDecimal());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsDouble());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsDuration());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsGuid());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsInt16());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsInt32());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsInt64());
            // Assert.True(propertyArray[i++].Type.AsPrimitive().IsSByte());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsSingle());
            // Assert.True(propertyArray[i++].Type.AsPrimitive().IsStream());
            Assert.True(propertyArray[i++].Type.AsPrimitive().IsString());
            // Assert.True(propertyArray[i].Type.AsPrimitive().IsTimeOfDay());
        }
    }
}
