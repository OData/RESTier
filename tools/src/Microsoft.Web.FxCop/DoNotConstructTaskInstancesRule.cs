// Domain Framework ver. 1.0
// Copyright (c) Microsoft Corporation
// All rights reserved.
// MIT License
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES
// OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Microsoft.FxCop.Sdk;

namespace Microsoft.Web.FxCop
{
    public class DoNotConstructTaskInstancesRule : IntrospectionRule
    {
        public DoNotConstructTaskInstancesRule()
            : base("DoNotConstructTaskInstances")
        {
        }

        public override ProblemCollection Check(Member member)
        {
            var method = member as Method;
            if (method != null)
            {
                VisitStatements(method.Body.Statements);
            }

            return Problems;
        }

        public override void VisitConstruct(Construct construct)
        {
            var memberBinding = construct.Constructor as MemberBinding;

            if (memberBinding != null
                && memberBinding.BoundMember.Name.Name == ".ctor"
                && memberBinding.BoundMember.DeclaringType.IsTask())
            {
                Problems.Add(new Problem(GetResolution(), construct.UniqueKey.ToString()));
            }

            base.VisitConstruct(construct);
        }
    }
}
