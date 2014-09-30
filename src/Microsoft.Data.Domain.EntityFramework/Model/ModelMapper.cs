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
using System.Data.Entity;
using Microsoft.Data.Domain.Model;

namespace Microsoft.Data.Domain.EntityFramework.Model
{
    /// <summary>
    /// Represents a model mapper based on a DbContext.
    /// </summary>
    public class ModelMapper: IModelMapper
    {
        private readonly Type _dbContextType;

        /// <summary>
        /// Initializes a new model mapper.
        /// </summary>
        /// <param name="dbContextType">
        /// The type of a DbContext class.
        /// </param>
        public ModelMapper(Type dbContextType)
        {
            Ensure.NotNull(dbContextType, "dbContextType");
            this._dbContextType = dbContextType;
        }

        /// <summary>
        /// Tries to get the relevant type of an entity
        /// set, singleton, or composable function import.
        /// </summary>
        /// <param name="context">
        /// A domain context.
        /// </param>
        /// <param name="name">
        /// The name of an entity set, singleton or composable function import.
        /// </param>
        /// <param name="relevantType">
        /// When this method returns, provides the
        /// relevant type of the queryable source.
        /// </param>
        /// <returns>
        /// <c>true</c> if the relevant type was
        /// provided; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetRelevantType(
            DomainContext context,
            string name,
            out Type relevantType)
        {
            // TODO: support something beyond entity sets
            relevantType = null;
            var property = this._dbContextType.GetProperty(name);
            if (property != null)
            {
                var type = property.PropertyType;
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(DbSet<>))
                {
                    relevantType = type.GetGenericArguments()[0];
                }
            }
            return relevantType != null;
        }

        /// <summary>
        /// Tries to get the relevant type of a composable function.
        /// </summary>
        /// <param name="context">
        /// A domain context.
        /// </param>
        /// <param name="namespaceName">
        /// The name of a namespace containing a composable function.
        /// </param>
        /// <param name="name">
        /// The name of composable function.
        /// </param>
        /// <param name="relevantType">
        /// When this method returns, provides the
        /// relevant type of the composable function.
        /// </param>
        /// <returns>
        /// <c>true</c> if the relevant type was
        /// provided; otherwise, <c>false</c>.
        /// </returns>
        public bool TryGetRelevantType(
            DomainContext context,
            string namespaceName, string name,
            out Type relevantType)
        {
            // TODO: support composable function imports
            relevantType = null;
            return false;
        }
    }
}
