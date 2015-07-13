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
        private const string OnlyWhiteSpacesRuleName = "LineMustNotContainOnlyWhiteSpaces";
        private const string LeadingTabsRuleName = "LineMustNotContainLeadingTabs";

        public override void AnalyzeDocument(CodeDocument document)
        {
            var csharpDocument = (CsDocument)document;
            if (csharpDocument.RootElement != null && !csharpDocument.RootElement.Generated)
            {
                CheckLineSpace(
                    csharpDocument,
                    IsRuleEnabled(csharpDocument, TrailingWhiteSpacesRuleName),
                    IsRuleEnabled(csharpDocument, OnlyWhiteSpacesRuleName),
                    IsRuleEnabled(csharpDocument, LeadingTabsRuleName));
            }
        }

        private void CheckLineSpace(
            CsDocument csharpDocument,
            bool checkTrailingWhiteSpaces,
            bool checkOnlyWhiteSpaces,
            bool checkLeadingTabs)
        {
            if (!checkTrailingWhiteSpaces && !checkOnlyWhiteSpaces && !checkLeadingTabs)
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

                    string trimmedLine = line.TrimEnd();
                    if (checkOnlyWhiteSpaces && trimmedLine.Length == 0 && line.Length > 0)
                    {
                        AddViolation(
                            csharpDocument.RootElement,
                            lineNumber,
                            OnlyWhiteSpacesRuleName);
                        continue;
                    }

                    if (checkTrailingWhiteSpaces
                        && trimmedLine.Length < line.Length
                        && !line.TrimStart().StartsWith("//"))
                    {
                        AddViolation(
                            csharpDocument.RootElement,
                            lineNumber,
                            TrailingWhiteSpacesRuleName);
                    }

                    if (checkLeadingTabs)
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
                                    LeadingTabsRuleName);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}