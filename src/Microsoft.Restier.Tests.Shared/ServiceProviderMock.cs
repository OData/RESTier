// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.Restier.Core;
using Microsoft.Restier.Core.Model;
using Microsoft.Restier.Core.Query;
using Microsoft.Restier.Core.Submit;
using Moq;

namespace Microsoft.Restier.Tests.Shared
{
    /// <summary>
    /// A class to setup an IServiceProvider instance that contains all the neccessary Mocks.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ServiceProviderMock
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceProviderMock"/> class.
        /// </summary>
        public ServiceProviderMock()
        {
            ServiceProvider = new Mock<IServiceProvider>();

            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExpressionSourcer))).Returns(new Mock<IQueryExpressionSourcer>().Object);

            this.QueryExpressionAuthorizer = new Mock<IQueryExpressionAuthorizer>();

            // authorize any query as default.
            this.QueryExpressionAuthorizer.Setup(x => x.Authorize(It.IsAny<QueryExpressionContext>())).Returns(true);

            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExpressionAuthorizer))).Returns(this.QueryExpressionAuthorizer.Object);
            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExpressionExpander))).Returns(new Mock<IQueryExpressionExpander>().Object);

            this.QueryExpressionProcessor = new Mock<IQueryExpressionProcessor>();

            // just pass on the visited node without filter as default.
            this.QueryExpressionProcessor.Setup(x => x.Process(It.IsAny<QueryExpressionContext>())).Returns<QueryExpressionContext>(q => q.VisitedNode);

            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExpressionProcessor))).Returns(this.QueryExpressionProcessor.Object);

            this.QueryExecutor = new Mock<IQueryExecutor>();

            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExecutor))).Returns(this.QueryExecutor.Object);

            this.ChangeSetInitializer = new Mock<IChangeSetInitializer>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetInitializer))).Returns(this.ChangeSetInitializer.Object);

            this.ChangeSetItemAuthorizer = new Mock<IChangeSetItemAuthorizer>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetItemAuthorizer))).Returns(this.ChangeSetItemAuthorizer.Object);

            this.ChangeSetItemValidator = new Mock<IChangeSetItemValidator>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetItemValidator))).Returns(this.ChangeSetItemValidator.Object);

            this.ChangeSetItemFilter = new Mock<IChangeSetItemFilter>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetItemFilter))).Returns(this.ChangeSetItemFilter.Object);

            this.SubmitExecutor = new Mock<ISubmitExecutor>();

            var submitResult = new SubmitResult(new ChangeSet());

            // return the result from the context as default operation.
            this.SubmitExecutor.Setup(x => x.ExecuteSubmitAsync(It.IsAny<SubmitContext>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(submitResult));

            ServiceProvider.Setup(x => x.GetService(typeof(ISubmitExecutor))).Returns(this.SubmitExecutor.Object);

            this.ModelBuilder = new Mock<IModelBuilder>();

            var edmModel = new Mock<IEdmModel>().Object;

            // return the edm model as default.
            this.ModelBuilder.Setup(x => x.GetModel(It.IsAny<ModelContext>())).Returns(edmModel);

            ServiceProvider.Setup(x => x.GetService(typeof(IModelBuilder))).Returns(this.ModelBuilder.Object);
            this.ModelMapper = new Mock<IModelMapper>();
            ServiceProvider.Setup(x => x.GetService(typeof(IModelMapper))).Returns(this.ModelMapper.Object);

            var propertyBag = new PropertyBag();
            ServiceProvider.Setup(x => x.GetService(typeof(PropertyBag))).Returns(propertyBag);
        }

        /// <summary>
        /// Gets the mock for IServiceProvider.
        /// </summary>
        public Mock<IServiceProvider> ServiceProvider { get; private set; }

        /// <summary>
        /// Gets the mock for IModelMapper.
        /// </summary>
        public Mock<IModelMapper> ModelMapper { get; private set; }

        /// <summary>
        /// Gets the mock for the ModelBuilder.
        /// </summary>
        public Mock<IModelBuilder> ModelBuilder { get; private set; }

        /// <summary>
        /// Gets the mock for the QueryExpressionAuthorizer.
        /// </summary>
        public Mock<IQueryExpressionAuthorizer> QueryExpressionAuthorizer { get; private set; }

        /// <summary>
        /// Gets the mock for the QueryExpressionProcessor.
        /// </summary>
        public Mock<IQueryExpressionProcessor> QueryExpressionProcessor { get; }

        /// <summary>
        /// Gets the mock for the QueryExecutor.
        /// </summary>
        public Mock<IQueryExecutor> QueryExecutor { get; }

        /// <summary>
        /// Gets the mock for the ChangeSetInitializer.
        /// </summary>
        public Mock<IChangeSetInitializer> ChangeSetInitializer { get; private set; }

        /// <summary>
        /// Gets the mock for the ChangeSetItemValidator.
        /// </summary>
        public Mock<IChangeSetItemValidator> ChangeSetItemValidator { get; private set; }

        /// <summary>
        /// Gets the mock for the ChangeSetItemAuthorizer.
        /// </summary>
        public Mock<IChangeSetItemAuthorizer> ChangeSetItemAuthorizer { get; private set; }

        /// <summary>
        /// Gets the mock for the ChangeSetItemFilter.
        /// </summary>
        public Mock<IChangeSetItemFilter> ChangeSetItemFilter { get; private set; }

        /// <summary>
        /// Gets the mock for the Submit executor.
        /// </summary>
        public Mock<ISubmitExecutor> SubmitExecutor { get; private set; }
    }
}
