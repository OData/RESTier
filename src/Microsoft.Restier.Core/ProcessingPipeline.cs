// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Restier.Core.Contexts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// 
        /// </summary>
        /// <param name="provider"></param>
        public ProcessingPipeline(IServiceProvider provider)
        {
            // @robertmclaws: I know this smells like BS, but if we make the API the "key", we can't use FromKeyedServicesAttribute.
            QueryHandlers = provider.GetKeyedServices<IQueryPipelineHandler>(typeof(TApi)).ToList() ?? [];
            OperationHandlers = provider.GetKeyedServices<IOperationPipelineHandler>(typeof(TApi)).ToList() ?? [];
            SubmissionHandlers = provider.GetKeyedServices<ISubmissionPipelineHandler>(typeof(TApi)).ToList() ?? [];
        }

        #endregion

        /// <summary>
        /// Processes read-only data requests.
        /// </summary>
        /// <param name="queryContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// @robertmclaws: Type parameters can't be used in attributes, so <see cref="FromKeyedServicesAttribute"/> can't be used here.
        /// </remarks>
        internal async Task ProcessQueryAsync(QueryContext queryContext, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"ProcessQueryAsync hit for path {queryContext.IncomingUrl}");
            foreach (var handler in QueryHandlers)
            {
                //await handler.ProcessAsync(queryContext, cancellationToken);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Processes custom operations that are attached to the model but not directly tied to CRUD requests.
        /// </summary>
        /// <param name="operationContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// @robertmclaws: Type parameters can't be used in attributes, so <see cref="FromKeyedServicesAttribute"/> can't be used here.
        /// </remarks>
        internal async Task ProcessOperationAsync(OperationContext operationContext, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"ProcessOperationAsync hit for path {operationContext.IncomingUrl}");
            foreach (var handler in OperationHandlers)
            {
                //await handler.ProcessAsync(operationContext, cancellationToken);
            }
            await Task.CompletedTask;
        }

        /// <summary>
        /// Processes CRUD persistence requests.
        /// </summary>
        /// <param name="submissionContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <remarks>
        /// @robertmclaws: Type parameters can't be used in attributes, so <see cref="FromKeyedServicesAttribute"/> can't be used here.
        /// </remarks>
        internal async Task ProcessSubmissionAsync(SubmissionContext submissionContext, CancellationToken cancellationToken)
        {
            Trace.WriteLine($"ProcessSubmissionAsync hit for path {submissionContext.IncomingUrl}");
            foreach (var handler in SubmissionHandlers)
            {
                //await handler.ProcessSAsync(submissionContext, cancellationToken);
            }
            await Task.CompletedTask;
        }

    }

}