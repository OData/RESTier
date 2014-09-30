// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Annotations;

namespace Microsoft.Data.Domain.Model
{
    // TODO: implement domain versions of all the other
    // EDM interfaces to ensure it is impossible to get
    // to elements that are not supposed to be visible.
    internal class DomainModel : IEdmModel
    {
        private IDictionary<string, DomainEntityContainer> _entityContainers;

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
            if (this._entityContainers == null)
            {
                this._entityContainers = new Dictionary<
                    string, DomainEntityContainer>();
            }
            DomainEntityContainer domainEntityContainer = null;
            if (!this._entityContainers.TryGetValue(
                    entityContainer.FullName(),
                    out domainEntityContainer))
            {
                domainEntityContainer = new DomainEntityContainer(
                    this, entityContainer);
                this._entityContainers.Add(
                    entityContainer.FullName(),
                    domainEntityContainer);
            }
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
