// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Tests.Core
{
    using FluentAssertions;
    using Microsoft.OData.Edm;
    using Microsoft.Restier.Core;
    using Microsoft.Restier.Core.Operation;
    using Microsoft.Restier.Core.Query;
    using Microsoft.Restier.Core.Submit;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedMethodNameFactory"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class ConventionBasedMethodNameFactoryTests
    {
        private readonly IQueryHandler queryHandler;
        private readonly IEdmModel model;
        private readonly ISubmitHandler submitHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionBasedMethodNameFactoryTests"/> class.
        /// </summary>
        public ConventionBasedMethodNameFactoryTests()
        {
            queryHandler = Substitute.For<IQueryHandler>();
            model = Substitute.For<IEdmModel>();
            submitHandler = Substitute.For<ISubmitHandler>();
        }

        /// <summary>
        /// Tests all posibilities for GetEntitySetMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="entitySetOperation">The entity set operation.</param>
        /// <param name="expected">The expected result.</param>
        [TestMethod]
        [DynamicData(nameof(GetMethodNameData))]
        public void CanCallGetEntitySetMethodNameWithEntitySetAndRestierPipelineStateAndOperation(
            RestierPipelineState pipelineState,
            RestierEntitySetOperation entitySetOperation,
            string expected)
        {
            var entitySet = Substitute.For<IEdmEntitySet>();
            var entityCollectionType = Substitute.For<IEdmCollectionType>();
            var entityTypeReference = Substitute.For<IEdmEntityTypeReference>();
            var entityType = Substitute.For<IEdmEntityType>();

            entityType.Name.Returns("Test");
            entityTypeReference.Definition.Returns(entityType);
            entityCollectionType.ElementType.Returns(entityTypeReference);
            entitySet.Name.Returns("Tests");
            entitySet.Type.Returns(entityCollectionType);
            entitySet.EntityType.Returns(entityType);

            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(entitySet, pipelineState, entitySetOperation);
            result.Should().Be(expected);
        }

        /// <summary>
        /// Checks that calling GetEntitySetMethodName with a null IEdmEntitySet returns an empty string.
        /// </summary>
        [TestMethod]
        public void CanCallGetEntitySetMethodNameWithEntitySetAndRestierPipelineStateAndOperationWithNullEntitySet()
        {
            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(
                default(IEdmEntitySet),
                RestierPipelineState.PostSubmit,
                RestierEntitySetOperation.Insert);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Tests all possibilities for GetEntitySetMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="entitySetOperation">The entity set operation.</param>
        /// <param name="expected">The expected result.</param>
        [TestMethod]
        [DynamicData(nameof(GetMethodNameData))]
        public void CanCallGetEntitySetMethodNameWithItemAndRestierPipelineState(
            RestierPipelineState pipelineState,
            RestierEntitySetOperation entitySetOperation,
            string expected)
        {
            var item = new DataModificationItem(
                "Tests",
                typeof(Test),
                typeof(Test),
                entitySetOperation,
                Substitute.For<IReadOnlyDictionary<string, object>>(),
                Substitute.For<IReadOnlyDictionary<string, object>>(),
                Substitute.For<IReadOnlyDictionary<string, object>>());
            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(item, pipelineState);
            result.Should().Be(expected);
        }

        /// <summary>
        /// Checks that calling GetEntitySetMethodName with a null DataModificationItem returns an empty string.
        /// </summary>
        [TestMethod]
        public void CanCallGetEntitySetMethodNameWithItemAndRestierPipelineStateWithNullItem()
        {
            var result = ConventionBasedMethodNameFactory.GetEntitySetMethodName(
                default(DataModificationItem),
                RestierPipelineState.Authorization);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Tests all possibilities for GetFunctionMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="expected">The expected result.</param>
        [TestMethod]
        [DataRow(RestierPipelineState.Authorization, "CanExecuteCalculate")]
        [DataRow(RestierPipelineState.PostSubmit, "OnExecutedCalculate")]
        [DataRow(RestierPipelineState.PreSubmit, "OnExecutingCalculate")]
        [DataRow(RestierPipelineState.Submit, "")]
        [DataRow(RestierPipelineState.Validation, "")]
        public void CanCallGetFunctionMethodNameWithIEdmOperationImportAndRestierPipelineStateAndRestierOperationMethod(
            RestierPipelineState pipelineState,
            string expected)
        {
            var operationImportMock = Substitute.For<IEdmOperationImport>();
            var operationMock = Substitute.For<IEdmOperation>();
            operationMock.Name.Returns("Calculate");
            operationImportMock.Operation.Returns(operationMock);
            var restierOperation = RestierOperationMethod.Execute;
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(operationImportMock, pipelineState, restierOperation);
            result.Should().Be(expected);
        }

        /// <summary>
        /// Checks that calling GetFunctionMethodName with a null IEdmOperationImport returns an empty string.
        /// </summary>
        [TestMethod]
        public void CanCallGetFunctionMethodNameWithIEdmOperationImportAndRestierPipelineStateAndRestierOperationMethodWithNullOperationImport()
        {
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(
                default(IEdmOperationImport),
                RestierPipelineState.PostSubmit,
                RestierOperationMethod.Execute);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Checks that calling GetFunctionMethodName with a null OperationContext returns an empty string.
        /// </summary>
        [TestMethod]
        public void CannotCallGetFunctionMethodNameWithOperationContextAndRestierPipelineStateAndRestierOperationMethodWithNullOperationImport()
        {
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(
                default(OperationContext),
                RestierPipelineState.Authorization,
                RestierOperationMethod.Execute);
            result.Should().Be(string.Empty);
        }

        /// <summary>
        /// Tests all possibilities for GetFunctionMethodName.
        /// </summary>
        /// <param name="pipelineState">The pipeline state.</param>
        /// <param name="expected">The expected result.</param>
        [TestMethod]
        [DataRow(RestierPipelineState.Authorization, "CanExecuteCalculate")]
        [DataRow(RestierPipelineState.PostSubmit, "OnExecutedCalculate")]
        [DataRow(RestierPipelineState.PreSubmit, "OnExecutingCalculate")]
        [DataRow(RestierPipelineState.Submit, "")]
        [DataRow(RestierPipelineState.Validation, "")]
        public void CanCallGetFunctionMethodNameWithOperationContextAndRestierPipelineStateAndRestierOperationMethod(
            RestierPipelineState pipelineState,
            string expected)
        {
            var operationImport = new OperationContext(
                new EmptyApi(model, queryHandler, submitHandler),
                name => (true, (object)this),
                "Calculate",
                false,
                Substitute.For<IEnumerable>());
            var restierOperation = RestierOperationMethod.Execute;
            var result = ConventionBasedMethodNameFactory.GetFunctionMethodName(operationImport, pipelineState, restierOperation);
            result.Should().Be(expected);
        }

        /// <summary>
        /// Verifies that GetFunctionImportMethodName returns the OnFilter form for Submit+Filter
        /// against an unbound function import (e.g., a keyless view), mirroring GetEntitySetMethodName
        /// which forces the suffix to empty for the Filter operation.
        /// </summary>
        [TestMethod]
        public void GetFunctionImportMethodName_FilterOnSubmit_ReturnsOnFilterName()
        {
            var result = ConventionBasedMethodNameFactory.GetFunctionImportMethodName(
                "BooksByPublisher",
                RestierPipelineState.Submit,
                RestierEntitySetOperation.Filter);
            result.Should().Be("OnFilterBooksByPublisher");
        }

        /// <summary>
        /// Verifies that GetFunctionImportMethodName suppresses the (Filter, Authorization) combo
        /// the same way GetEntitySetMethodName does — no <c>CanFilter&lt;View&gt;</c> surface is
        /// invented for a pipeline state that has no backing convention.
        /// </summary>
        [TestMethod]
        public void GetFunctionImportMethodName_FilterOnAuthorization_ReturnsEmpty()
        {
            var result = ConventionBasedMethodNameFactory.GetFunctionImportMethodName(
                "BooksByPublisher",
                RestierPipelineState.Authorization,
                RestierEntitySetOperation.Filter);
            result.Should().Be(string.Empty);
        }

        public static IEnumerable<object[]> GetMethodNameData()
        {
            yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Delete, "CanDeleteTest" };
            yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Delete, "OnDeletedTest" };
            yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Delete, "OnDeletingTest" };
            yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Delete, string.Empty };
            yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Delete, string.Empty };
            yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Filter, string.Empty };
            yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Filter, string.Empty };
            yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Filter, string.Empty };
            yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Filter, "OnFilterTests" };
            yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Filter, string.Empty };
            yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Insert, "CanInsertTest" };
            yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Insert, "OnInsertedTest" };
            yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Insert, "OnInsertingTest" };
            yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Insert, string.Empty };
            yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Insert, string.Empty };
            yield return new object[] { RestierPipelineState.Authorization, RestierEntitySetOperation.Update, "CanUpdateTest" };
            yield return new object[] { RestierPipelineState.PostSubmit, RestierEntitySetOperation.Update, "OnUpdatedTest" };
            yield return new object[] { RestierPipelineState.PreSubmit, RestierEntitySetOperation.Update, "OnUpdatingTest" };
            yield return new object[] { RestierPipelineState.Submit, RestierEntitySetOperation.Update, string.Empty };
            yield return new object[] { RestierPipelineState.Validation, RestierEntitySetOperation.Update, string.Empty };
        }

        private class Test
        {
        }

        private class EmptyApi : ApiBase
        {
            public EmptyApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler) : base(model, queryHandler, submitHandler)
            {
            }
        }
    }
}