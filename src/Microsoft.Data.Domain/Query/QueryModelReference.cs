using System;
using System.Linq;
using Microsoft.OData.Edm;

namespace Microsoft.Data.Domain.Query
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
    /// Represents a reference to domain data in terms of a model.
    /// </summary>
    public class DomainDataReference : QueryModelReference
    {
        private readonly QueryContext _context;
        private readonly string _namespaceName;
        private readonly string _name;

        /// <summary>
        /// Initializes a new domain data reference.
        /// </summary>
        /// <param name="context">
        /// A query context.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or function import.
        /// </param>
        public DomainDataReference(QueryContext context, string name)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(name, "name");
            this._context = context;
            this._name = name;
        }

        /// <summary>
        /// Initializes a new domain data reference to a function.
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
        public DomainDataReference(QueryContext context,
            string namespaceName, string name)
        {
            Ensure.NotNull(context, "context");
            Ensure.NotNull(namespaceName, "namespaceName");
            Ensure.NotNull(name, "name");
            this._context = context;
            this._namespaceName = namespaceName;
            this._name = name;
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
                // TODO: others
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
                if (this._namespaceName == null)
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
        /// Gets the element representing the domain data.
        /// </summary>
        public IEdmElement Element
        {
            get
            {
                if (this._namespaceName == null)
                {
                    return this._context.Model.EntityContainer.Elements
                        .SingleOrDefault(e => e.Name == this._name);
                }
                else
                {
                    return this._context.Model.SchemaElements
                        .SingleOrDefault(e =>
                            e.Namespace == this._namespaceName &&
                            e.Name == this._name);
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
        /// Initializes a new derived data reference.
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
        /// Initializes a new collection element reference.
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
        private readonly string _propertyName;

        /// <summary>
        /// Initializes a new property data reference.
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
            this._propertyName = propertyName;
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
                    return structuredType.FindProperty(this._propertyName);
                }
                return null;
            }
        }
    }
}
