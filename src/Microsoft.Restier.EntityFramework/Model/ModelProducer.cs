// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Library;
using Microsoft.OData.Edm.Library.Annotations;
using Microsoft.OData.Edm.Library.Values;
using Microsoft.OData.Edm.Vocabularies.V1;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using EdmModel = Microsoft.OData.Edm.Library.EdmModel;
using EdmProperty = System.Data.Entity.Core.Metadata.Edm.EdmProperty;

namespace Microsoft.Restier.EntityFramework.Model
{
    /// <summary>
    /// Represents a model producer that uses the
    /// metadata workspace accessible from a DbContext.
    /// </summary>
    internal class ModelProducer : IModelBuilder
    {
        private const string AnnotationSchema =
            "http://schemas.microsoft.com/ado/2009/02/edm/annotation";

        private const string StoreGeneratedPatternKey = ":StoreGeneratedPattern";
        private const string StoreGeneratedPatternValueComputed = "Computed";
        private const string ColumnTypeNameDateUpper = "DATE";
        private const string ColumnTypeNameTimeUpper = "TIME";

        private static IDictionary<PrimitiveTypeKind, EdmPrimitiveTypeKind>
            primitiveTypeKindMap = new Dictionary<PrimitiveTypeKind, EdmPrimitiveTypeKind>
        {
            { PrimitiveTypeKind.Binary, EdmPrimitiveTypeKind.Binary },
            { PrimitiveTypeKind.Boolean, EdmPrimitiveTypeKind.Boolean },
            { PrimitiveTypeKind.Byte, EdmPrimitiveTypeKind.Byte },
            { PrimitiveTypeKind.DateTime, EdmPrimitiveTypeKind.DateTimeOffset },
            { PrimitiveTypeKind.DateTimeOffset, EdmPrimitiveTypeKind.DateTimeOffset },
            { PrimitiveTypeKind.Decimal, EdmPrimitiveTypeKind.Decimal },
            { PrimitiveTypeKind.Double, EdmPrimitiveTypeKind.Double },
            { PrimitiveTypeKind.Geography, EdmPrimitiveTypeKind.Geography },
            { PrimitiveTypeKind.GeographyCollection, EdmPrimitiveTypeKind.GeographyCollection },
            { PrimitiveTypeKind.GeographyLineString, EdmPrimitiveTypeKind.GeographyLineString },
            { PrimitiveTypeKind.GeographyMultiLineString, EdmPrimitiveTypeKind.GeographyMultiLineString },
            { PrimitiveTypeKind.GeographyMultiPoint, EdmPrimitiveTypeKind.GeographyMultiPoint },
            { PrimitiveTypeKind.GeographyMultiPolygon, EdmPrimitiveTypeKind.GeographyMultiPolygon },
            { PrimitiveTypeKind.GeographyPoint, EdmPrimitiveTypeKind.GeographyPoint },
            { PrimitiveTypeKind.GeographyPolygon, EdmPrimitiveTypeKind.GeographyPolygon },
            { PrimitiveTypeKind.Geometry, EdmPrimitiveTypeKind.Geometry },
            { PrimitiveTypeKind.GeometryCollection, EdmPrimitiveTypeKind.GeometryCollection },
            { PrimitiveTypeKind.GeometryLineString, EdmPrimitiveTypeKind.GeometryLineString },
            { PrimitiveTypeKind.GeometryMultiLineString, EdmPrimitiveTypeKind.GeometryMultiLineString },
            { PrimitiveTypeKind.GeometryMultiPoint, EdmPrimitiveTypeKind.GeometryMultiPoint },
            { PrimitiveTypeKind.GeometryMultiPolygon, EdmPrimitiveTypeKind.GeometryMultiPolygon },
            { PrimitiveTypeKind.GeometryPoint, EdmPrimitiveTypeKind.GeometryPoint },
            { PrimitiveTypeKind.GeometryPolygon, EdmPrimitiveTypeKind.GeometryPolygon },
            { PrimitiveTypeKind.Guid, EdmPrimitiveTypeKind.Guid },
            { PrimitiveTypeKind.Int16, EdmPrimitiveTypeKind.Int16 },
            { PrimitiveTypeKind.Int32, EdmPrimitiveTypeKind.Int32 },
            { PrimitiveTypeKind.Int64, EdmPrimitiveTypeKind.Int64 },
            { PrimitiveTypeKind.SByte, EdmPrimitiveTypeKind.SByte },
            { PrimitiveTypeKind.Single, EdmPrimitiveTypeKind.Single },
            { PrimitiveTypeKind.String, EdmPrimitiveTypeKind.String },
            { PrimitiveTypeKind.Time, EdmPrimitiveTypeKind.Duration }
        };

