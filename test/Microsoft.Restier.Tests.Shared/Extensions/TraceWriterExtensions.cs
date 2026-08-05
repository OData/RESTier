// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Restier.Tests.Shared.Extensions
{
    public static class TraceWriterExtensions
    {
        /// <summary>
        /// Attempts to unwrap the <see cref="HttpResponseMessage.Content"/> and log it to the <paramref name="traceListener"/> if possible.
        /// </summary>
        /// <param name="traceListener">The traceListener to use to write the output.</param>
        /// <param name="message">The message to write.</param>
        /// <param name="nullIsExpected">Specifies whether the <see cref="HttpResponseMessage.Content"/> in <paramref name="message"/> is expected to be null.</param>
        /// <remarks>
        /// This exists in order to safely allow the tests to continue in the absence of correct content. This is because the tests should log 
        /// the response content BEFORE failing the test for an incorrect <see cref="HttpResponseMessage.StatusCode"/>.
        /// </remarks>
        public static async Task<string> LogAndReturnMessageContentAsync(this TraceListener traceListener, HttpResponseMessage message, bool nullIsExpected = false)
        {
            Ensure.NotNull(traceListener, nameof(traceListener));
            Ensure.NotNull(message, nameof(message));

            if (message.Content != null)
            {
                var content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                traceListener.WriteLine(content);
                return content;
            }
            else
            {
                traceListener.WriteLine($"HttpRequestMessage.Content was null. This {(nullIsExpected ? "is" : "is not")} expected.");
                return string.Empty;
            }
        }
    }
}
