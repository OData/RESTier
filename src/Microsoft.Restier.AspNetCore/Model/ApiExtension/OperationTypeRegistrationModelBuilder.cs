// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// Pre-pass model builder that scans the target API for methods decorated with
    /// <see cref="OperationAttribute"/>-family attributes and registers any
    /// CLR types referenced by their parameters or return types that the inner
    /// model has not already declared.
    /// </summary>
    /// <remarks>
    /// Runs between <see cref="RestierWebApiModelBuilder"/> and
    /// <see cref="RestierWebApiOperationModelBuilder"/> in the chain so that operations
    /// resolved by the latter can find their referenced types via
    /// <c>EdmHelpers.GetTypeReference</c>. Uses <see cref="ODataConventionModelBuilder"/>
    /// as the underlying registrar so type conventions (enums, [Required], nested
    /// complex properties) are honored without re-implementing them here.
    /// </remarks>
    public class OperationTypeRegistrationModelBuilder : IModelBuilder
    {
        private readonly Type _targetApiType;

        /// <summary>
        /// Gets or sets the inner model builder.
        /// </summary>
        public IModelBuilder Inner { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationTypeRegistrationModelBuilder"/> class.
        /// </summary>
        /// <param name="targetApiType">The target API type to scan for [Operation]-decorated methods.</param>
        public OperationTypeRegistrationModelBuilder(Type targetApiType)
        {
            Ensure.NotNull(targetApiType, nameof(targetApiType));
            _targetApiType = targetApiType;
        }

        /// <inheritdoc />
        public IEdmModel GetEdmModel()
        {
            var inner = Inner?.GetEdmModel();
            if (inner is not EdmModel model)
            {
                return inner;
            }

            var referencedTypes = CollectReferencedTypes();
            var missingTypes = referencedTypes
                .Where(t => !IsBuiltInPrimitive(t) && !IsDeclaredInModel(model, t))
                .Distinct()
                .ToList();
            if (missingTypes.Count == 0)
            {
                return model;
            }

            try
            {
                MergeIntoInnerModel(model, missingTypes);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning(
                    "Restier: OperationTypeRegistrationModelBuilder failed to register one or more types referenced " +
                    "by [Operation]-decorated methods. Error: {0}", ex.Message);
            }

            return model;
        }

        /// <summary>
        /// Returns the EDM full name for a CLR type using the OData convention:
        /// <c>Namespace.SimpleName</c>. For types without a namespace the simple name is returned.
        /// This mirrors how <see cref="ODataConventionModelBuilder"/> names types — it uses
        /// <c>Type.Namespace</c> and <c>Type.Name</c>, not <c>Type.FullName</c>, so nested CLR
        /// types (whose <c>FullName</c> contains a <c>+</c> separator) map correctly.
        /// </summary>
        private static string GetEdmFullName(Type type)
        {
            var ns = type.Namespace;
            return string.IsNullOrEmpty(ns) ? type.Name : ns + "." + type.Name;
        }

        /// <summary>
        /// Determines whether the given CLR type is already represented in the model,
        /// using the OData convention name (<c>Namespace.SimpleName</c>) rather than
        /// the CLR <c>FullName</c> (which uses <c>+</c> for nested types).
        /// Also checks via <see cref="ClrTypeAnnotation"/> for types the inner model
        /// has decorated with their originating CLR type.
        /// </summary>
        private static bool IsDeclaredInModel(EdmModel model, Type type)
        {
            // Primary check: look up by OData-convention full name.
            if (model.FindDeclaredType(GetEdmFullName(type)) is not null)
            {
                return true;
            }

            // Secondary check: look for ClrTypeAnnotation match.
            return model.SchemaElements.OfType<IEdmSchemaType>()
                .Any(s => model.GetAnnotationValue<ClrTypeAnnotation>(s)?.ClrType == type);
        }

        private HashSet<Type> CollectReferencedTypes()
        {
            var methods = _targetApiType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Public
                          | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
                .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
                .Where(m => m.GetCustomAttribute<OperationAttribute>(inherit: true) is not null);

            var seen = new HashSet<Type>();
            foreach (var method in methods)
            {
                foreach (var parameter in method.GetParameters())
                {
                    AddType(parameter.ParameterType, seen);
                }

                AddType(method.ReturnType, seen);
            }

            return seen;
        }

        private static void AddType(Type type, HashSet<Type> seen)
        {
            if (type is null)
            {
                return;
            }

            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            // Unwrap collection wrappers (arrays, IEnumerable<T>, IQueryable<T>).
            if (underlying.IsArray && underlying.GetElementType() is not null)
            {
                AddType(underlying.GetElementType(), seen);
                return;
            }

            if (underlying.IsGenericType)
            {
                var generic = underlying.GetGenericTypeDefinition();
                if (generic == typeof(System.Collections.Generic.IEnumerable<>)
                    || generic == typeof(System.Collections.Generic.IList<>)
                    || generic == typeof(System.Collections.Generic.ICollection<>)
                    || generic == typeof(System.Collections.Generic.IReadOnlyList<>)
                    || generic == typeof(System.Collections.Generic.IReadOnlyCollection<>)
                    || generic == typeof(System.Linq.IQueryable<>)
                    || generic == typeof(System.Collections.Generic.List<>))
                {
                    AddType(underlying.GetGenericArguments()[0], seen);
                    return;
                }
            }

            if (underlying.IsValueType && !underlying.IsEnum)
            {
                return;   // primitives handled by EdmHelpers
            }

            if (underlying == typeof(string) || underlying == typeof(void) || underlying == typeof(object))
            {
                return;
            }

            seen.Add(underlying);
        }

        private static bool IsBuiltInPrimitive(Type type)
            => type.IsPrimitive || type == typeof(string) || type == typeof(decimal)
               || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid)
               || type == typeof(TimeSpan) || type == typeof(byte[]);

        private static void MergeIntoInnerModel(EdmModel model, List<Type> missingTypes)
        {
            var auxBuilder = new ODataConventionModelBuilder();

            // Pre-ignore every type the inner model already declares so the auxiliary
            // builder doesn't re-emit it. ClrTypeAnnotation is read directly because
            // EdmHelpers.GetClrType throws when the annotation is absent.
            var alreadyKnown = model.SchemaElements.OfType<IEdmSchemaType>()
                .Select(s => model.GetAnnotationValue<ClrTypeAnnotation>(s)?.ClrType)
                .Where(t => t is not null)
                .Distinct()
                .ToArray();
            if (alreadyKnown.Length > 0)
            {
                auxBuilder.Ignore(alreadyKnown);
            }

            foreach (var type in missingTypes)
            {
                if (type.IsEnum)
                {
                    auxBuilder.AddEnumType(type);
                    continue;
                }

                if (HasKey(type))
                {
                    auxBuilder.AddEntityType(type);
                    continue;
                }

                auxBuilder.AddComplexType(type);
            }

            var auxModel = auxBuilder.GetEdmModel() as EdmModel;
            if (auxModel is null)
            {
                return;
            }

            foreach (var element in auxModel.SchemaElements.OfType<IEdmSchemaElement>())
            {
                if (element is not IEdmSchemaType schemaType
                    || model.FindDeclaredType(schemaType.FullName()) is not null)
                {
                    continue;
                }

                model.AddElement(schemaType);

                // Carry the ClrTypeAnnotation across so downstream consumers
                // (e.g. ConventionBasedAnnotationModelBuilder.ApplyAnnotations) can resolve
                // the CLR type for further enrichment.
                var clrAnnotation = auxModel.GetAnnotationValue<ClrTypeAnnotation>(schemaType);
                if (clrAnnotation is not null)
                {
                    model.SetAnnotationValue(schemaType, clrAnnotation);
                }
            }
        }

        private static bool HasKey(Type type)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttributes(true).Any(a => a.GetType().Name == "KeyAttribute"))
                {
                    return true;
                }

                if (string.Equals(property.Name, "Id", StringComparison.Ordinal)
                    || string.Equals(property.Name, type.Name + "Id", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