        private static IModelBuilder instance;

        private ModelProducer()
        {
        }

        /// <summary>
        /// Gets the single instance of this model producer.
        /// </summary>
        public static IModelBuilder Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ModelProducer();
                }

                return instance;
            }
        }

        /// <inheritdoc/>
        public Task<IEdmModel> GetModelAsync(InvocationContext context, CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");
            var model = new EdmModel();
            var apiContext = context.ApiContext;
            var dbContext = apiContext.GetProperty<DbContext>(DbApiConstants.DbContextKey);
            var elementMap = new Dictionary<MetadataItem, IEdmElement>();
            var efModel = (dbContext as IObjectContextAdapter)
                .ObjectContext.MetadataWorkspace;
            var namespaceName = efModel.GetItems<EntityType>(DataSpace.CSpace)
                .Select(t => efModel.GetObjectSpaceType(t).NamespaceName)
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

            var efEntityContainer = efModel.GetItems<EntityContainer>(DataSpace.CSpace).Single();
            var entityContainer = new EdmEntityContainer(namespaceName, efEntityContainer.Name);
            elementMap.Add(efEntityContainer, entityContainer);

            // TODO GitHubIssue#36 : support complex and enumeration types
            foreach (var efEntitySet in efEntityContainer.EntitySets)
            {
                var efEntityType = efEntitySet.ElementType;
                if (elementMap.ContainsKey(efEntityType))
                {
                    continue;
                }

                List<EdmStructuralProperty> concurrencyProperties;
                var entityType = CreateEntityType(efModel, efEntityType, model, elementMap, out concurrencyProperties);
                model.AddElement(entityType);
                elementMap.Add(efEntityType, entityType);
                var entitySet = entityContainer.AddEntitySet(efEntitySet.Name, entityType);
                if (concurrencyProperties != null)
                {
                    model.SetOptimisticConcurrencyAnnotation(entitySet, concurrencyProperties);
                }

                elementMap.Add(efEntitySet, entitySet);
            }

            foreach (var efAssociationSet in efEntityContainer.AssociationSets)
            {
                AddNavigationProperties(efAssociationSet, elementMap);
                AddNavigationPropertyBindings(efAssociationSet, elementMap);
            }

            // TODO GitHubIssue#36 : support function imports
            model.AddElement(entityContainer);

            return Task.FromResult<IEdmModel>(model);
        }

        private static IEdmEntityType CreateEntityType(
            MetadataWorkspace efModel,
            EntityType efEntityType,
            EdmModel model,
            IDictionary<MetadataItem, IEdmElement> elementMap,
            out List<EdmStructuralProperty> concurrencyProperties)
        {
            // TODO GitHubIssue#36 : support complex and entity inheritance
            var objectSpaceType = efModel.GetObjectSpaceType(efEntityType) as EntityType;
            var entityType = new EdmEntityType(objectSpaceType.NamespaceName, objectSpaceType.Name);
            concurrencyProperties = null;
            foreach (var efProperty in efEntityType.Properties)
            {
                var type = GetTypeReference(efProperty, model, elementMap);
                if (type != null)
                {
                    string defaultValue = null;
                    if (efProperty.DefaultValue != null)
                    {
                        defaultValue = efProperty.DefaultValue.ToString();
                    }

                    var property = entityType.AddStructuralProperty(
                        efProperty.Name,
                        type,
                        defaultValue,
                        EdmConcurrencyMode.None); // alway None:replaced by OptimisticConcurrency annotation
                    MetadataProperty storeGeneratedPattern = null;
                    efProperty.MetadataProperties.TryGetValue(
                        AnnotationSchema + StoreGeneratedPatternKey,
                        true,
                        out storeGeneratedPattern);

                    if (storeGeneratedPattern != null &&
                        (string)storeGeneratedPattern.Value == StoreGeneratedPatternValueComputed)
                    {
                        SetComputedAnnotation(model, property);
                    }

                    if (efProperty.ConcurrencyMode == ConcurrencyMode.Fixed)
                    {
                        concurrencyProperties = concurrencyProperties ?? new List<EdmStructuralProperty>();
                        concurrencyProperties.Add(property);
                    }
                }
            }

            entityType.AddKeys(efEntityType.KeyProperties
                .Select(p => entityType.FindProperty(p.Name))
                .Cast<IEdmStructuralProperty>());
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

        private static IEdmTypeReference GetTypeReference(
            EdmProperty efProperty,
            EdmModel model,
            IDictionary<MetadataItem, IEdmElement> elementMap)
        {
            if (efProperty.IsPrimitiveType)
            {
                return GetPrimitiveTypeReference(efProperty);
            }
            else if (efProperty.IsComplexType)
            {
                return GetComplexTypeReference(efProperty, model, elementMap);
            }
            else if (efProperty.IsEnumType)
            {
                return GetEnumTypeReference(efProperty, model, elementMap);
            }

            // TODO GitHubIssue#103 : Choose property error message for unknown type
            return null;
        }

        private static IEdmComplexTypeReference GetComplexTypeReference(
            EdmProperty efProperty,
            EdmModel model,
            IDictionary<MetadataItem, IEdmElement> elementMap)
        {
            var efComplexType = efProperty.ComplexType;
            EdmComplexType complexType;
            IEdmElement element;

            if (elementMap.TryGetValue(efComplexType, out element))
            {
                complexType = (EdmComplexType)element;
            }
            else
            {
                complexType = new EdmComplexType(efComplexType.NamespaceName, efComplexType.Name);
                elementMap.Add(efComplexType, complexType);
                model.AddElement(complexType);

                foreach (var property in efComplexType.Properties)
                {
                    var propertyTypeRef = GetTypeReference(property, model, elementMap);
                    if (propertyTypeRef != null)
                    {
                        complexType.AddStructuralProperty(property.Name, propertyTypeRef);
                    }
                }
            }

            return new EdmComplexTypeReference(complexType, efProperty.Nullable);
        }

        private static IEdmEnumTypeReference GetEnumTypeReference(
            EdmProperty efProperty,
            EdmModel model,
            IDictionary<MetadataItem, IEdmElement> elementMap)
        {
            var efEnumType = efProperty.EnumType;
            EdmEnumType enumType;
            IEdmElement element;

            if (elementMap.TryGetValue(efEnumType, out element))
            {
                enumType = (EdmEnumType)element;
            }
            else
            {
                enumType = new EdmEnumType(efEnumType.NamespaceName, efEnumType.Name);
                elementMap.Add(efEnumType, enumType);
                model.AddElement(enumType);

                foreach (var member in efEnumType.Members)
                {
                    var longValue = Convert.ToInt64(member.Value, CultureInfo.InvariantCulture);
                    enumType.AddMember(member.Name, new EdmIntegerConstant(longValue));
                }
            }

            return new EdmEnumTypeReference(enumType, efProperty.Nullable);
        }

        private static IEdmPrimitiveTypeReference GetPrimitiveTypeReference(EdmProperty efProperty)
        {
            EdmPrimitiveTypeKind kind;
            var efKind = efProperty.PrimitiveType.PrimitiveTypeKind;
            if (!primitiveTypeKindMap.TryGetValue(efKind, out kind))
            {
                // TODO GitHubIssue#103 : Choose property error message for unknown type
                return null;
            }

            // The EDM type of a System.DateTime or System.TimeSpan property can be determined by
            // the DB type name specified through the ColumnAttribute on that property.
            if (efKind == PrimitiveTypeKind.DateTime || efKind == PrimitiveTypeKind.Time)
            {
                var columnTypeName = GetColumnTypeName(efProperty).ToUpperInvariant();
                if (efKind == PrimitiveTypeKind.DateTime && columnTypeName == ColumnTypeNameDateUpper)
                {
                    kind = EdmPrimitiveTypeKind.Date;
                }
                else if (efKind == PrimitiveTypeKind.Time && columnTypeName == ColumnTypeNameTimeUpper)
                {
                    kind = EdmPrimitiveTypeKind.TimeOfDay;
                }
            }

            switch (kind)
            {
                default:
                    return EdmCoreModel.Instance.GetPrimitive(
                        kind,
                        efProperty.Nullable);
                case EdmPrimitiveTypeKind.Binary:
                    return EdmCoreModel.Instance.GetBinary(
                        efProperty.IsMaxLength,
                        efProperty.MaxLength,
                        efProperty.Nullable);
                case EdmPrimitiveTypeKind.Decimal:
                    return EdmCoreModel.Instance.GetDecimal(
                        efProperty.Precision,
                        efProperty.Scale,
                        efProperty.Nullable);
                case EdmPrimitiveTypeKind.String:
                    return EdmCoreModel.Instance.GetString(
                        efProperty.IsMaxLength,
                        efProperty.MaxLength,
                        efProperty.IsUnicode,
                        efProperty.Nullable);
                case EdmPrimitiveTypeKind.DateTimeOffset:
                    if (efProperty.PrimitiveType.PrimitiveTypeKind == PrimitiveTypeKind.DateTime)
                    {
                        MetadataProperty metadata;
                        efProperty.MetadataProperties.TryGetValue("Configuration", true, out metadata);
                        if (metadata != null)
                        {
                            object config = metadata.Value;
                            var columnType = (string)config.GetType().GetProperty("ColumnType").GetValue(config);

                            if (string.Equals(columnType, "date", StringComparison.OrdinalIgnoreCase))
                            {
                                return EdmCoreModel.Instance.GetDate(efProperty.Nullable);
                            }
                        }
                    }

                    return EdmCoreModel.Instance.GetTemporal(
                        kind,
                        efProperty.Precision,
                        efProperty.Nullable);
                case EdmPrimitiveTypeKind.Duration:
                    {
                        MetadataProperty metadata;
                        efProperty.MetadataProperties.TryGetValue("Configuration", true, out metadata);
                        if (metadata != null)
                        {
                            object config = metadata.Value;
                            var columnType = (string)config.GetType().GetProperty("ColumnType").GetValue(config);

                            if (string.Equals(columnType, "time", StringComparison.OrdinalIgnoreCase))
                            {
                                return EdmCoreModel.Instance.GetTimeOfDay(efProperty.Nullable);
                            }
                        }

                        return EdmCoreModel.Instance.GetTemporal(
                            kind,
                            efProperty.Precision,
                            efProperty.Nullable);
                    }
            }
        }

        private static void AddNavigationProperties(
            AssociationSet efAssociationSet,
            IDictionary<MetadataItem, IEdmElement> elementMap)
        {
            if (efAssociationSet.AssociationSetEnds.Count != 2)
            {
                return;
            }

            var efAssociation = efAssociationSet.ElementType;
            var navPropertyInfos = new EdmNavigationPropertyInfo[2];
            for (var i = 0; i < 2; i++)
            {
                var efEnd = efAssociation.AssociationEndMembers[i];
                var efEntityType = efEnd.GetEntityType();
                if (!elementMap.ContainsKey(efEntityType))
                {
                    continue;
                }

                var entityType = elementMap[efEntityType] as IEdmEntityType;
                var efNavProperty = efEntityType.NavigationProperties
                    .SingleOrDefault(np => np.FromEndMember == efEnd);
                if (efNavProperty == null)
                {
                    continue;
                }

                var efTargetEntityType = efNavProperty
                    .ToEndMember.GetEntityType();
                if (!elementMap.ContainsKey(efTargetEntityType))
                {
                    continue;
                }

                var targetEntityType = elementMap[efTargetEntityType] as IEdmEntityType;
                navPropertyInfos[i] = new EdmNavigationPropertyInfo()
                {
                    ContainsTarget = false,
                    Name = efNavProperty.Name,
                    OnDelete = efEnd.DeleteBehavior == OperationAction.Cascade
                        ? EdmOnDeleteAction.Cascade : EdmOnDeleteAction.None,
                    Target = targetEntityType,
                    TargetMultiplicity = GetEdmMultiplicity(
                        efNavProperty.ToEndMember.RelationshipMultiplicity)
                };

                var constraint = efAssociation.Constraint;
                if (constraint != null && constraint.ToRole == efEnd)
                {
                    navPropertyInfos[i].DependentProperties = constraint.ToProperties
                        .Select(p => entityType.FindProperty(p.Name) as IEdmStructuralProperty);
                    navPropertyInfos[i].PrincipalProperties = constraint.FromProperties
                        .Select(p => targetEntityType.FindProperty(p.Name) as IEdmStructuralProperty);
                }
            }

            if (navPropertyInfos[0] == null && navPropertyInfos[1] != null)
            {
                var efEnd = efAssociation.AssociationEndMembers[1];
                var efEntityType = efEnd.GetEntityType();
                var entityType = elementMap[efEntityType] as EdmEntityType;
                if (entityType.FindProperty(navPropertyInfos[1].Name) == null)
                {
                    entityType.AddUnidirectionalNavigation(navPropertyInfos[1]);
                }
            }

            if (navPropertyInfos[0] != null && navPropertyInfos[1] == null)
            {
                var efEnd = efAssociation.AssociationEndMembers[0];
                var efEntityType = efEnd.GetEntityType();
                var entityType = elementMap[efEntityType] as EdmEntityType;
                if (entityType.FindProperty(navPropertyInfos[0].Name) == null)
                {
                    entityType.AddUnidirectionalNavigation(navPropertyInfos[0]);
                }
            }

            if (navPropertyInfos[0] != null && navPropertyInfos[1] != null)
            {
                var efEnd = efAssociation.AssociationEndMembers[0];
                var efEntityType = efEnd.GetEntityType();
                var entityType = elementMap[efEntityType] as EdmEntityType;
                if (entityType.FindProperty(navPropertyInfos[0].Name) == null)
                {
                    entityType.AddBidirectionalNavigation(
                        navPropertyInfos[0], navPropertyInfos[1]);
                }
            }
        }

        private static void AddNavigationPropertyBindings(
            AssociationSet efAssociationSet,
            IDictionary<MetadataItem, IEdmElement> elementMap)
        {
            if (efAssociationSet.AssociationSetEnds.Count != 2)
            {
                return;
            }

            for (var i = 0; i < 2; i++)
            {
                var efSetEnd = efAssociationSet.AssociationSetEnds[i];
                var efEnd = efSetEnd.CorrespondingAssociationEndMember;
                var efEntityType = efEnd.GetEntityType();
                if (!elementMap.ContainsKey(efEntityType))
                {
                    continue;
                }

                var entityType = elementMap[efEntityType] as IEdmEntityType;
                var efNavProperty = efEntityType.NavigationProperties
                    .Where(np => np.FromEndMember == efEnd)
                    .SingleOrDefault();
                if (efNavProperty == null)
                {
                    continue;
                }

                var navProperty = entityType.FindProperty(
                    efNavProperty.Name) as IEdmNavigationProperty;
                if (navProperty == null)
                {
                    continue;
                }

                var entitySet = elementMap[efSetEnd.EntitySet] as EdmEntitySet;
                var efTargetSetEnd = efAssociationSet.AssociationSetEnds
                    .Single(e => e.Name == efNavProperty.ToEndMember.Name);
                entitySet.AddNavigationTarget(navProperty, elementMap[efTargetSetEnd.EntitySet] as IEdmEntitySet);
            }
        }

        private static EdmMultiplicity GetEdmMultiplicity(
            RelationshipMultiplicity relMultiplicity)
        {
            switch (relMultiplicity)
            {
                case RelationshipMultiplicity.Many:
                    return EdmMultiplicity.Many;
                case RelationshipMultiplicity.One:
                    return EdmMultiplicity.One;
                case RelationshipMultiplicity.ZeroOrOne:
                    return EdmMultiplicity.ZeroOrOne;
                default:
                    return EdmMultiplicity.Unknown;
            }
        }

        private static IEnumerable<Attribute> GetClrAttributes(EdmProperty efProperty)
        {
            var metadataProperty = efProperty.MetadataProperties.SingleOrDefault(p => p.Name == "ClrAttributes");
            if (metadataProperty == null)
            {
                return Enumerable.Empty<Attribute>();
            }

            return (IEnumerable<Attribute>)metadataProperty.Value;
        }

        private static string GetColumnTypeName(EdmProperty efProperty)
        {
            var columnAttribute = GetClrAttributes(efProperty).OfType<ColumnAttribute>().SingleOrDefault();
            if (columnAttribute != null)
            {
                // Returns an empty string for ColumnTypeAttribute without TypeName specified.
                return columnAttribute.TypeName ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
