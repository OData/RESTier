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
    /// Represents a hook point that profiles other hook points.
    /// </summary>
    /// <remarks>
    /// This is a multi-cast hook point whose instances
    /// are used in the original order of registration.
    /// </remarks>
    public interface IDomainProfiler
    {
        /// <summary>
        /// Profiles a hook point.
        /// </summary>
        /// <typeparam name="T">
        /// The type of a hook point.
        /// </typeparam>
        /// <param name="instance">
        /// An instance of the hook point.
        /// </param>
        /// <returns>
        /// A profiled instance of the hook point.
        /// </returns>
        T Profile<T>(T instance);
    }
}
