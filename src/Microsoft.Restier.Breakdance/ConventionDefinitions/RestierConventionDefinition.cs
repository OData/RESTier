// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Restier.Core;

namespace Microsoft.Restier.Breakdance
{

    /// <summary>
    /// 
    /// </summary>
    public abstract class RestierConventionDefinition
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RestierPipelineState? PipelineState { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="pipelineState"></param>
        internal RestierConventionDefinition(string name, RestierPipelineState pipelineState)
        {
            Name = name;
            PipelineState = pipelineState;
        }

        #endregion

    }

}