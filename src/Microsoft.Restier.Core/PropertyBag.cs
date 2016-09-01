// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Restier.Core
{
    /// <summary>
    /// Represents a bag of properties.
    /// </summary>
    internal class PropertyBag
    {
        private readonly IDictionary<string, object> properties =
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
        public bool HasProperty(string name)
        {
            Ensure.NotNull(name, "name");
            return this.properties.ContainsKey(name);
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
        public object GetProperty(string name)
        {
            Ensure.NotNull(name, "name");
            object value = null;
            this.properties.TryGetValue(name, out value);
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
            this.properties[name] = value;
        }

        /// <summary>
        /// Removes a property.
        /// </summary>
        /// <param name="name">
        /// The name of a property.
        /// </param>
        public void RemoveProperty(string name)
        {
            Ensure.NotNull(name, "name");
            this.properties.Remove(name);
        }
    }
}
