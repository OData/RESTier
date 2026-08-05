// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.Restier.Core.Spatial
{
    /// <summary>
    /// Format/parse helpers for the SQL Server extended WKT dialect — bare WKT prefixed with <c>SRID=N;</c>.
    /// Microsoft.Spatial's <c>WellKnownTextSqlFormatter</c> reads/writes this dialect; storage APIs
    /// (<c>DbGeography.FromText</c>, NTS <c>WKTReader.Read</c>) speak the bare body and take the SRID separately.
    /// </summary>
    public static class SridPrefixHelpers
    {
        private const string Prefix = "SRID=";

        /// <summary>
        /// Returns <paramref name="bareWkt"/> prefixed with <c>SRID={srid};</c>.
        /// </summary>
        public static string FormatWithSridPrefix(int srid, string bareWkt)
        {
            if (bareWkt is null)
            {
                throw new ArgumentNullException(nameof(bareWkt));
            }

            return string.Concat(Prefix, srid.ToString(CultureInfo.InvariantCulture), ";", bareWkt);
        }

        /// <summary>
        /// Splits an SRID-prefixed WKT string into its (SRID, body) components.
        /// </summary>
        /// <param name="text">Either bare WKT or SRID-prefixed WKT.</param>
        /// <returns>
        /// (parsed SRID, body) when the input begins with <c>SRID=N;</c>;
        /// (null, original text) when the input has no prefix.
        /// </returns>
        /// <exception cref="FormatException">Thrown when the input begins with <c>SRID=</c> but is malformed.</exception>
        public static (int? srid, string body) ParseSridPrefix(string text)
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!text.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return (null, text);
            }

            var semicolon = text.IndexOf(';', Prefix.Length);
            if (semicolon < 0)
            {
                throw new FormatException(
                    "SRID prefix is malformed: missing ';' separator. Expected 'SRID=N;<body>'.");
            }

            var sridText = text.Substring(Prefix.Length, semicolon - Prefix.Length);
            if (sridText.Length == 0
                || !int.TryParse(sridText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var srid))
            {
                throw new FormatException(
                    "SRID prefix is malformed: SRID value is not a valid integer.");
            }

            var body = text.Substring(semicolon + 1);
            return (srid, body);
        }
    }
}
