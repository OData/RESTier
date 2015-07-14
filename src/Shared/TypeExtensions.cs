// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Reflection;

namespace System
{
    internal static partial class TypeExtensions
    {
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

        public static MethodInfo GetQualifiedMethod(this Type type, string methodName)
        {
            var bindingFlags = BindingFlags.NonPublic |
                               BindingFlags.Static |
                               BindingFlags.Instance |
                               BindingFlags.IgnoreCase |
                               BindingFlags.DeclaredOnly;
            return type.GetMethod(methodName, bindingFlags);
        }
    }
}
