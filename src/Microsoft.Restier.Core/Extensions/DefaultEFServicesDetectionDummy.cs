// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{

    #region Private Members

    /// <summary>
    /// Dummy class to detect double registration of Default Entity framework services inside a container.
    /// </summary>
    /// <remarks>This class is located here because it's shared between both EF and EF Core on the
    /// netstandard2.1 platforms.</remarks>
    internal sealed class DefaultEFProviderServicesDetectionDummy
    {

    }

    #endregion
}
