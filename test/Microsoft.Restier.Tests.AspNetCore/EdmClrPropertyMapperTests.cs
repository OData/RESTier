// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.AspNetCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Tests.AspNetCore
{
    public class EdmClrPropertyMapperSampleEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [TestClass]
    public class EdmClrPropertyMapperTests
    {
        [TestMethod]
        public void GetClrPropertyName_WithoutCamelCase_ReturnsEdmName()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<EdmClrPropertyMapperSampleEntity>("Samples");
            var model = builder.GetEdmModel();

            var entityType = model.FindDeclaredType(typeof(EdmClrPropertyMapperSampleEntity).FullName) as IEdmStructuredType;
            var firstNameProperty = entityType.FindProperty("FirstName");

            var result = EdmClrPropertyMapper.GetClrPropertyName(firstNameProperty, model);

            result.Should().Be("FirstName");
        }

        [TestMethod]
        public void GetClrPropertyName_WithCamelCase_ReturnsClrName()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<EdmClrPropertyMapperSampleEntity>("Samples");
            builder.EnableLowerCamelCase();
            var model = builder.GetEdmModel();

            var entityType = model.FindDeclaredType(typeof(EdmClrPropertyMapperSampleEntity).FullName) as IEdmStructuredType;
            var firstNameProperty = entityType.FindProperty("firstName");

            firstNameProperty.Should().NotBeNull("EnableLowerCamelCase should create camelCase EDM property names");

            var result = EdmClrPropertyMapper.GetClrPropertyName(firstNameProperty, model);

            result.Should().Be("FirstName");
        }

        [TestMethod]
        public void GetClrPropertyName_WithCamelCase_KeyProperty_ReturnsClrName()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<EdmClrPropertyMapperSampleEntity>("Samples");
            builder.EnableLowerCamelCase();
            var model = builder.GetEdmModel();

            var entityType = model.FindDeclaredType(typeof(EdmClrPropertyMapperSampleEntity).FullName) as IEdmStructuredType;
            var idProperty = entityType.FindProperty("id");

            idProperty.Should().NotBeNull();

            var result = EdmClrPropertyMapper.GetClrPropertyName(idProperty, model);

            result.Should().Be("Id");
        }
    }
}
