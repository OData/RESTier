// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using StyleCop;
using StyleCop.CSharp;

namespace Microsoft.OData.StyleCop
{
    [SourceAnalyzer(typeof(CsParser))]
    public class LineSpaceRule : SourceAnalyzer
    {
        private const string TrailingWhiteSpacesRuleName = "LineMustNotContainTrailingWhiteSpaces";
        private const string TabIndentationRuleName = "LineMustNotContainTabIndentation";

        public override void AnalyzeDocument(CodeDocument document)
        {
            var csharpDocument = (CsDocument)document;
            if (csharpDocument.RootElement != null && !csharpDocument.RootElement.Generated)
            {
                CheckLineSpace(
                    csharpDocument,
                    IsRuleEnabled(csharpDocument, TrailingWhiteSpacesRuleName),
                    IsRuleEnabled(csharpDocument, TabIndentationRuleName));
            }
        }

        private void CheckLineSpace(
            CsDocument csharpDocument,
            bool checkTrailingWhiteSpaces,
            bool checkTabIndentation)
        {
            if (!checkTrailingWhiteSpaces && !checkTabIndentation)
            {
                return;
            }

            using (var reader = csharpDocument.SourceCode.Read())
            {
                string line;
                var lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (line.Length == 0)
                    {
                        continue;
                    }

                    if (checkTrailingWhiteSpaces && char.IsWhiteSpace(line[line.Length - 1]))
                    {
                        AddViolation(
                            csharpDocument.RootElement,
                            lineNumber,
                            TrailingWhiteSpacesRuleName);
                    }

                    if (checkTabIndentation)
                    {
                        foreach (var c in line)
                        {
                            if (!char.IsWhiteSpace(c))
                            {
                                break;
                            }

                            if (c == '\t')
                            {
                                AddViolation(
                                    csharpDocument.RootElement,
                                    lineNumber,
                                    TabIndentationRuleName);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}