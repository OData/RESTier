// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

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

        public static Type FindGenericType(this Type type, Type definition)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == definition)
                {
                    return type;
                }

                if (definition.IsInterface)
                {
                    foreach (var interfaceType in type.GetInterfaces())
                    {
                        var found = interfaceType.FindGenericType(definition);
                        if (found != null)
                        {
                            return found;
                        }
                    }
                }

                type = type.BaseType;
            }

            return null;
        }

        internal static MethodInfo GetQualifiedMethod(this Type type, string methodName)
        {
            return type.GetMethod(methodName, QualifiedMethodBindingFlags);
        }
    }
}
