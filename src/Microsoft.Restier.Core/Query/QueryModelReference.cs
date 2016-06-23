// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.OData.Edm;

namespace Microsoft.Restier.Core.Query
{
    /// <summary>
    /// Represents a reference to query data in terms of a model.
    /// </summary>
    public class QueryModelReference
    {
        private readonly IEdmEntitySet edmEntitySet;

        private readonly IEdmType edmType;

        internal QueryModelReference()
        {
        }

        internal QueryModelReference(IEdmEntitySet entitySet, IEdmType type)
        {
            this.edmEntitySet = entitySet;
            this.edmType = type;
        }

        /// <summary>
        /// Gets the entity set that ultimately contains the data.
        /// </summary>
        public virtual IEdmEntitySet EntitySet
        {
            get
            {
                return this.edmEntitySet;
            }
        }

        /// <summary>
        /// Gets the type of the data, if any.
        /// </summary>
        public virtual IEdmType Type
        {
            get
            {
                return this.edmType;
            }
        }
    }

    /// <summary>
    /// Represents a reference to data source stub in terms of a model.
    /// </summary>
    public class DataSourceStubModelReference : QueryModelReference
    {
        private readonly QueryContext context;
        private readonly string namespaceName;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceStubModelReference" /> class.
        /// </summary>
        /// <param name="context">
        /// A query context.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or function import.
        /// </param>
        internal DataSourceStubModelReference(QueryContext context, string name)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(name, "name");
            this.context = context;
            this.name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceStubModelReference" /> class referring to a function.
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
        internal DataSourceStubModelReference(
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
    /// Represents a reference to parameter data in terms of a model.
    /// It does not have special logic
    /// </summary>
    public class ParameterModelReference : QueryModelReference
    {
        internal ParameterModelReference(IEdmEntitySet entitySet, IEdmType type)
            : base(entitySet, type)
        {
        }
    }

    /// <summary>
    /// Represents a reference to property data in terms of a model.
    /// </summary>
    public class PropertyModelReference : QueryModelReference
    {
        private readonly string propertyName;
        private IEdmProperty property;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyModelReference" /> class.
        /// </summary>
        /// <param name="propertyName">
        /// The name of a property.
        /// </param>
        /// <param name="property">
        /// EDM property instance
        /// </param>
        /// <param name="source">
        /// A source query model reference.
        /// </param>
        internal PropertyModelReference(string propertyName, IEdmProperty property, QueryModelReference source)
        {
            Ensure.NotNull(propertyName, "propertyName");
            this.propertyName = propertyName;

            Ensure.NotNull(property, "property");
            this.property = property;
            this.Source = source;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyModelReference" /> class.
        /// </summary>
        /// <param name="source">
        /// A source query model reference.
        /// </param>
        /// <param name="propertyName">
        /// The name of a property.
        /// </param>
        internal PropertyModelReference(QueryModelReference source, string propertyName)
        {
            Ensure.NotNull(propertyName, "propertyName");
            this.propertyName = propertyName;

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
                if (this.Source != null)
                {
                    return this.Source.EntitySet;
                }

                return null;
            }
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
                if (property != null)
                {
                    return property;
                }

                if (Source != null)
                {
                    var structuredType = Source.Type as IEdmStructuredType;
                    if (structuredType != null)
                    {
                        property = structuredType.FindProperty(this.propertyName);
                        return property;
                    }
                }

                return null;
            }
        }
    }
}
