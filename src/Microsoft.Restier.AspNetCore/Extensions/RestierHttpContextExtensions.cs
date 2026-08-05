// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Restier.AspNetCore.Batch;

namespace Microsoft.Restier.AspNetCore
{

    /// <summary>
    /// Offers a collection of extension methods to <see cref="HttpContext"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class RestierHttpContextExtensions
    {

        private const string ChangeSetKey = "Microsoft.Restier.Submit.ChangeSet";

        /// <summary>
        /// Sets the <see cref="RestierChangeSetProperty"/> to the <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="changeSetProperty">The change set to be set.</param>
        public static void SetChangeSet(this HttpContext context, RestierChangeSetProperty changeSetProperty)
        {
            Ensure.NotNull(context, nameof(context));
            context.Items.Add(ChangeSetKey, changeSetProperty);
        }

        /// <summary>
        /// Gets the <see cref="RestierChangeSetProperty"/> from the <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>The <see cref="RestierChangeSetProperty"/>.</returns>
        public static RestierChangeSetProperty GetChangeSet(this HttpContext context)
        {
            Ensure.NotNull(context, nameof(context));

            if (context.Items.TryGetValue(ChangeSetKey, out var value))
            {
                return value as RestierChangeSetProperty;
            }

            return null;
        }

    }

}
