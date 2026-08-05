// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Asp.Versioning;

namespace Microsoft.Restier.AspNetCore.Versioning.Internal
{

    /// <summary>
    /// Reads <see cref="ApiVersionAttribute"/> instances from a type and projects each declared
    /// version into an <see cref="ApiVersionAttributeReadResult"/>.
    /// </summary>
    /// <remarks>
    /// Sunset is intentionally NOT read here — <see cref="ApiVersionAttribute"/> does not carry
    /// sunset metadata. Sunset comes from <see cref="RestierVersioningOptions.SunsetDate"/>.
    /// </remarks>
    internal static class ApiVersionAttributeReader
    {

        public static IEnumerable<ApiVersionAttributeReadResult> Read(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var attributes = type.GetCustomAttributes<ApiVersionAttribute>(inherit: true).ToArray();
            if (attributes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type {type.FullName} has no [ApiVersion] attribute. " +
                    "Add [ApiVersion(\"1.0\")] (or another version) to the class, " +
                    "or use the imperative overload of AddVersion that takes an ApiVersion argument explicitly.");
            }

            foreach (var attribute in attributes)
            {
                foreach (var version in attribute.Versions)
                {
                    yield return new ApiVersionAttributeReadResult(version, attribute.Deprecated);
                }
            }
        }

    }

    internal readonly struct ApiVersionAttributeReadResult
    {

        public ApiVersionAttributeReadResult(ApiVersion apiVersion, bool isDeprecated)
        {
            ApiVersion = apiVersion;
            IsDeprecated = isDeprecated;
        }

        public ApiVersion ApiVersion { get; }

        public bool IsDeprecated { get; }

    }

}
