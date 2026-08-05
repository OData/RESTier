// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;

namespace Microsoft.Restier.Core.Model
{
	/// <summary>
	/// Maps an unbound function-import name to the CLR type and source factory needed to dispatch
	/// a request for a keyless EF view (or other ComplexType-backed read-only collection).
	/// </summary>
	/// <remarks>
	/// Populated by <c>EFModelBuilder</c> during model construction inside the temporary
	/// model-building service provider used by <c>RestierODataOptionsExtensions.AddRestierRoute</c>.
	/// The populated instance is captured locally before that service provider is disposed and
	/// re-registered into the per-route services lambda, so request-time consumers
	/// (notably <c>RestierOperationExecutor</c>) resolve the same populated instance.
	/// </remarks>
	public sealed class KeylessViewRegistry
	{
		private readonly ConcurrentDictionary<string, KeylessViewEntry> entries
			= new(StringComparer.Ordinal);

		/// <summary>
		/// Registers a keyless view's dispatch metadata. Throws if <paramref name="functionImportName"/>
		/// has already been registered.
		/// </summary>
		/// <param name="functionImportName">The unbound function-import name as it appears in <c>$metadata</c>.</param>
		/// <param name="clrType">The CLR type of the view's element (registered as an EDM <c>ComplexType</c>).</param>
		/// <param name="sourceFactory">Builds an <see cref="IQueryable"/> over the underlying view, given the live API instance.</param>
		public void Register(string functionImportName, Type clrType, Func<object, IQueryable> sourceFactory)
		{
			Ensure.NotNullOrWhiteSpace(functionImportName, nameof(functionImportName));
			Ensure.NotNull(clrType, nameof(clrType));
			Ensure.NotNull(sourceFactory, nameof(sourceFactory));

			var entry = new KeylessViewEntry(functionImportName, clrType, sourceFactory);
			if (!entries.TryAdd(functionImportName, entry))
			{
				throw new InvalidOperationException(string.Format(
					CultureInfo.InvariantCulture,
					"A keyless view named '{0}' is already registered.",
					functionImportName));
			}
		}

		/// <summary>
		/// Attempts to find the dispatch metadata for an unbound function-import name.
		/// </summary>
		/// <param name="functionImportName">The unbound function-import name to look up.</param>
		/// <param name="entry">When this method returns, contains the matching entry, or <c>null</c> if not found.</param>
		/// <returns><c>true</c> if a matching entry was found; otherwise <c>false</c>.</returns>
		public bool TryGet(string functionImportName, out KeylessViewEntry entry)
		{
			if (string.IsNullOrEmpty(functionImportName))
			{
				entry = null;
				return false;
			}

			return entries.TryGetValue(functionImportName, out entry);
		}
	}
}
