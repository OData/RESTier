// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.


namespace Microsoft.Restier.Tests.Core
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    /// <summary>
    /// A trace listener that can be used to assert trace messages.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class TestTraceListener : TraceListener
    {
        private readonly StringBuilder stringBuilder = new StringBuilder();

        /// <summary>
        /// Gets the messages.
        /// </summary>
        public string Messages => stringBuilder.ToString();

        /// <inheritdoc />
        public override void Write(string message)
        {
            stringBuilder.Append(message);
        }

        /// <inheritdoc />
        public override void WriteLine(string message)
        {
            stringBuilder.AppendLine(message);
        }

        /// <summary>
        /// Clears the TraceListener.
        /// </summary>
        public void Clear()
        {
            stringBuilder.Clear();
        }
    }
}
