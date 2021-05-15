// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Restier.Tests.Core.Query
{
    /// <summary>
    /// Unit tests for the <see cref="QueryModelReference" /> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class QueryModelReferenceTests
    {
        /// <summary>
        /// Can get the entity set.
        /// </summary>
        [TestMethod]
        public void CanGetEntitySet()
        {
            var edmEntitySet = new Mock<IEdmEntitySet>().Object;
            var edmType = new Mock<IEdmType>().Object;
            var instance = new QueryModelReference(edmEntitySet, edmType);
            instance.EntitySet.Should().Be(edmEntitySet);
        }

        /// <summary>
        /// Can get the type.
        /// </summary>
        [TestMethod]
        public void CanGetType()
        {
            var edmEntitySet = new Mock<IEdmEntitySet>().Object;
            var edmType = new Mock<IEdmType>().Object;
            var instance = new QueryModelReference(edmEntitySet, edmType);
            instance.Type.Should().Be(edmType);
        }
    }
}