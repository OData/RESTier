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
    using NSubstitute;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ConventionBasedMethodNameFactory"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
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
        [Theory]
        [MemberData(nameof(GetMethodNameData))]
        public static void CanCallGetEntitySetMethodNameWithEntitySetAndRestierPipelineStateAndOperation(
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
        [Fact]
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
        [Theory]
#pragma warning disable MSTEST0018 // DynamicData should be valid
        [MemberData(nameof(GetMethodNameData))]
#pragma warning restore MSTEST0018 // DynamicData should be valid
        public static void CanCallGetEntitySetMethodNameWithItemAndRestierPipelineState(
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
        [Fact]
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
        [Theory]
        [InlineData(RestierPipelineState.Authorization, "CanExecuteCalculate")]
        [InlineData(RestierPipelineState.PostSubmit, "OnExecutedCalculate")]
        [InlineData(RestierPipelineState.PreSubmit, "OnExecutingCalculate")]
        [InlineData(RestierPipelineState.Submit, "")]
        [InlineData(RestierPipelineState.Validation, "")]
        public static void CanCallGetFunctionMethodNameWithIEdmOperationImportAndRestierPipelineStateAndRestierOperationMethod(
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
        [Fact]
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
        [Fact]
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
        [Theory]
        [InlineData(RestierPipelineState.Authorization, "CanExecuteCalculate")]
        [InlineData(RestierPipelineState.PostSubmit, "OnExecutedCalculate")]
        [InlineData(RestierPipelineState.PreSubmit, "OnExecutingCalculate")]
        [InlineData(RestierPipelineState.Submit, "")]
        [InlineData(RestierPipelineState.Validation, "")]
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

        public static IEnumerable<TheoryDataRow<RestierPipelineState, RestierEntitySetOperation, string>> GetMethodNameData()
        {
            yield return ( RestierPipelineState.Authorization, RestierEntitySetOperation.Delete, "CanDeleteTest" );
            yield return ( RestierPipelineState.PostSubmit, RestierEntitySetOperation.Delete, "OnDeletedTest" );
            yield return ( RestierPipelineState.PreSubmit, RestierEntitySetOperation.Delete, "OnDeletingTest" );
            yield return ( RestierPipelineState.Submit, RestierEntitySetOperation.Delete, string.Empty );
            yield return ( RestierPipelineState.Validation, RestierEntitySetOperation.Delete, string.Empty );
            yield return ( RestierPipelineState.Authorization, RestierEntitySetOperation.Filter, string.Empty );
            yield return ( RestierPipelineState.PostSubmit, RestierEntitySetOperation.Filter, string.Empty );
            yield return ( RestierPipelineState.PreSubmit, RestierEntitySetOperation.Filter, string.Empty );
            yield return ( RestierPipelineState.Submit, RestierEntitySetOperation.Filter, "OnFilterTests" );
            yield return ( RestierPipelineState.Validation, RestierEntitySetOperation.Filter, string.Empty );
            yield return ( RestierPipelineState.Authorization, RestierEntitySetOperation.Insert, "CanInsertTest" );
            yield return ( RestierPipelineState.PostSubmit, RestierEntitySetOperation.Insert, "OnInsertedTest" );
            yield return ( RestierPipelineState.PreSubmit, RestierEntitySetOperation.Insert, "OnInsertingTest" );
            yield return ( RestierPipelineState.Submit, RestierEntitySetOperation.Insert, string.Empty );
            yield return ( RestierPipelineState.Validation, RestierEntitySetOperation.Insert, string.Empty );
            yield return ( RestierPipelineState.Authorization, RestierEntitySetOperation.Update, "CanUpdateTest" );
            yield return ( RestierPipelineState.PostSubmit, RestierEntitySetOperation.Update, "OnUpdatedTest" );
            yield return ( RestierPipelineState.PreSubmit, RestierEntitySetOperation.Update, "OnUpdatingTest" );
            yield return ( RestierPipelineState.Submit, RestierEntitySetOperation.Update, string.Empty );
            yield return ( RestierPipelineState.Validation, RestierEntitySetOperation.Update, string.Empty );
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