// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Restier.Breakdance
{

    /// <summary>
    /// 
    /// </summary>
    public static class IEdmModelExtensions
    {

        /// <summary>
        /// Generates a list of detailed information about the expected Restier conventions for a given Api.
        /// </summary>
        /// <param name="edmModel">The <see cref="IEdmModel"/> to use to generate the convention definitions list.</param>
        /// <returns>A <see cref="List{RestierConventionDefinition}"/> containing detailed information about the expected Restier conventions.</returns>
        public static List<RestierConventionDefinition> GenerateConventionDefinitions(this IEdmModel edmModel)
        {
            if (edmModel is null)
            {
                throw new ArgumentNullException(nameof(edmModel));
            }

            var entries = new List<RestierConventionDefinition>();
            var model = (EdmModel)edmModel;

            //RWM: Cycle through the EntitySets first.
            foreach (var entitySet in model.EntityContainer.EntitySets().OrderBy(c => c.Name))
            {
                foreach (var pipelineState in Enum.GetValues(typeof(RestierPipelineState)).Cast<RestierPipelineState>())
                {
                    foreach (var operation in Enum.GetValues(typeof(RestierEntitySetOperation)).Cast<RestierEntitySetOperation>())
                    {
                        var functionName = ConventionBasedMethodNameFactory.GetEntitySetMethodName(entitySet, pipelineState, operation);
                        if (!string.IsNullOrWhiteSpace(functionName))
                        {
                            entries.Add(new RestierConventionEntitySetDefinition(functionName, pipelineState, entitySet.Name, operation));
                            entries.Add(new RestierConventionEntitySetDefinition($"{functionName}Async", pipelineState, entitySet.Name, operation));
                        }
                    }
                }
                //TODO: Handle EntitySet-bound functions.
            }

            foreach (var function in model.EntityContainer.OperationImports())
            {
                foreach (var pipelineState in Enum.GetValues(typeof(RestierPipelineState)).Cast<RestierPipelineState>())
                {
                    foreach (var operation in Enum.GetValues(typeof(RestierOperationMethod)).Cast<RestierOperationMethod>())
                    {
                        var functionName = ConventionBasedMethodNameFactory.GetFunctionMethodName(function, pipelineState, operation);
                        if (!string.IsNullOrWhiteSpace(functionName))
                        {
                            entries.Add(new RestierConventionMethodDefinition(functionName, pipelineState, function.Name, operation));
                            entries.Add(new RestierConventionMethodDefinition($"{functionName}Async", pipelineState, function.Name, operation));
                        }
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Generates a human-readable list of conventions for a Restier Api.
        /// </summary>
        /// <param name="edmModel">The <see cref="IEdmModel"/> to use to generate the conventions list.</param>
        /// <param name="addTableSeparators">A boolean specifying whether or not to add visual separators to the list.</param>
        /// <returns></returns>
        public static string GenerateConventionReport(this IEdmModel edmModel, bool addTableSeparators = false)
        {
            var sb = new StringBuilder();
            var conventions = GenerateConventionDefinitions(edmModel);

            foreach (var entitySet in conventions.OfType<RestierConventionEntitySetDefinition>().GroupBy(c => c.EntitySetName).OrderBy(c => c.Key))
            {
                if (addTableSeparators)
                {
                    sb.AppendLine($"-- {entitySet.Key} --");
                }

                foreach (var definition in entitySet.OrderBy(c => c.PipelineState).ThenBy(c => c.EntitySetOperation))
                {
                    sb.AppendLine(definition.Name);
                }

                if (addTableSeparators)
                {
                    sb.AppendLine();
                }
            }

            foreach (var function in conventions.OfType<RestierConventionMethodDefinition>().GroupBy(c => c.MethodName).OrderBy(c => c.Key))
            {
                if (addTableSeparators)
                {
                    sb.AppendLine($"-- OperationImports --");
                }

                foreach (var definition in function.OrderBy(c => c.PipelineState).ThenBy(c => c.MethodOperation))
                {
                    sb.AppendLine(definition.Name);
                }

                if (addTableSeparators)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }


    }

}