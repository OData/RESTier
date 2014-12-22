// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Restier.Core.Tests
{
    [TestClass]
    public class PropertyBagTests
    {
        [TestMethod]
        public void PropertyBagRepresentsPropertiesCorrectly()
        {
            var propertyBag = new PropertyBag();

            Assert.IsFalse(propertyBag.HasProperty("Test"));
            Assert.IsNull(propertyBag.GetProperty("Test"));
            Assert.IsNull(propertyBag.GetProperty<string>("Test"));
            Assert.AreEqual(default(int), propertyBag.GetProperty<int>("Test"));

            propertyBag.SetProperty("Test", "Test");
            Assert.IsTrue(propertyBag.HasProperty("Test"));
            Assert.AreEqual("Test", propertyBag.GetProperty("Test"));
            Assert.AreEqual("Test", propertyBag.GetProperty<string>("Test"));

            propertyBag.ClearProperty("Test");
            Assert.IsFalse(propertyBag.HasProperty("Test"));
            Assert.IsNull(propertyBag.GetProperty("Test"));
            Assert.IsNull(propertyBag.GetProperty<string>("Test"));
            Assert.AreEqual(default(int), propertyBag.GetProperty<int>("Test"));
        }
    }
}
