// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Restier.Core.Authorization
{

    /// <summary>
    /// Describes the methods of verifying various CRUD operations for a given EF Entity. Useful in code generation scenarios
    /// </summary>
    public class AuthorizationEntry
    {
        /// <summary>
        /// The <see cref="Type"/> to register this <see cref="AuthorizationEntry"/> for in the <see cref="AuthorizationFactory">AuthorizationFactory's</see> backing Dictionary.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be inserted through the Restier API. The default is false.
        /// </summary>
        public Func<bool> CanInsertAction { get; set; }

        /// <summary>
        /// A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be updated through the Restier API. The default is false.
        /// </summary>
        public Func<bool> CanUpdateAction { get; set; }

        /// <summary>
        /// A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be deleted through the Restier API. The default is false.
        /// </summary>
        public Func<bool> CanDeleteAction { get; set; }

        /// <summary>
        /// Creates a new instance of an <see cref="AuthorizationEntry"/> for a given <see cref="Type"/>. Assumes all authorization checks will return false by default.
        /// </summary>
        /// <param name="t">The <see cref="Type"/> to track authorization methods for.</param>
        public AuthorizationEntry(Type t)
        {
            Type = t;
            CanInsertAction = () => false;
            CanUpdateAction = () => false;
            CanDeleteAction = () => false;
        }

        /// <summary>
        /// Creates a new instance of an <see cref="AuthorizationEntry"/> for a given <see cref="Type"/> while allowing you to specify the action to run when authorizing Inserts.
        /// </summary>
        /// <param name="t">The <see cref="Type"/> to track authorization methods for.</param>
        /// <param name="canInsertAction">A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be inserted through the Restier API.</param>
        public AuthorizationEntry(Type t, Func<bool> canInsertAction) : this(t)
        {
            CanInsertAction = canInsertAction;
        }

        /// <summary>
        /// Creates a new instance of an <see cref="AuthorizationEntry"/> for a given <see cref="Type"/> while allowing you to specify the actions to run when authorizing Inserts and Updates.
        /// </summary>
        /// <param name="t">The <see cref="Type"/> to track authorization methods for.</param>
        /// <param name="canInsertAction">A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be inserted through the Restier API.</param>
        /// <param name="canUpdateAction">A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be updated through the Restier API.</param>
        public AuthorizationEntry(Type t, Func<bool> canInsertAction, Func<bool> canUpdateAction) : this(t, canInsertAction)
        {
            CanUpdateAction = canUpdateAction;
        }

        /// <summary>
        /// Creates a new instance of an <see cref="AuthorizationEntry"/> for a given <see cref="Type"/> while allowing you to specify the actions to run when authorizing Inserts, Updates, and Deletes.
        /// </summary>
        /// <param name="t">The <see cref="Type"/> to track authorization methods for.</param>
        /// <param name="canInsertAction">A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be inserted through the Restier API.</param>
        /// <param name="canUpdateAction">A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be updated through the Restier API.</param>
        /// <param name="canDeleteAction">A <see cref="Func{Boolean}"/> that evaluates to a <see cref="bool"/> specifying whether or not a record can be deleted through the Restier API.</param>
        public AuthorizationEntry(Type t, Func<bool> canInsertAction, Func<bool> canUpdateAction, Func<bool> canDeleteAction) : this(t, canInsertAction, canUpdateAction)
        {
            CanDeleteAction = canDeleteAction;
        }
    }
}