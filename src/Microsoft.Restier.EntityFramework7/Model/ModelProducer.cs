// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Annotations;
using Microsoft.OData.Edm.Library.Values;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.Restier.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EdmModel = Microsoft.OData.Edm.Library.EdmModel;

namespace Microsoft.Restier.EntityFramework.Model
{
    /// <summary>
    /// Represents a model producer that uses the
    /// metadata workspace accessible from a DbContext.
    /// </summary>
    public class ModelProducer : IModelProducer
    {
        private const string c_annotationSchema =
            "http://schemas.microsoft.com/ado/2009/02/edm/annotation";

        private ModelProducer()
        {
        }

        private static readonly ModelProducer instance = new ModelProducer();

        /// <summary>
        /// Gets the single instance of this model producer.
        /// </summary>
        public static ModelProducer Instance { get { return instance; } }

        /// <summary>
        /// Asynchronously produces a base model.
        /// </summary>
        /// <param name="context">
        /// The model context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is the base model.
        /// </returns>
        public Task<EdmModel> ProduceModelAsync(
            ModelContext context,
            CancellationToken cancellationToken)
        {
            var model = new EdmModel();
            var domainContext = context.DomainContext;
            var dbContext = domainContext.GetProperty<DbContext>("DbContext");
            var elementMap = new Dictionary<IMetadata, IEdmElement>();
            var efModel = dbContext.Model;
            var namespaceName = efModel.EntityTypes
                .Select(t => t.HasClrType ? t.Type.Namespace : null)
                .Where(t => t != null)
                .GroupBy(nameSpace => nameSpace)
                .Select(group => new
                {
                    NameSpace = group.Key,
                    Count = group.Count(),
                })
                .OrderByDescending(nsItem => nsItem.Count)
                .Select(nsItem => nsItem.NameSpace)
                .FirstOrDefault();
            if (namespaceName == null)
            {
                // When dbContext has not a namespace, just use its type name as namespace.
                namespaceName = dbContext.GetType().Namespace ?? dbContext.GetType().Name;
            }

            var entityTypes = efModel.EntityTypes;
            var entityContainer = new EdmEntityContainer(
                namespaceName, "Container");

            var dbSetProperties = dbContext.GetType().
                GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).
                Where(e => e.PropertyType.IsGenericType && e.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)).
                ToDictionary(e => e.PropertyType.GetGenericArguments()[0]);

            // TODO GitHubIssue#36 : support complex and entity inheritance
            foreach (var efEntityType in entityTypes)
            {
                if (elementMap.ContainsKey(efEntityType))
                {
                    continue;
                }
                List<EdmStructuralProperty> concurrencyProperties;
                var entityType = ModelProducer.CreateEntityType(
                    efModel, efEntityType, model, out concurrencyProperties);
                model.AddElement(entityType);
                elementMap.Add(efEntityType, entityType);

                System.Reflection.PropertyInfo propInfo;
                if (dbSetProperties.TryGetValue(efEntityType.Type, out propInfo))
                {
                    var entitySet = entityContainer.AddEntitySet(propInfo.Name, entityType);
                    if (concurrencyProperties != null)
                    {
                        model.SetOptimisticConcurrencyAnnotation(entitySet, concurrencyProperties);
                    }
                }
            }

            foreach (var efEntityType in entityTypes)
            {
                foreach (var navi in efEntityType.Navigations)
                {
                    ModelProducer.AddNavigationProperties(
                        efModel, navi, model, elementMap);
                    ModelProducer.AddNavigationPropertyBindings(
                        efModel, navi, entityContainer, elementMap);
                }
            }

            // TODO GitHubIssue#36 : support function imports
            model.AddElement(entityContainer);

