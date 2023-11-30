// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Restier.Core
{

    /// <summary>
    /// This is a prototype for the new execution pipeline.
    /// </summary>
    /// <remarks>
    /// Conceptually this is similar to SimpleMessageBus. In that pattern, we have an IMessage that comes into the pipeline, and then
    /// the registered handlers are executed in series or parallel. In this case they will always execute in series, but that will allow
    /// the developer to have multiple validation steps, for example, that can be executed in a specific order.
    /// </remarks>
    public class ProcessingPipeline<TApi>
    {

        #region Public Properties

        /// <summary>
        /// 
        /// </summary>
        public List<IQueryPipelineHandler> QueryHandlers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<IOperationPipelineHandler> OperationHandlers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<ISubmissionPipelineHandler> SubmissionHandlers { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// You 
        /// </summary>
        /// <param name="provider"></param>
        public ProcessingPipeline(IKeyedServiceProvider provider)
        {
            // @robertmclaws: I know this smells like BS, but if we make the API the "key", we can't use FromKeyedServicesAttribute.
            QueryHandlers = provider.GetKeyedServices<IQueryPipelineHandler>(typeof(TApi)).ToList();
            OperationHandlers = provider.GetKeyedServices<IOperationPipelineHandler>(typeof(TApi)).ToList();
            SubmissionHandlers = provider.GetKeyedServices<ISubmissionPipelineHandler>(typeof(TApi)).ToList();
        }

        #endregion

        /// <summary>
        /// Processes read-only data requests.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// @robertmclaws: Type parameters can't be used in attributes, so <see cref="FromKeyedServicesAttribute"/> can't be used here.
        /// </remarks>
        internal async Task ProcessQuery(CancellationToken cancellationToken)
        {
            foreach (var handler in QueryHandlers)
            {
                //await handler.ProcessQuery(cancellationToken);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Processes custom operations that are attached to the model but not directly tied to CRUD requests.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// @robertmclaws: Type parameters can't be used in attributes, so <see cref="FromKeyedServicesAttribute"/> can't be used here.
        /// </remarks>
        internal async Task ProcessOperation(CancellationToken cancellationToken)
        {
            foreach (var handler in OperationHandlers)
            {
                //await handler.ProcessOperation(cancellationToken);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Processeses CRUD persistence requests.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// @robertmclaws: Type parameters can't be used in attributes, so <see cref="FromKeyedServicesAttribute"/> can't be used here.
        /// </remarks>
        internal async Task ProcessSubmission(CancellationToken cancellationToken)
        {
            foreach (var handler in SubmissionHandlers)
            {
                //await handler.ProcessSubmission(cancellationToken);
            }
            await Task.CompletedTask;
        }

    }

}
