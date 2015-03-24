// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Restier.Core.Tests
{
    public class PropertyBagTests
    {
        [Fact]
        public void PropertyBagRepresentsPropertiesCorrectly()
        {
            var propertyBag = new PropertyBag();

            Assert.False(propertyBag.HasProperty("Test"));
            Assert.Null(propertyBag.GetProperty("Test"));
            Assert.Null(propertyBag.GetProperty<string>("Test"));
            Assert.Equal(default(int), propertyBag.GetProperty<int>("Test"));

            propertyBag.SetProperty("Test", "Test");
            Assert.True(propertyBag.HasProperty("Test"));
            Assert.Equal("Test", propertyBag.GetProperty("Test"));
            Assert.Equal("Test", propertyBag.GetProperty<string>("Test"));

            propertyBag.ClearProperty("Test");
            Assert.False(propertyBag.HasProperty("Test"));
            Assert.Null(propertyBag.GetProperty("Test"));
            Assert.Null(propertyBag.GetProperty<string>("Test"));
            Assert.Equal(default(int), propertyBag.GetProperty<int>("Test"));
        }
    }
}
