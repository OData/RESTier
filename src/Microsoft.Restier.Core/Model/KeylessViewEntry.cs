// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Restier.Core.Model
{
	/// <summary>
	/// A single entry in the <c>KeylessViewRegistry</c>. Carries enough information to
	/// dispatch a request for a keyless-view function import back to its underlying IQueryable
	/// source at request time.
	/// </summary>
	public sealed class KeylessViewEntry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="KeylessViewEntry"/> class.
		/// </summary>
		/// <param name="functionImportName">The unbound function-import name as it appears in $metadata.</param>
		/// <param name="clrType">The CLR type of the view's element (registered as an EDM ComplexType).</param>
		/// <param name="sourceFactory">Builds an <see cref="IQueryable"/> over the underlying view, given the live API instance.</param>
		public KeylessViewEntry(string functionImportName, Type clrType, Func<object, IQueryable> sourceFactory)
		{
			Ensure.NotNullOrWhiteSpace(functionImportName, nameof(functionImportName));
			Ensure.NotNull(clrType, nameof(clrType));
			Ensure.NotNull(sourceFactory, nameof(sourceFactory));

			FunctionImportName = functionImportName;
			ClrType = clrType;
			SourceFactory = sourceFactory;
		}

		/// <summary>
		/// Gets the unbound function-import name as it appears in <c>$metadata</c>.
		/// </summary>
		public string FunctionImportName { get; }

		/// <summary>
		/// Gets the CLR type of the view's element (registered as an EDM <c>ComplexType</c>).
		/// </summary>
		public Type ClrType { get; }

		/// <summary>
		/// Gets the factory that builds an <see cref="IQueryable"/> over the underlying view.
		/// </summary>
		/// <remarks>
		/// The argument is the live API instance (cast to <c>IEntityFrameworkApi</c> by EF-flavour factories).
		/// </remarks>
		public Func<object, IQueryable> SourceFactory { get; }
	}
}
