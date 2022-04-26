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

            QueryExpressionAuthorizer = new Mock<IQueryExpressionAuthorizer>();

            // authorize any query as default.
            QueryExpressionAuthorizer.Setup(x => x.Authorize(It.IsAny<QueryExpressionContext>())).Returns(true);

            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExpressionAuthorizer))).Returns(QueryExpressionAuthorizer.Object);
            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExpressionExpander))).Returns(new Mock<IQueryExpressionExpander>().Object);

            QueryExpressionProcessor = new Mock<IQueryExpressionProcessor>();

            // just pass on the visited node without filter as default.
            QueryExpressionProcessor.Setup(x => x.Process(It.IsAny<QueryExpressionContext>())).Returns<QueryExpressionContext>(q => q.VisitedNode);

            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExpressionProcessor))).Returns(QueryExpressionProcessor.Object);

            QueryExecutor = new Mock<IQueryExecutor>();

            ServiceProvider.Setup(x => x.GetService(typeof(IQueryExecutor))).Returns(QueryExecutor.Object);

            ChangeSetInitializer = new Mock<IChangeSetInitializer>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetInitializer))).Returns(ChangeSetInitializer.Object);

            ChangeSetItemAuthorizer = new Mock<IChangeSetItemAuthorizer>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetItemAuthorizer))).Returns(ChangeSetItemAuthorizer.Object);

            ChangeSetItemValidator = new Mock<IChangeSetItemValidator>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetItemValidator))).Returns(ChangeSetItemValidator.Object);

            ChangeSetItemFilter = new Mock<IChangeSetItemFilter>();

            ServiceProvider.Setup(x => x.GetService(typeof(IChangeSetItemFilter))).Returns(ChangeSetItemFilter.Object);

            SubmitExecutor = new Mock<ISubmitExecutor>();

            var submitResult = new SubmitResult(new ChangeSet());

            // return the result from the context as default operation.
            SubmitExecutor.Setup(x => x.ExecuteSubmitAsync(It.IsAny<SubmitContext>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(submitResult));

            ServiceProvider.Setup(x => x.GetService(typeof(ISubmitExecutor))).Returns(SubmitExecutor.Object);

            ModelBuilder = new Mock<IModelBuilder>();

            var edmModel = new Mock<IEdmModel>().Object;

            // return the edm model as default.
            ModelBuilder.Setup(x => x.GetModel(It.IsAny<ModelContext>())).Returns(edmModel);

            ServiceProvider.Setup(x => x.GetService(typeof(IModelBuilder))).Returns(ModelBuilder.Object);
            ModelMapper = new Mock<IModelMapper>();
            ServiceProvider.Setup(x => x.GetService(typeof(IModelMapper))).Returns(ModelMapper.Object);

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
