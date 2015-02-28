// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Annotations;

namespace Microsoft.Restier.Core.Model
{
    // TODO GitHubIssue#27 : Support full Edm interfaces
    internal class DomainModel : IEdmModel
    {
        private readonly Lazy<ConcurrentDictionary<string, DomainEntityContainer>> _entityContainers =
            new Lazy<ConcurrentDictionary<string, DomainEntityContainer>>(LazyThreadSafetyMode.PublicationOnly);

        public DomainModel(DomainConfiguration configuration, IEdmModel model)
        {
            this.Configuration = configuration;
            this.InnerModel = model;
        }

        public DomainModel(InvocationContext context, IEdmModel model)
        {
            this.Configuration = context.DomainContext.Configuration;
            this.Context = context;
            this.InnerModel = model;
        }

        public DomainConfiguration Configuration { get; private set; }

        public InvocationContext Context { get; private set; }

        public IEdmModel InnerModel { get; private set; }

        public IEnumerable<IEdmModel> ReferencedModels
        {
            get { return this.InnerModel.ReferencedModels; }
        }

        public IEdmDirectValueAnnotationsManager DirectValueAnnotationsManager
        {
            get { return this.InnerModel.DirectValueAnnotationsManager; }
        }

        public IEnumerable<string> DeclaredNamespaces
        {
            get
            {
                return this.InnerModel.DeclaredNamespaces.Where(
                    namespaceName => this.IsSchemaVisible(namespaceName));
            }
        }

        public IEnumerable<IEdmSchemaElement> SchemaElements
        {
            get
            {
                return this.InnerModel.SchemaElements.Select(element =>
                {
                    var entityContainer = element as IEdmEntityContainer;
                    if (entityContainer != null)
                    {
                        element = this.GetDomainEntityContainer(
                            entityContainer);
                    }
                    return element;
                }).Where(element => this.IsSchemaElementVisible(element));
            }
        }

        public IEdmEntityContainer EntityContainer
        {
            get
            {
                var entityContainer = this.GetDomainEntityContainer(
                    this.InnerModel.EntityContainer);
                if (entityContainer != null &&
                    !this.IsSchemaElementVisible(entityContainer))
                {
                    entityContainer = null;
                }
                return entityContainer;
            }
        }

        public IEnumerable<IEdmVocabularyAnnotation> VocabularyAnnotations
        {
            get
            {
                return this.InnerModel.VocabularyAnnotations.Where(annotation
                    => this.IsVocabularyAnnotatableVisible(annotation.Target));
            }
        }

        public IEdmSchemaType FindDeclaredType(string qualifiedName)
        {
            var type = this.InnerModel.FindDeclaredType(qualifiedName);
            if (type != null && !this.IsSchemaElementVisible(type))
            {
                type = null;
            }
            return type;
        }

        public IEnumerable<IEdmStructuredType> FindDirectlyDerivedTypes(
            IEdmStructuredType baseType)
        {
            return this.InnerModel.FindDirectlyDerivedTypes(baseType).Where(
                type => this.IsSchemaElementVisible(type as IEdmSchemaElement));
        }

        public IEnumerable<IEdmOperation> FindDeclaredOperations(
            string qualifiedName)
        {
            return this.InnerModel.FindDeclaredOperations(qualifiedName)
                .Where(operation => this.IsSchemaElementVisible(operation));
        }

        public IEnumerable<IEdmOperation> FindDeclaredBoundOperations(
            IEdmType bindingType)
        {
            return this.InnerModel.FindDeclaredBoundOperations(bindingType)
                .Where(operation => this.IsSchemaElementVisible(operation));
        }

        public IEnumerable<IEdmOperation> FindDeclaredBoundOperations(
            string qualifiedName, IEdmType bindingType)
        {
            return this.InnerModel.FindDeclaredBoundOperations(qualifiedName, bindingType)
                .Where(operation => this.IsSchemaElementVisible(operation));
        }

        public IEdmValueTerm FindDeclaredValueTerm(string qualifiedName)
        {
            var term = this.InnerModel.FindDeclaredValueTerm(qualifiedName);
            if (term != null && !this.IsSchemaElementVisible(term))
            {
                term = null;
            }
            return term;
        }

        public IEnumerable<IEdmVocabularyAnnotation> FindDeclaredVocabularyAnnotations(
            IEdmVocabularyAnnotatable element)
        {
            return this.InnerModel.FindDeclaredVocabularyAnnotations(element);
        }

        private bool IsSchemaVisible(string namespaceName)
        {
            return this.SchemaElements.Any(element =>
                element.Namespace == namespaceName);
        }

        private bool IsSchemaElementVisible(IEdmSchemaElement element)
        {
            var entityContainer = element as IEdmEntityContainer;
            if (entityContainer != null)
            {
                return entityContainer.Elements.Any();
            }
            return this.Configuration
                .GetHookPoints<IModelVisibilityFilter>().Reverse()
                .All(filter => filter.IsVisible(this.Configuration,
                    this.Context, this.InnerModel, element));
        }

        private bool IsVocabularyAnnotatableVisible(
            IEdmVocabularyAnnotatable element)
        {
            var schemaElement = element as IEdmSchemaElement;
            if (schemaElement != null)
            {
                return this.IsSchemaElementVisible(schemaElement);
            }
            var entityContainerElement = element as IEdmEntityContainerElement;
            if (entityContainerElement != null)
            {
                return entityContainerElement.Container
                    .Elements.Contains(entityContainerElement);
            }
            return true;
        }

        private DomainEntityContainer GetDomainEntityContainer(
            IEdmEntityContainer entityContainer)
        {
            var domainEntityContainer = this._entityContainers.Value.GetOrAdd(
                entityContainer.FullName(), name => new DomainEntityContainer(this, entityContainer));
            return domainEntityContainer;
        }
    }

    internal class DomainEntityContainer : IEdmEntityContainer
    {
        private DomainModel _model;
        private IEdmEntityContainer _innerContainer;

        public DomainEntityContainer(
            DomainModel model,
            IEdmEntityContainer innerContainer)
        {
            this._model = model;
            this._innerContainer = innerContainer;
        }

        public string Namespace
        {
            get { return this._innerContainer.Namespace; }
        }

        public string Name
        {
            get { return this._innerContainer.Name; }
        }

        public EdmSchemaElementKind SchemaElementKind
        {
            get { return this._innerContainer.SchemaElementKind; }
        }

        public IEnumerable<IEdmEntityContainerElement> Elements
        {
            get
            {
                return this._innerContainer.Elements
                    .Where(element => this.IsElementVisible(element));
            }
        }

        public IEdmEntitySet FindEntitySet(string setName)
        {
            var entitySet = this._innerContainer.FindEntitySet(setName);
            if (entitySet != null && !this.IsElementVisible(entitySet))
            {
                entitySet = null;
            }
            return entitySet;
        }

        public IEdmSingleton FindSingleton(string singletonName)
        {
            var singleton = this._innerContainer.FindSingleton(singletonName);
            if (singleton != null && !this.IsElementVisible(singleton))
            {
                singleton = null;
            }
            return singleton;
        }

        public IEnumerable<IEdmOperationImport> FindOperationImports(
            string operationName)
        {
            return this._innerContainer.FindOperationImports(operationName)
                .Where(opImport => this.IsElementVisible(opImport));
        }

        private bool IsElementVisible(IEdmEntityContainerElement element)
        {
            return this._model.Configuration
                .GetHookPoints<IModelVisibilityFilter>().Reverse()
                .All(filter => filter.IsVisible(this._model.Configuration,
                    this._model.Context, this._model.InnerModel, element));
        }
    }
}
