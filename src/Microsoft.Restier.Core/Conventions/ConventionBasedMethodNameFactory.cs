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
        /// The <see cref="RestierPipelineState"/> to exclude from Filter name processing.
        /// </summary>
        private static List<RestierPipelineState> ExcludedFilterStates = new List<RestierPipelineState>
        {
            RestierPipelineState.Authorization,
            RestierPipelineState.PreSubmit,
            RestierPipelineState.PostSubmit
        };

        /// <summary>
        /// The <see cref="RestierEntitySetOperation"/> to exclude from EntitySet Submit name processing.
        /// </summary>
        private static List<RestierEntitySetOperation> ExcludedEntitySetSubmitOperations = new List<RestierEntitySetOperation>
        {
            RestierEntitySetOperation.Insert,
            RestierEntitySetOperation.Update,
            RestierEntitySetOperation.Delete
        };

        /// <summary>
        /// The <see cref="RestierOperationMethod"/> to exclude from Method Submit name processing.
        /// </summary>
        private static List<RestierOperationMethod> ExcludedMethodSubmitOperations = new List<RestierOperationMethod>
        {
            RestierOperationMethod.Execute
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineState"/>, and <see cref="RestierEntitySetOperation"/>.
        /// </summary>
        /// <param name="entitySet">The <see cref="IEdmEntitySet"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="operation">The <see cref="RestierEntitySetOperation"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        /// <returns></returns>
        public static string GetEntitySetMethodName(IEdmEntitySet entitySet, RestierPipelineState restierPipelineState, RestierEntitySetOperation operation)
        {
            if ( entitySet == null
                || (operation == RestierEntitySetOperation.Filter && ExcludedFilterStates.Contains(restierPipelineState))
                || restierPipelineState == RestierPipelineState.Submit && ExcludedEntitySetSubmitOperations.Contains(operation))
            {
                return string.Empty;
            }

            var prefix = GetPipelinePrefixInternal(restierPipelineState);

            //RWM: If, for some reason, we don't have a prefix, then we don't have a method for this operation. So don't do anything.
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;

            var operationName = GetRestierOperationNameInternal(operation, restierPipelineState);
            var suffix = operation != RestierEntitySetOperation.Filter ? GetPipelineSuffixInternal(restierPipelineState) : string.Empty;
            var entityReferenceName = GetEntityReferenceNameInternal(operation, entitySet);
            return $"{prefix}{operationName}{suffix}{entityReferenceName}";
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineState"/>, and <see cref="RestierEntitySetOperation"/>.
        /// </summary>
        /// <param name="item">The <see cref="DataModificationItem"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        /// <returns></returns>
        public static string GetEntitySetMethodName(DataModificationItem item, RestierPipelineState restierPipelineState)
        {
            if (item == null
                || (item.EntitySetOperation == RestierEntitySetOperation.Filter && ExcludedFilterStates.Contains(restierPipelineState))
                || restierPipelineState == RestierPipelineState.Submit && ExcludedEntitySetSubmitOperations.Contains(item.EntitySetOperation))
            {
                return string.Empty;
            }

            var prefix = GetPipelinePrefixInternal(restierPipelineState);

            //RWM: If, for some reason, we don't have a prefix, then we don't have a method for this operation. So don't do anything.
            if (string.IsNullOrWhiteSpace(prefix)) return string.Empty;

            var operationName = GetRestierOperationNameInternal(item.EntitySetOperation, restierPipelineState);
            var suffix = item.EntitySetOperation != RestierEntitySetOperation.Filter ? GetPipelineSuffixInternal(restierPipelineState) : string.Empty;
            var entityReferenceName = GetEntityReferenceNameInternal(item.EntitySetOperation, item.ResourceSetName, item.ExpectedResourceType.Name);
            return $"{prefix}{operationName}{suffix}{entityReferenceName}";
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineState"/>, and <see cref="RestierEntitySetOperation"/>.
        /// </summary>
        /// <param name="operationImport">The <see cref="IEdmOperationImport"/> to generate a name for.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="restierOperation">The <see cref="RestierOperationMethod"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        public static string GetFunctionMethodName(IEdmOperationImport operationImport, RestierPipelineState restierPipelineState, RestierOperationMethod restierOperation)
        {
            if (operationImport == null) return string.Empty;
            return GetFunctionMethodNameInternal(operationImport.Operation.Name, restierPipelineState, restierOperation);
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="OperationContext"/>, <see cref="RestierPipelineState"/>, and <see cref="RestierEntitySetOperation"/>.
        /// </summary>
        /// <param name="operationImport">The <see cref="OperationContext"/> to generate a name for.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="restierOperation">The <see cref="RestierOperationMethod"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        public static string GetFunctionMethodName(OperationContext operationImport, RestierPipelineState restierPipelineState, RestierOperationMethod restierOperation)
        {
            if (operationImport == null) return string.Empty;
            return GetFunctionMethodNameInternal(operationImport.OperationName, restierPipelineState, restierOperation);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates the right EntityName reference for a given Operation.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperation"/> to determine the Entity name for.</param>
        /// <param name="entitySet">The <see cref="IEdmEntitySet"/> that contains the details for the EntitySet and the Entities it holds.</param>
        /// <returns>A string representing the right EntityName reference for a given Operation.</returns>
        internal static string GetEntityReferenceNameInternal(RestierEntitySetOperation operation, IEdmEntitySet entitySet)
        {
            //RWM: You filter a set, but you Insert/Update/Delete individual items.
            return GetEntityReferenceNameInternal(operation, entitySet.Name, entitySet.EntityType().Name);
        }

        /// <summary>
        /// Generates the right EntityName reference for a given Operation.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperation"/> to determine the Entity name for.</param>
        /// <param name="entitySetName">The <see cref="string"/> that contains the name of the EntitySet.</param>
        /// <param name="entityTypeName">The <see cref="string"/> that contains the name of the Entity type.</param>
        /// <returns>A string representing the right EntityName reference for a given Operation.</returns>
        internal static string GetEntityReferenceNameInternal(RestierEntitySetOperation operation, string entitySetName, string entityTypeName)
        {
            //RWM: You filter a set, but you Insert/Update/Delete individual items.
            return operation == RestierEntitySetOperation.Filter ? entitySetName : entityTypeName;
        }

        /// <summary>
        /// Generates the complete MethodName for a given <see cref="IEdmOperationImport"/>, <see cref="RestierPipelineState"/>, and <see cref="RestierEntitySetOperation"/>.
        /// </summary>
        /// <param name="operationName">The <see cref="string"/> containing the name of the operation.</param>
        /// <param name="restierPipelineState">The part of the Restier pipeline currently executing.</param>
        /// <param name="restierOperation">The <see cref="RestierOperationMethod"/> currently being executed.</param>
        /// <returns>A string representing the fully-realized MethodName.</returns>
        private static string GetFunctionMethodNameInternal(string operationName, RestierPipelineState restierPipelineState, RestierOperationMethod restierOperation)
        {
            if (restierPipelineState == RestierPipelineState.Submit && ExcludedMethodSubmitOperations.Contains(restierOperation))
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
        /// Generates the right OperationName string for a given <see cref="RestierEntitySetOperation"/> and <see cref="RestierPipelineState"/>.
        /// </summary>
        /// <param name="operation">The <see cref="RestierEntitySetOperation"/> to determine the method name for.</param>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineState"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        internal static string GetRestierOperationNameInternal(RestierEntitySetOperation operation, RestierPipelineState restierPipelineState)
        {
            return GetRestierOperationNameInternal(operation.ToString(), restierPipelineState);
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierOperationMethod"/> and <see cref="RestierPipelineState"/>.
        /// </summary>
        /// <param name="operation">The <see cref="RestierOperationMethod"/> to determine the method name for.</param>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineState"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        internal static string GetRestierOperationNameInternal(RestierOperationMethod operation, RestierPipelineState restierPipelineState)
        {
            return GetRestierOperationNameInternal(operation.ToString(), restierPipelineState);
        }

        /// <summary>
        /// Generates the right OperationName string for a given <see cref="RestierOperationMethod"/> and <see cref="RestierPipelineState"/>.
        /// </summary>
        /// <param name="operation">The string representing the Operation to determine the method name for.</param>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineState"/> to determine the method name for.</param>
        /// <returns>A string containing the corrected OperationName, accounting for what the suffix will end up being.</returns>
        /// <remarks>This method is for base processing. The other overloads should be used to ensure the right name gets generated.</remarks>
        private static string GetRestierOperationNameInternal(string operation, RestierPipelineState restierPipelineState)
        {
            switch (restierPipelineState)
            {
                case RestierPipelineState.PreSubmit:
                case RestierPipelineState.PostSubmit:
                    //RWM: If the last letter of the string is an e, cut off it's head.
                    return operation.LastIndexOf("e") == operation.Length - 1 ? operation.Substring(0, operation.Length - 1) : operation;
                default:
                    return operation;
            }
        }

        /// <summary>
        /// Returns a method prefix string for a given <see cref="RestierPipelineState"/>.
        /// </summary>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineState"/> to determine the prefix for.</param>
        /// <returns></returns>
        internal static string GetPipelinePrefixInternal(RestierPipelineState restierPipelineState)
        {
            switch (restierPipelineState)
            {
                case RestierPipelineState.Authorization:
                    return Can;
                case RestierPipelineState.PreSubmit:
                case RestierPipelineState.Submit:
                case RestierPipelineState.PostSubmit:
                    return On;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Returns a method suffix string for a given <see cref="RestierPipelineState"/>.
        /// </summary>
        /// <param name="restierPipelineState">The <see cref="RestierPipelineState"/> to determine the suffix for.</param>
        /// <returns></returns>
        internal static string GetPipelineSuffixInternal(RestierPipelineState restierPipelineState)
        {
            switch (restierPipelineState)
            {
                case RestierPipelineState.PreSubmit:
                    return Ing;
                case RestierPipelineState.PostSubmit:
                    return Ed;
                default:
                    return string.Empty;
            }
        }

        #endregion

    }

}