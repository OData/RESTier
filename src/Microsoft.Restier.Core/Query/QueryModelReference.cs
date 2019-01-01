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
            edmEntitySet = entitySet;
            edmType = type;
        }

        /// <summary>
        /// Gets the entity set that ultimately contains the data.
        /// </summary>
        public virtual IEdmEntitySet EntitySet => edmEntitySet;

        /// <summary>
        /// Gets the type of the data, if any.
        /// </summary>
        public virtual IEdmType Type => edmType;
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
            Ensure.NotNull(context, nameof(context));
            Ensure.NotNull(name, nameof(name));
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
            Ensure.NotNull(context, nameof(context));
            Ensure.NotNull(namespaceName, nameof(namespaceName));
            Ensure.NotNull(name, nameof(name));
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
                var entitySet = Element as IEdmEntitySet;
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
                if (namespaceName == null)
                {
                    var source = Element as IEdmNavigationSource;
                    if (source != null)
                    {
                        return source.Type;
                    }

                    var function = Element as IEdmFunctionImport;
                    if (function != null)
                    {
                        return function.Function.ReturnType.Definition;
                    }
                }
                else
                {
                    var function = Element as IEdmFunction;
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
                if (namespaceName == null)
                {
                    return context.Model.EntityContainer.Elements
                        .SingleOrDefault(e => e.Name == name);
                }
                else
                {
                    return context.Model.SchemaElements
                        .SingleOrDefault(e =>
                            e.Namespace == namespaceName &&
                            e.Name == name);
                }
            }
        }
    }
}
