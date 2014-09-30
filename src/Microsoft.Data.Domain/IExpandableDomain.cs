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

namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Represents an expandable domain.
    /// </summary>
    /// <remarks>
    /// An expandable domain is a domain that can operate given a domain
    /// configuration that was derived from its own. To achieve this, it
    /// defers creation of its domain context until either the Context
    /// property is accessed, in which case its own configuration is used,
    /// or the Initialize method is called with a derived configuration.
    /// </remarks>
    public interface IExpandableDomain : IDomain
    {
        /// <summary>
        /// Gets the configuration for this expandable domain.
        /// </summary>
        DomainConfiguration Configuration { get; }

        /// <summary>
        /// Gets a value indicating if this
        /// expandable domain has been initialized.
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes this expandable domain.
        /// </summary>
        /// <param name="derivedConfiguration">
        /// A derived domain configuration.
        /// </param>
        void Initialize(DomainConfiguration derivedConfiguration);
    }
}
