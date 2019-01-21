// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using CloudNimble.Breakdance.Restier;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Validation;
using Microsoft.Restier.Tests.Shared;
using Microsoft.Restier.Tests.Shared.Scenarios.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNet.Model
{

    [TestClass]
    public class RestierModelBuilderTests : RestierTestBase
    {
        [TestMethod]
        public async Task ComplexTypeShoudWork()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<LibraryApi>();

            model.Validate(out var errors).Should().BeTrue();
            errors.Should().BeEmpty();

            var address = model.FindDeclaredType("Microsoft.Restier.Tests.Shared.Scenarios.Library.Address") as IEdmComplexType;
            address.Should().NotBeNull();
            address.Properties().Should().HaveCount(2);
        }

        [TestMethod]
        public async Task PrimitiveTypesShouldWork()
        {
            var model = await RestierTestHelpers.GetTestableModelAsync<LibraryApi>();

            model.Validate(out var errors).Should().BeTrue();
            errors.Should().BeEmpty();

            var universe = model.FindDeclaredType("Microsoft.Restier.Tests.Shared.Scenarios.Library.Universe")
             as IEdmComplexType;
            universe.Should().NotBeNull();

            var propertyArray = universe.Properties().ToArray();
            var i = 0;
            propertyArray[i++].Type.AsPrimitive().IsBinary().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsBoolean().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsByte().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDate().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDateTimeOffset().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDecimal().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDouble().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsDuration().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsGuid().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsInt16().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsInt32().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsInt64().Should().BeTrue();
            // propertyArray[i++].Type.AsPrimitive().IsSByte().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsSingle().Should().BeTrue();
            // propertyArray[i++].Type.AsPrimitive().IsStream().Should().BeTrue();
            propertyArray[i++].Type.AsPrimitive().IsString().Should().BeTrue();
            // propertyArray[i].Type.AsPrimitive().IsTimeOfDay().Should().BeTrue();
        }
    }
}
