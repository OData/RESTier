// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Operation;
using Microsoft.Restier.Core.Submit;
using System.Collections.Generic;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// A set of string factory methods than generate Restier names for various possible operations.
    /// </summary>
    public static class ConventionBasedMethodNameFactory
    {

        #region Constants

        private const string Can = "Can";

        private const string On = "On";

        private const string Ing = "ing";

        private const string Ed = "ed";

        #endregion

        #region Private Members

        /// <summary>
        /// The <see cref="RestierPipelineStates"/> to exclude from Filter name processing.
        /// </summary>
        private static List<RestierPipelineStates> ExcludedFilterStates = new List<RestierPipelineStates>
        {
            RestierPipelineStates.Authorization,
            RestierPipelineStates.PreSubmit,
            RestierPipelineStates.PostSubmit
        };

        /// <summary>
        /// The <see cref="RestierEntitySetOperations"/> to exclude from EntitySet Submit name processing.
        /// </summary>
        private static List<RestierEntitySetOperations> ExcludedEntitySetSubmitOperations = new List<RestierEntitySetOperations>
        {
            RestierEntitySetOperations.Insert,
            RestierEntitySetOperations.Update,
            RestierEntitySetOperations.Delete
        };

        /// <summary>
        /// The <see cref="RestierOperationMethods"/> to exclude from Method Submit name processing.
        /// </summary>
        private static List<RestierOperationMethods> ExcludedMethodSubmitOperations = new List<RestierOperationMethods>
        {
            RestierOperationMethods.Execute
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineStates"/>, and <see cref="RestierEntitySetOperations"/>.
        /// </summary>
        /// <param name="entitySet">The <see cref="IEdmEntitySet"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="operation">The <see cref="RestierEntitySetOperations"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        /// <returns></returns>
        public static string GetEntitySetMethodName(IEdmEntitySet entitySet, RestierPipelineStates restierPipelineState, RestierEntitySetOperations operation)
        {
            if ((operation == RestierEntitySetOperations.Filter && ExcludedFilterStates.Contains(restierPipelineState))
                || restierPipelineState == RestierPipelineStates.Submit && ExcludedEntitySetSubmitOperations.Contains(operation))
            {
                return string.Empty;
            }

            var prefix = GetPipelinePrefixInternal(restierPipelineState);

            //RWM: If, for some reason, we don't have a prefix, then we don't have a method for this operation. So don't do anything.
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;

            var operationName = GetRestierOperationNameInternal(operation, restierPipelineState);
            var suffix = operation != RestierEntitySetOperations.Filter ? GetPipelineSuffixInternal(restierPipelineState) : string.Empty;
            var entityReferenceName = GetEntityReferenceNameInternal(operation, entitySet);
            return $"{prefix}{operationName}{suffix}{entityReferenceName}";
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineStates"/>, and <see cref="RestierEntitySetOperations"/>.
        /// </summary>
        /// <param name="item">The <see cref="DataModificationItem"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        /// <returns></returns>
        public static string GetEntitySetMethodName(DataModificationItem item, RestierPipelineStates restierPipelineState)
        {
            if ((item.EntitySetOperation == RestierEntitySetOperations.Filter && ExcludedFilterStates.Contains(restierPipelineState))
                || restierPipelineState == RestierPipelineStates.Submit && ExcludedEntitySetSubmitOperations.Contains(item.EntitySetOperation))
            {
                return string.Empty;
            }

            var prefix = GetPipelinePrefixInternal(restierPipelineState);

            //RWM: If, for some reason, we don't have a prefix, then we don't have a method for this operation. So don't do anything.
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;

            var operationName = GetRestierOperationNameInternal(item.EntitySetOperation, restierPipelineState);
            var suffix = item.EntitySetOperation != RestierEntitySetOperations.Filter ? GetPipelineSuffixInternal(restierPipelineState) : string.Empty;
            var entityReferenceName = GetEntityReferenceNameInternal(item.EntitySetOperation, item.ResourceSetName, item.ExpectedResourceType.Name);
            return $"{prefix}{operationName}{suffix}{entityReferenceName}";
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineStates"/>, and <see cref="RestierEntitySetOperations"/>.
        /// </summary>
        /// <param name="operationImport">The <see cref="IEdmOperationImport"/> to generate a name for.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="restierOperation">The <see cref="RestierOperationMethods"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        public static string GetFunctionMethodName(IEdmOperationImport operationImport, RestierPipelineStates restierPipelineState, RestierOperationMethods restierOperation)
        {
            return GetFunctionMethodNameInternal(operationImport.Operation.Name, restierPipelineState, restierOperation);
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="OperationContext"/>, <see cref="RestierPipelineStates"/>, and <see cref="RestierEntitySetOperations"/>.
        /// </summary>
        /// <param name="operationImport">The <see cref="OperationContext"/> to generate a name for.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="restierOperation">The <see cref="RestierOperationMethods"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        public static string GetFunctionMethodName(OperationContext operationImport, RestierPipelineStates restierPipelineState, RestierOperationMethods restierOperation)
        {
            return GetFunctionMethodNameInternal(operationImport.OperationName, restierPipelineState, restierOperation);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates the right EntityName reference for a given Operation.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperations"/> to determine the Entity name for.</param>
        /// <param name="entitySet">The <see cref="IEdmEntitySet"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <returns>A string representing the right EntityName reference for a given Operation.</returns>
        internal static string GetEntityReferenceNameInternal(RestierEntitySetOperations operation, IEdmEntitySet entitySet)
        {
            //RWM: You filter a set, but you Insert/Update/Delete individual items.
            return GetEntityReferenceNameInternal(operation, entitySet.Name, entitySet.EntityType().Name);
        }

        /// <summary>
        /// Generates the right EntityName reference for a given Operation.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperations"/> to determine the Entity name for.</param>
        /// <param name="entitySetName">The <see cref="string"/> that contains the name of the EntitySet.</param>
        /// <param name="entityTypeName">The <see cref="string"/> that contains the name of the Entity type.</param>
        /// <returns>A string representing the right EntityName reference for a given Operation.</returns>
        internal static string GetEntityReferenceNameInternal(RestierEntitySetOperations operation, string entitySetName, string entityTypeName)
        {
            //RWM: You filter a set, but you Insert/Update/Delete individual items.
            return operation == RestierEntitySetOperations.Filter ? entitySetName : entityTypeName;
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineStates"/>, and <see cref="RestierEntitySetOperations"/>.
        /// </summary>
        /// <param name="operationName">The <see cref="string"/> containing the name of the operation.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="restierOperation">The <see cref="RestierOperationMethods"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        private static string GetFunctionMethodNameInternal(string operationName, RestierPipelineStates restierPipelineState, RestierOperationMethods restierOperation)
        {
            if (restierPipelineState == RestierPipelineStates.Submit && ExcludedMethodSubmitOperations.Contains(restierOperation))
            {
                return string.Empty;
            }

            var prefix = GetPipelinePrefixInternal(restierPipelineState);

            //RWM: If, for some reason, we don't have a prefix, then we don't have a method for this operation. So don't do anything.
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;

            var restierOperationName = GetRestierOperationNameInternal(restierOperation, restierPipelineState);
            var suffix = GetPipelineSuffixInternal(restierPipelineState);
            return $"{prefix}{restierOperationName}{suffix}{operationName}";
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierEntitySetOperations"/> and <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperations"/> to determine the method name for.</param>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineStates"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        internal static string GetRestierOperationNameInternal(RestierEntitySetOperations operation, RestierPipelineStates restierPipelineState)
        {
            return GetRestierOperationNameInternal(operation.ToString(), restierPipelineState);
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierOperationMethods"/> and <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="operation">The <see cref="RestierOperationMethods"/> to determine the method name for.</param>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineStates"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        internal static string GetRestierOperationNameInternal(RestierOperationMethods operation, RestierPipelineStates restierPipelineState)
        {
            return GetRestierOperationNameInternal(operation.ToString(), restierPipelineState);
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierOperationMethods"/> and <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="operation">The string representing the Operation to determine the method name for.</param>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineStates"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        /// <remarks>This method is for base processing. The other overloads should be used to ensure the right name gets generated.</remarks>
        private static string GetRestierOperationNameInternal(string operation, RestierPipelineStates restierPipelineState)
        {
            switch (restierPipelineState)
            {
                case RestierPipelineStates.PreSubmit:
                case RestierPipelineStates.PostSubmit:
                    //RWM: If the last letter of the string is an e, cut off it's head.
                    return operation.LastIndexOf("e") == operation.Length - 1 ? operation.Substring(0, operation.Length - 1) : operation;
                default:
                    return operation;
            }
        }

        /// <summary>
        /// Returns a method prefix string for a given <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineStates"/> to determine the prefix for.</param>
        /// <returns></returns>
        internal static string GetPipelinePrefixInternal(RestierPipelineStates restierPipelineState)
        {
            switch (restierPipelineState)
            {
                case RestierPipelineStates.Authorization:
                    return Can;
                case RestierPipelineStates.PreSubmit:
                case RestierPipelineStates.Submit:
                case RestierPipelineStates.PostSubmit:
                    return On;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Returns a method suffix string for a given <see cref="RestierPipelineStates"/>.
        /// </summary>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineStates"/> to determine the suffix for.</param>
        /// <returns></returns>
        internal static string GetPipelineSuffixInternal(RestierPipelineStates restierPipelineState)
        {
            switch (restierPipelineState)
            {
                case RestierPipelineStates.PreSubmit:
                    return Ing;
                case RestierPipelineStates.PostSubmit:
                    return Ed;
                default:
                    return string.Empty;
            }
        }

        #endregion

    }

}