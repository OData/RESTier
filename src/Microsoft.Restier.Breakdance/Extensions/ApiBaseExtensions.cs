// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using CloudNimble.EasyAF.Core;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Restier.Breakdance
{

    /// <summary>
    /// 
    /// </summary>
    public static class ApiBaseExtensions
    {

        #region Constants

        const string separator              = "---------------------------------------------------|--------";
        const string markdownSeparator      = "---------------------------------------------------|----------:";
        const string formatString           = "{0,-50} | {1,7}";
        const string markdownFormatString   = "{0,-50} | {1,10}";

        #endregion

        #region Public Methods

        /// <summary>
        /// An extension method that generates a Markdown table of all of the possible Restier methods for the given API in the first column, and a boolean
        /// indicating whether or not the method was found in the second column.
        /// </summary>
        /// <param name="api">The <see cref="ApiBase"/> instance to process.</param>
        /// <param name="markdown"></param>
        /// <returns>A string containing the Markdown table of results.</returns>
        public static string GenerateVisibilityMatrix(this ApiBase api, bool markdown = false)
        {
            Ensure.ArgumentNotNull(api, nameof(api));

            var sb = new StringBuilder();
            var model = (EdmModel)api.GetModel();
            var apiType = api.GetType();

            var conventions = model.GenerateConventionDefinitions();
            var entitySetMatrix = conventions.OfType<RestierConventionEntitySetDefinition>().ToDictionary(c => c, c => false);
            var methodMatrix = conventions.OfType<RestierConventionMethodDefinition>().ToDictionary(c => c, c => false);

            foreach (var definition in entitySetMatrix.ToList())
            {
                var value = false;
                switch (definition.Key.PipelineState)
                {
                    case RestierPipelineState.Authorization:
                        value = IsAuthorizerMethodAccessible(apiType, definition.Key.Name);
                        break;
                    default:
                        if (definition.Key.EntitySetOperation == RestierEntitySetOperation.Filter)
                        {
                            value = IsFilterMethodAccessible(apiType, definition.Key.Name);
                        }
                        else
                        {
                            value = IsInterceptorMethodAccessible(apiType, definition.Key.Name);
                        }
                        break;
                }
                entitySetMatrix[definition.Key] = value;
            }

            foreach (var definition in methodMatrix.ToList())
            {
                var value = false;
                switch (definition.Key.PipelineState)
                {
                    case RestierPipelineState.Authorization:
                        value = IsAuthorizerMethodAccessible(apiType, definition.Key.Name);
                        break;
                    default:
                        value = IsInterceptorMethodAccessible(apiType, definition.Key.Name);
                        break;
                }
                methodMatrix[definition.Key] = value;
            }

            if (!markdown)
            {
                sb.AppendLine(separator);
            }

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, markdown ? markdownFormatString : formatString, "Function Name", "Found?"));
            sb.AppendLine(markdown ? markdownSeparator : separator);
            foreach (var result in entitySetMatrix)
            {
                sb.AppendLine(FormatRow(result, markdown));
            }
            foreach (var result in methodMatrix)
            {
                sb.AppendLine(FormatRow(result, markdown));
            }

            if (!markdown)
            {
                sb.AppendLine(separator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// An extension method that generates the Visibility Matrix for the current Api and writes it to a text file.
        /// </summary>
        /// <param name="api">The <see cref="ApiBase"/> instance to build the Visibility Matrix for.</param>
        /// <param name="sourceDirectory">
        /// A string containing the relative or absolute path to use as the root. The default is "". If you want to be able to have it as part of the project,
        /// so you can check it into source control, use "..//..//".
        /// </param>
        /// <param name="suffix">A string to append to the Api name when writing the text file.</param>
        /// <param name="markdown"></param>
        public static void WriteCurrentVisibilityMatrix(this ApiBase api, string sourceDirectory = "", string suffix = "ApiSurface", bool markdown = false)
        {
            if (api is null)
            {
                throw new ArgumentNullException(nameof(api));
            }

            var filePath = Path.Combine(sourceDirectory, $"{api.GetType().Name}-{suffix}.{(markdown ? "md" : "txt")}");
            var report = api.GenerateVisibilityMatrix(markdown);
            File.WriteAllText(filePath, report);
        }

        #endregion

        #region Private Members

        private static string FormatRow(KeyValuePair<RestierConventionEntitySetDefinition, bool> result, bool markdown = false)
        {
            return string.Format(
                CultureInfo.InvariantCulture, 
                markdown ? markdownFormatString : formatString,
                markdown && result.Value ? $"**{result.Key.Name}**" : result.Key.Name,
                markdown && result.Value ? $"**{result.Value}**" : result.Value
            );
        }

        private static string FormatRow(KeyValuePair<RestierConventionMethodDefinition, bool> result, bool markdown = false)
        {
            return string.Format(
                CultureInfo.InvariantCulture, 
                markdown ? markdownFormatString : formatString,
                markdown && result.Value ? $"**{result.Key.Name}**" : result.Key.Name,
                markdown && result.Value ? $"**{result.Value}**" : result.Value
            );
        }

        /// <summary>
        /// This method recreates parts of the code in <see cref="ConventionBasedChangeSetItemAuthorizer" /> to determine if
        /// Restier can access the specified method name in the specified type.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static bool IsAuthorizerMethodAccessible(Type api, string methodName)
        {
            //ConventionBasedChangeSetItemAuthorizer.cs:46
            Type returnType = typeof(bool);
            var method = api.GetQualifiedMethod(methodName);

            if (method is not null && (method.IsFamily || method.IsFamilyOrAssembly) &&
                method.ReturnType == returnType)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// This method recreates parts of the code in <see cref="ConventionBasedChangeSetItemFilter" /> to determine if
        /// Restier can access the specified method name in the specified type.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static bool IsInterceptorMethodAccessible(Type api, string methodName)
        {
            //ConventionBasedChangeSetItemFilter.cs:116
            var method = api.GetQualifiedMethod(methodName);

            if (method is not null &&
                (method.ReturnType == typeof(void) ||
                typeof(Task).IsAssignableFrom(method.ReturnType)))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// This method recreates parts of the code in <see cref="ConventionBasedQueryExpressionProcessor" /> to determine if
        /// Restier can access the specified method name in the specified type.
        /// </summary>
        /// <param name="api"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static bool IsFilterMethodAccessible(Type api, string methodName)
        {
            //ConventionBasedQueryExpressionProcessor.cs:110
            var method = api.GetQualifiedMethod(methodName);

            if (method is not null && (method.IsFamily || method.IsFamilyOrAssembly))
            {
                var parameter = method.GetParameters().SingleOrDefault();
                if (parameter is not null &&
                    parameter.ParameterType == method.ReturnType)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

    }

}