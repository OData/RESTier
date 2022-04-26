// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System
{
    internal static partial class TypeExtensions
    {
        private const BindingFlags QualifiedMethodBindingFlags = BindingFlags.NonPublic |
                                                                 BindingFlags.Static |
                                                                 BindingFlags.Instance |
                                                                 BindingFlags.IgnoreCase |
                                                                 BindingFlags.DeclaredOnly;

        /// <summary>
        /// Find a base type or implemented interface which has a generic definition
        /// represented by the parameter, <c>definition</c>.
        /// </summary>
        /// <param name="type">
        /// The subject type.
        /// </param>
        /// <param name="definition">
        /// The generic definition to check with.
        /// </param>
        /// <returns>
        /// The base type or the interface found; otherwise, <c>null</c>.
        /// </returns>
        public static Type FindGenericType(this Type type, Type definition)
        {
            if (type is null)
            {
                return null;
            }

            // If the type conforms the given generic definition, no further check required.
            if (type.IsGenericDefinition(definition))
            {
                return type;
            }

            // If the definition is interface, we only need to check the interfaces implemented by the current type
            if (definition.IsInterface)
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.IsGenericDefinition(definition))
                    {
                        return interfaceType;
                    }
                }
            }
            else if (!type.IsInterface)
            {
                // If the definition is not an interface, then the current type cannot be an interface too.
                // Otherwise, we should only check the parent class types of the current type.

                // no null check for the type required, as we are sure it is not an interface type
                while (type != typeof(object))
                {
                    if (type.IsGenericDefinition(definition))
                    {
                        return type;
                    }

                    type = type.BaseType;
                }
            }

            return null;
        }

        public static MethodInfo FindQualifiedMethod(this Type type, string methodName)
        {
            return type.GetMethods(QualifiedMethodBindingFlags).FirstOrDefault(c => c.Name.EndsWith(methodName, StringComparison.InvariantCultureIgnoreCase));
        }


        public static MethodInfo GetQualifiedMethod(this Type type, string methodName)
        {
            return type.GetMethod(methodName, QualifiedMethodBindingFlags);
        }

        public static bool TryGetElementType(this Type type, out Type elementType)
        {
            // Special case: string implements IEnumerable<char> however it should
            // NOT be treated as a collection type.
            if (type == typeof(string))
            {
                elementType = null;
                return false;
            }

            var interfaceType = type.FindGenericType(typeof(IEnumerable<>));
            if (interfaceType is not null)
            {
                elementType = interfaceType.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool IsGenericDefinition(this Type type, Type definition)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == definition;
        }
    }
}
