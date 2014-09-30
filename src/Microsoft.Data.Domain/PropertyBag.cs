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
using System.Collections.Generic;

namespace Microsoft.Data.Domain
{
    /// <summary>
    /// Represents a bag of properties.
    /// </summary>
    public class PropertyBag
    {
        private readonly IDictionary<string, object> _properties =
            new Dictionary<string, object>();

        /// <summary>
        /// Indicates if this object has a property.
        /// </summary>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// <c>true</c> if this object has the
        /// property; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool HasProperty(string name)
        {
            Ensure.NotNull(name, "name");
            return this._properties.ContainsKey(name);
        }

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the property.
        /// </typeparam>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// The value of the property.
        /// </returns>
        public T GetProperty<T>(string name)
        {
            Ensure.NotNull(name, "name");
            var value = this.GetProperty(name);
            if (!(value is T))
            {
                value = default(T);
            }
            return (T)value;
        }

        /// <summary>
        /// Gets a property.
        /// </summary>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <returns>
        /// The value of the property.
        /// </returns>
        public virtual object GetProperty(string name)
        {
            Ensure.NotNull(name, "name");
            object value = null;
            this._properties.TryGetValue(name, out value);
            return value;
        }

        /// <summary>
        /// Sets a property.
        /// </summary>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        /// <param name="value">
        /// A value for the property.
        /// </param>
        public void SetProperty(string name, object value)
        {
            Ensure.NotNull(name, "name");
            this._properties[name] = value;
        }

        /// <summary>
        /// Clears a property.
        /// </summary>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        public void ClearProperty(string name)
        {
            Ensure.NotNull(name, "name");
            this._properties.Remove(name);
        }
    }
}
