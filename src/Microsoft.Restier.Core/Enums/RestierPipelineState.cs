// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents the different parts of the Restier request execution pipeline.
    /// </summary>
    public enum RestierPipelineState
    {
        /// <summary>
        /// Represents the first step of the pipeline, when Restier checks to see if the call is allowed.
        /// </summary>
        Authorization = 1,

        /// <summary>
        /// Represents the second step of the pipeline, where the payload is validated.
        /// </summary>
        Validation = 2,

        /// <summary>
        /// Represents the third step of the pipeline, where the developer can change the payload before it is submitted.
        /// </summary>
        PreSubmit = 3,

        /// <summary>
        /// Represents the fourth step of the pipeline, where the action is executed against the Entity Framework DbContext.
        /// </summary>
        Submit = 4,

        /// <summary>
        /// Represents the fifth step of the pipeline, where you can spin off other work after the action has completed successfully.
        /// </summary>
        PostSubmit = 5,
    }
}