            return Task.FromResult(model);
        }

        private static IEdmEntityType CreateEntityType(
            IModel efModel, IEntityType efEntityType,
            EdmModel model, out List<EdmStructuralProperty> concurrencyProperties)
        {
            // TODO GitHubIssue#36 : support complex and entity inheritance
            var entityType = new EdmEntityType(
                efEntityType.Type.Namespace, efEntityType.Type.Name);
            concurrencyProperties = null;
            foreach (var efProperty in efEntityType.Properties)
            {
                var type = ModelProducer.GetPrimitiveTypeReference(efProperty);
                if (type != null)
                {
                    string defaultValue = null;
                    if (efProperty.Relational().DefaultValue != null)
                    {
                        defaultValue = efProperty.Relational().DefaultExpression;
                    }
                    var property = entityType.AddStructuralProperty(
                        efProperty.Name, type, defaultValue,
                        EdmConcurrencyMode.None); // alway None:replaced by OptimisticConcurrency annotation

                    // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
                    if (efProperty.GenerateValueOnAdd || efProperty.IsStoreComputed)
                    {
                        SetComputedAnnotation(model, property);
                    }

                    if (efProperty.IsConcurrencyToken)
                    {
                        concurrencyProperties = concurrencyProperties ?? new List<EdmStructuralProperty>();
                        concurrencyProperties.Add(property);
                    }
                }
            }

            var key = efEntityType.TryGetPrimaryKey();
            if (key != null)
            {
                entityType.AddKeys(key.Properties
                    .Select(p => entityType.FindProperty(p.Name))
                    .Cast<IEdmStructuralProperty>());
            }
            return entityType;
        }

        private static void SetComputedAnnotation(EdmModel model, IEdmProperty target)
        {
            // when 'target' is <Key> property, V4's 'Computed' also has the meaning of OData V3's 'Identity'.
            var val = new EdmBooleanConstant(value: true);
            var annotation = new EdmAnnotation(target, CoreVocabularyModel.ComputedTerm, val);
            annotation.SetSerializationLocation(model, EdmVocabularyAnnotationSerializationLocation.Inline);
            model.SetVocabularyAnnotation(annotation);
        }

        private static IEdmPrimitiveTypeReference GetPrimitiveTypeReference(
            IProperty efProperty)
        {
            var kind = EdmPrimitiveTypeKind.None;
            var propertyType = efProperty.PropertyType;
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(System.Nullable<>))
            {
                propertyType = propertyType.GetGenericArguments()[0];
            }

            if (!s_primitiveTypeKindMap.TryGetValue(propertyType, out kind))
            {
                return null;
            }
            switch (kind)
            {
                default:
                    return EdmCoreModel.Instance.GetPrimitive(
                        kind, efProperty.IsNullable);
                case EdmPrimitiveTypeKind.Binary:
                    return EdmCoreModel.Instance.GetBinary(
                        efProperty.MaxLength < 0, efProperty.MaxLength,
                        efProperty.IsNullable);
                case EdmPrimitiveTypeKind.Decimal:
                    // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
                    //return EdmCoreModel.Instance.GetDecimal(
                    //    efProperty.Precision, efProperty.Scale,
                    //    efProperty.Nullable);
                    return EdmCoreModel.Instance.GetDecimal(efProperty.IsNullable);
                case EdmPrimitiveTypeKind.String:
                    // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
                    return EdmCoreModel.Instance.GetString(
                        efProperty.MaxLength < 0, efProperty.MaxLength,
                        null, efProperty.IsNullable);
                case EdmPrimitiveTypeKind.DateTimeOffset:
                case EdmPrimitiveTypeKind.Duration:
                    // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
                    //return EdmCoreModel.Instance.GetTemporal(
                    //    kind, efProperty.Precision, efProperty.Nullable);
                    return EdmCoreModel.Instance.GetTemporal(kind, efProperty.IsNullable);
            }
        }

        private static IDictionary<Type, EdmPrimitiveTypeKind>
            s_primitiveTypeKindMap = new Dictionary<Type, EdmPrimitiveTypeKind>()
        {
            { typeof(byte[]), EdmPrimitiveTypeKind.Binary },
            { typeof(System.IO.Stream), EdmPrimitiveTypeKind.Binary },
            { typeof(bool), EdmPrimitiveTypeKind.Boolean },
            //{ PrimitiveTypeKind.DateTime, EdmPrimitiveTypeKind.DateTimeOffset },
            { typeof(DateTimeOffset), EdmPrimitiveTypeKind.DateTimeOffset },
            { typeof(Decimal), EdmPrimitiveTypeKind.Decimal },
            { typeof(double), EdmPrimitiveTypeKind.Double },
            // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
            //{ PrimitiveTypeKind.Geography, EdmPrimitiveTypeKind.Geography },
            //{ PrimitiveTypeKind.GeographyCollection, EdmPrimitiveTypeKind.GeographyCollection },
            //{ PrimitiveTypeKind.GeographyLineString, EdmPrimitiveTypeKind.GeographyLineString },
            //{ PrimitiveTypeKind.GeographyMultiLineString, EdmPrimitiveTypeKind.GeographyMultiLineString },
            //{ PrimitiveTypeKind.GeographyMultiPoint, EdmPrimitiveTypeKind.GeographyMultiPoint },
            //{ PrimitiveTypeKind.GeographyMultiPolygon, EdmPrimitiveTypeKind.GeographyMultiPolygon },
            //{ PrimitiveTypeKind.GeographyPoint, EdmPrimitiveTypeKind.GeographyPoint },
            //{ PrimitiveTypeKind.GeographyPolygon, EdmPrimitiveTypeKind.GeographyPolygon },
            //{ PrimitiveTypeKind.Geometry, EdmPrimitiveTypeKind.Geometry },
            //{ PrimitiveTypeKind.GeometryCollection, EdmPrimitiveTypeKind.GeometryCollection },
            //{ PrimitiveTypeKind.GeometryLineString, EdmPrimitiveTypeKind.GeometryLineString },
            //{ PrimitiveTypeKind.GeometryMultiLineString, EdmPrimitiveTypeKind.GeometryMultiLineString },
            //{ PrimitiveTypeKind.GeometryMultiPoint, EdmPrimitiveTypeKind.GeometryMultiPoint },
            //{ PrimitiveTypeKind.GeometryMultiPolygon, EdmPrimitiveTypeKind.GeometryMultiPolygon },
            //{ PrimitiveTypeKind.GeometryPoint, EdmPrimitiveTypeKind.GeometryPoint },
            //{ PrimitiveTypeKind.GeometryPolygon, EdmPrimitiveTypeKind.GeometryPolygon },
            { typeof(Guid), EdmPrimitiveTypeKind.Guid },
            { typeof(Int16), EdmPrimitiveTypeKind.Int16 },
            { typeof(Int32), EdmPrimitiveTypeKind.Int32 },
            { typeof(Int64), EdmPrimitiveTypeKind.Int64 },
            { typeof(sbyte), EdmPrimitiveTypeKind.SByte },
            { typeof(Single), EdmPrimitiveTypeKind.Single },
            { typeof(string), EdmPrimitiveTypeKind.String },
            { typeof(TimeSpan), EdmPrimitiveTypeKind.Duration }
        };

        private static void AddNavigationProperties(
            IModel efModel, INavigation navi,
            EdmModel model, IDictionary<IMetadata, IEdmElement> elementMap)
        {
            if (!navi.PointsToPrincipal)
            {
                return;
            }
            var naviPair = new INavigation[] { navi, navi.TryGetInverse() };
            var navPropertyInfos = new EdmNavigationPropertyInfo[2];
            for (var i = 0; i < 2; i++)
            {
                var efEnd = naviPair[i];
                if (efEnd == null) continue;

                var efEntityType = efEnd.EntityType;
                if (!elementMap.ContainsKey(efEntityType))
                {
                    continue;
                }
                var entityType = elementMap[efEntityType] as IEdmEntityType;
                var efTargetEntityType = naviPair[i].GetTargetType();
                if (!elementMap.ContainsKey(efTargetEntityType))
                {
                    continue;
                }
                var targetEntityType = elementMap[
                    efTargetEntityType] as IEdmEntityType;
                navPropertyInfos[i] = new EdmNavigationPropertyInfo()
                {
                    ContainsTarget = false,
                    Name = naviPair[i].Name,
                    // TODO GitHubIssue#57: Complete EF7 to EDM model mapping
                    //OnDelete = efEnd.DeleteBehavior == OperationAction.Cascade
                    //    ? EdmOnDeleteAction.Cascade : EdmOnDeleteAction.None,
                    OnDelete = EdmOnDeleteAction.None,
                    Target = targetEntityType,
                    TargetMultiplicity = ModelProducer.GetEdmMultiplicity(
                        naviPair[i]),
                };
                var foreignKey = naviPair[i].ForeignKey;
                if (foreignKey != null && naviPair[i].PointsToPrincipal)
                {
                    navPropertyInfos[i].DependentProperties = foreignKey.Properties
                        .Select(p => entityType.FindProperty(p.Name) as IEdmStructuralProperty);
                    navPropertyInfos[i].PrincipalProperties = foreignKey.ReferencedProperties
                        .Select(p => targetEntityType.FindProperty(p.Name) as IEdmStructuralProperty);
                }
            }
            if (navPropertyInfos[0] == null && navPropertyInfos[1] != null)
            {
                var efEntityType = navi.GetTargetType();
                var entityType = elementMap[efEntityType] as EdmEntityType;
                if (entityType.FindProperty(navPropertyInfos[1].Name) == null)
                {
                    entityType.AddUnidirectionalNavigation(navPropertyInfos[1]);
                }
            }
            if (navPropertyInfos[0] != null && navPropertyInfos[1] == null)
            {
                var efEntityType = navi.EntityType;
                var entityType = elementMap[efEntityType] as EdmEntityType;
                if (entityType.FindProperty(navPropertyInfos[0].Name) == null)
                {
                    entityType.AddUnidirectionalNavigation(navPropertyInfos[0]);
                }
            }
            if (navPropertyInfos[0] != null && navPropertyInfos[1] != null)
            {
                var efEntityType = navi.EntityType;
                var entityType = elementMap[efEntityType] as EdmEntityType;
                if (entityType.FindProperty(navPropertyInfos[0].Name) == null)
                {
                    entityType.AddBidirectionalNavigation(
                        navPropertyInfos[0], navPropertyInfos[1]);
                }
            }
        }

        private static void AddNavigationPropertyBindings(
            IModel efModel, INavigation navi,
            EdmEntityContainer container, IDictionary<IMetadata, IEdmElement> elementMap)
        {
            if (!navi.PointsToPrincipal)
            {
                return;
            }
            var naviPair = new INavigation[] { navi, navi.TryGetInverse() };
            for (var i = 0; i < 2; i++)
            {
                if (naviPair[i] == null) continue;

                var efEntityType = naviPair[i].EntityType;
                if (!elementMap.ContainsKey(efEntityType))
                {
                    continue;
                }
                var entityType = elementMap[efEntityType] as IEdmEntityType;
                var navProperty = entityType.FindProperty(
                    naviPair[i].Name) as IEdmNavigationProperty;
                if (navProperty == null)
                {
                    continue;
                }
                var entitySet = (EdmEntitySet)container.EntitySets().
                    First(e => e.EntityType() == entityType);
                var targetEntitySet = container.EntitySets().
                    First(e => e.EntityType() == navProperty.ToEntityType());
                entitySet.AddNavigationTarget(navProperty, targetEntitySet);
            }
        }

        private static EdmMultiplicity GetEdmMultiplicity(
            INavigation navi)
        {
            if (navi.IsCollection())
            {
                return EdmMultiplicity.Many;
            }
            if (navi.ForeignKey.IsRequired)
            {
                return EdmMultiplicity.One;
            }
            return EdmMultiplicity.ZeroOrOne;
        }
    }
}
