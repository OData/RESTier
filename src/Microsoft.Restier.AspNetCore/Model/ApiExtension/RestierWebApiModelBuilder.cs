// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.Restier.Core.Model;

namespace Microsoft.Restier.AspNetCore.Model
{
    /// <summary>
    /// This is a RESTier model builder extends the Entity Sets retrieved from the
    /// OR Mapper (like Entity Framework) with the properties and relations found on the Clr Types.
    /// </summary>
    public class RestierWebApiModelBuilder : IModelBuilder
    {
        private readonly RestierWebApiModelExtender _modelExtender;

        /// <summary>
        /// Gets or sets the Inner model builder.
        /// </summary>
        public IModelBuilder Inner { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestierWebApiModelBuilder"/> class.
        /// </summary>
        /// <param name="modelExtender">The model extender.</param>
        public RestierWebApiModelBuilder(RestierWebApiModelExtender modelExtender)
        {
            _modelExtender = modelExtender;
        }

        /// <inheritdoc/>
        public IEdmModel GetEdmModel()
        {
            var innerModel = Inner?.GetEdmModel();

            if (innerModel is null)
            {
                // There is no model returned so return an empty model.
                var emptyModel = new EdmModel();
                emptyModel.EnsureEntityContainer(_modelExtender.TargetApiType);
                return emptyModel;
            }

            var edmModel = innerModel as EdmModel;
            if (edmModel is null)
            {
                // The model returned is not an EDM model.
                return innerModel;
            }

            _modelExtender.ScanForDeclaredPublicProperties();
            _modelExtender.BuildEntitySetsAndSingletons(edmModel);
            _modelExtender.AddNavigationPropertyBindings(edmModel);
            return edmModel;
        }
    }
}
