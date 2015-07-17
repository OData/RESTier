// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Microsoft.Restier.Conventions;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.EntityFramework;
using Microsoft.Restier.WebApi.Test.Services.Trippin.Models;

namespace Microsoft.Restier.WebApi.Test.Services.Trippin.Domain
{
    [Test]
    public class TrippinDomain : DbDomain<TrippinModel>
    {
        public TrippinModel Context { get { return DbContext; } }
        /// <summary>
        /// Implements an action import.
        /// </summary>
        [Action]
        public void ResetDataSource()
        {
            TrippinModel.ResetDataSource();
        }
    }

    public class TestAttribute : DomainParticipantAttribute
    {
        private EnableConventionsAttribute enableConventionsAttribute = new EnableConventionsAttribute();

        public override void Configure(
            DomainConfiguration configuration,
            Type type)
        {
            enableConventionsAttribute.Configure(configuration, type);
            ConventionalActionProvider.ApplyTo(configuration, type);
            configuration.AddHookPoint(typeof(IModelExtender), new CustomExtender());
        }

        public override void Initialize(DomainContext context, Type type, object instance)
        {
            enableConventionsAttribute.Initialize(context, type, instance);
        }
    }

    public class CustomExtender : IModelExtender
    {
        public async System.Threading.Tasks.Task ExtendModelAsync(
            ModelContext context,
            System.Threading.CancellationToken cancellationToken)
        {
            var entityContainer = (EdmEntityContainer)context.Model.EntityContainer;
            var type = (IEdmEntityType)context.Model
                .FindDeclaredType("Microsoft.Restier.WebApi.Test.Services.Trippin.Models.Person");
            entityContainer.AddSingleton("Me", type);
        }
    }
}