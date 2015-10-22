﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a reference to query data in terms of a model.
    /// </summary>
    public abstract class QueryModelReference
    {
        internal QueryModelReference()
        {
        }

        /// <summary>
        /// Gets the entity set that ultimately contains the data.
        /// </summary>
        public abstract IEdmEntitySet EntitySet { get; }

        /// <summary>
        /// Gets the type of the data, if any.
        /// </summary>
        public abstract IEdmType Type { get; }
    }

    /// <summary>
    /// Represents a reference to API data in terms of a model.
    /// </summary>
    public class ApiDataReference : QueryModelReference
    {
        private readonly QueryContext context;
        private readonly string namespaceName;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDataReference" /> class.
        /// </summary>
        /// <param name="context">
        /// A query context.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or function import.
        /// </param>
        public ApiDataReference(QueryContext context, string name)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(name, "name");
            this.context = context;
            this.name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiDataReference" /> class referring to a function.
        /// </summary>
        /// <param name="context">
        /// A query context.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing the function.
        /// </param>
        /// <param name="name">
        /// The name of a function.
        /// </param>
        public ApiDataReference(
            QueryContext context,
            string namespaceName,
            string name)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");
            this.context = context;
            this.namespaceName = namespaceName;
            this.name = name;
        }

        /// <summary>
        /// Gets the entity set that ultimately contains the data.
        /// </summary>
        public override IEdmEntitySet EntitySet
        {
            get
            {
                var entitySet = this.Element as IEdmEntitySet;
                if (entitySet != null)
                {
                    return entitySet;
                }

                // TODO GitHubIssue#30 : others
                return null;
            }
        }

        /// <summary>
        /// Gets the type of the data, if any.
        /// </summary>
        public override IEdmType Type
        {
            get
            {
                if (this.namespaceName == null)
                {
                    var source = this.Element as IEdmNavigationSource;
                    if (source != null)
                    {
                        return source.Type;
                    }

                    var function = this.Element as IEdmFunctionImport;
                    if (function != null)
                    {
                        return function.Function.ReturnType.Definition;
                    }
                }
                else
                {
                    var function = this.Element as IEdmFunction;
                    if (function != null)
                    {
                        return function.ReturnType.Definition;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the element representing the API data.
        /// </summary>
        public IEdmElement Element
        {
            get
            {
                if (this.namespaceName == null)
                {
                    return this.context.Model.EntityContainer.Elements
                        .SingleOrDefault(e => e.Name == this.name);
                }
                else
                {
                    return this.context.Model.SchemaElements
                        .SingleOrDefault(e =>
                            e.Namespace == this.namespaceName &&
                            e.Name == this.name);
                }
            }
        }
    }

    /// <summary>
    /// Represents a reference to derived data in terms of a model.
    /// </summary>
    public class DerivedDataReference : QueryModelReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedDataReference" /> class.
        /// </summary>
        /// <param name="source">
        /// A source query model reference.
        /// </param>
        public DerivedDataReference(QueryModelReference source)
        {
            Ensure.NotNull(source, "source");
            this.Source = source;
        }

        /// <summary>
        /// Gets the source of the derived data.
        /// </summary>
        public QueryModelReference Source { get; private set; }

        /// <summary>
        /// Gets the entity set that contains the data.
        /// </summary>
        public override IEdmEntitySet EntitySet
        {
            get
            {
                return this.Source.EntitySet;
            }
        }

        /// <summary>
        /// Gets the type of the data.
        /// </summary>
        public override IEdmType Type
        {
            get
            {
                return this.Source.Type;
            }
        }
    }

    /// <summary>
    /// Represents a reference to element data in terms of a model.
    /// </summary>
    public class CollectionElementReference : DerivedDataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionElementReference" /> class.
        /// </summary>
        /// <param name="source">
        /// A source query model reference.
        /// </param>
        public CollectionElementReference(QueryModelReference source)
            : base(source)
        {
        }

        /// <summary>
        /// Gets the type of the data.
        /// </summary>
        public override IEdmType Type
        {
            get
            {
                var collectionType = this.Source.Type as IEdmCollectionType;
                if (collectionType != null)
                {
                    return collectionType.ElementType.Definition;
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Represents a reference to property data in terms of a model.
    /// </summary>
    public class PropertyDataReference : DerivedDataReference
    {
        private readonly string propertyName;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDataReference" /> class.
        /// </summary>
        /// <param name="source">
        /// A source query model reference.
        /// </param>
        /// <param name="propertyName">
        /// The name of a property.
        /// </param>
        public PropertyDataReference(QueryModelReference source, string propertyName)
            : base(source)
        {
            Ensure.NotNull(propertyName, "propertyName");
            this.propertyName = propertyName;
        }

        /// <summary>
        /// Gets the type of the queryable data.
        /// </summary>
        public override IEdmType Type
        {
            get
            {
                if (this.Property != null)
                {
                    return this.Property.Type.Definition;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the property representing the property data.
        /// </summary>
        public IEdmProperty Property
        {
            get
            {
                var structuredType = this.Source.Type as IEdmStructuredType;
                if (structuredType != null)
                {
                    return structuredType.FindProperty(this.propertyName);
                }

                return null;
            }
        }
    }
}
