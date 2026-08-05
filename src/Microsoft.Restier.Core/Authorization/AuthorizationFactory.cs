// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Restier.Core.Authorization
{
    /// <summary>
    /// Maintains a Dictionary of <see cref="AuthorizationEntry">AuthorizationEntries</see> for eacy access by Restier's Authorization framework.
    /// </summary>
    public static class AuthorizationFactory
    {
        /// <summary>
        /// The backing collection that will store the <see cref="AuthorizationEntry">AuthorizationEntries</see>.
        /// </summary>
        private static readonly Dictionary<Type,AuthorizationEntry> _entries = new Dictionary<Type, AuthorizationEntry>();

        /// <summary>
        /// Returns an <see cref="AuthorizationEntry"/> for a given <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to find the AuthorizationEntry for.</typeparam>
        /// <returns>
        /// An <see cref="AuthorizationEntry"/> for a given <typeparamref name="T"/>. If the specific <typeparamref name="T"/> was not found
        /// in the internal dictionary, a new <see cref="AuthorizationEntry"/> for <typeparamref name="T"/> will be returned with the default
        /// (disallowed) permissions.
        /// </returns>
        /// <example>
        /// <code>
        /// protected internal bool CanInsertReadOnlyEntry() => AuthorizationFactory.ForType<ReadOnlyEntry>().CanInsertAction();
        /// protected internal bool CanUpdateReadOnlyEntry() => AuthorizationFactory.ForType<ReadOnlyEntry>().CanUpdateAction();
        /// protected internal bool CanDeleteReadOnlyEntry() => AuthorizationFactory.ForType<ReadOnlyEntry>().CanDeleteAction();
        /// 
        /// protected internal bool CanInsertCantUpdateEntry() => AuthorizationFactory.ForType<CantUpdateEntry>().CanInsertAction();
        /// protected internal bool CanUpdateCantUpdateEntry() => AuthorizationFactory.ForType<CantUpdateEntry>().CanUpdateAction();
        /// protected internal bool CanDeleteCantUpdateEntry() => AuthorizationFactory.ForType<CantUpdateEntry>().CanDeleteAction();
        /// 
        /// protected internal bool CanInsertAdminArchiveEntry() => AuthorizationFactory.ForType<AdminArchiveEntry>().CanInsertAction();
        /// protected internal bool CanUpdateAdminArchiveEntry() => AuthorizationFactory.ForType<AdminArchiveEntry>().CanUpdateAction();
        /// protected internal bool CanDeleteAdminArchiveEntry() => AuthorizationFactory.ForType<AdminArchiveEntry>().CanDeleteAction();
        /// </code>
        /// </example>
        public static AuthorizationEntry ForType<T>() where T : class
        {
            //RWM: Allocating the Type once for speed, see https://twitter.com/robertmclaws/status/1123573880780668928.
            var type = typeof(T);
            if (!_entries.ContainsKey(type))
            {
                Console.WriteLine($"RestierEssentials: The AuthorizationEntry for {type.Name} was not found. Adding new AuthorizationEntry with default permissions to speed up future lookups.");
                _entries[type] = new AuthorizationEntry(type);
            }
           return _entries[type];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entries"></param>
        /// <example>
        /// <code>
        /// bool trueAction() => true;
        /// bool adminAction() => ClaimsPrincipal.Current.IsInRole("admin"); //RWM: WARNING MAGIC STRINGS!

        /// var entries = new List<AuthorizationEntry>
        ///    {
        ///        // false, false. false
        ///        new AuthorizationEntry(typeof(ReadOnlyEntry)),
        ///        // true, false. false
        ///        new AuthorizationEntry(typeof(CantUpdateEntry), trueAction),
        ///        // true (admin), true (admin). false
        ///        new AuthorizationEntry(typeof(AdminArchiveEntry), adminAction, adminAction),
        ///    };
        /// AuthorizationFactory.RegisterEntries(entries);
        /// </code>
        /// </example>
        public static void RegisterEntries(List<AuthorizationEntry> entries) => entries?.ForEach(c => _entries[c.Type] = c);
    }
}