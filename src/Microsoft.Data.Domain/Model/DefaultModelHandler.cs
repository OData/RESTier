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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;

namespace Microsoft.Data.Domain.Model
{
    /// <summary>
    /// Represents the default model handler.
    /// </summary>
    public class DefaultModelHandler : IModelHandler
    {
        /// <summary>
        /// Asynchronously executes the model flow.
        /// </summary>
        /// <param name="context">
        /// The model context.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous
        /// operation whose result is a domain model.
        /// </returns>
        public async Task<IEdmModel> GetModelAsync(
            ModelContext context,
            CancellationToken cancellationToken)
        {
            Ensure.NotNull(context, "context");

            // STEP 1: produce model
            var producer = context.GetHookPoint<IModelProducer>();
            if (producer != null)
            {
                context.Model = await producer.ProduceModelAsync(
                    context, cancellationToken);
            }
            if (context.Model == null)
            {
                context.Model = new EdmModel();
            }

            // STEP 2: extend model
            var extenders = context.GetHookPoints<IModelExtender>();
            foreach (var extender in extenders)
            {
                await extender.ExtendModelAsync(context, cancellationToken);
            }

            return context.Model;
        }
    }
}
