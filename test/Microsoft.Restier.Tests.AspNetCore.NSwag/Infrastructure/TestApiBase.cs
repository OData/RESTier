// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using System.Linq;

namespace Microsoft.Restier.Tests.AspNetCore.NSwag.Infrastructure
{

    public class TestApi : ApiBase
    {

        public TestApi(IEdmModel model, IQueryHandler queryHandler, ISubmitHandler submitHandler)
            : base(model, queryHandler, submitHandler)
        {
        }

        public IQueryable<TestEntity> Items => Enumerable.Empty<TestEntity>().AsQueryable();

    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Inner <see cref="IModelBuilder"/> used by <see cref="TestApi"/>: declares the
    /// <see cref="TestEntity"/> entity set so <c>RestierWebApiModelBuilder</c> can
    /// then extend it with conventions discovered on <see cref="TestApi"/>.
    /// </summary>
    public class TestApiModelBuilder : IModelBuilder
    {
        public IModelBuilder Inner { get; set; }

        public IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<TestEntity>(nameof(TestApi.Items));
            return builder.GetEdmModel();
        }
    }

}
