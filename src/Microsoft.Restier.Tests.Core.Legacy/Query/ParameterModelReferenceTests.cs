// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core.Query
{
    using System.Diagnostics.CodeAnalysis;
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core.Query;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
  
    /// <summary>
    /// Unit tests for the <see cref="ParameterModelReference"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ParameterModelReferenceTests
    {
        /// <summary>
        /// Can construct a ParameterModelReference class.
        /// </summary>
        [TestMethod]
        public void CanConstruct()
        {
            var instance = new ParameterModelReference(new Mock<IEdmEntitySet>().Object, new Mock<IEdmType>().Object);
            instance.Should().NotBeNull();
        }
    }
}