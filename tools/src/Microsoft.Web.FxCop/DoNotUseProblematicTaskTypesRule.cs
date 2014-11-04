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

using System.Collections.Generic;
using Microsoft.FxCop.Sdk;

namespace Microsoft.Web.FxCop
{
    public class DoNotUseProblematicTaskTypesRule : IntrospectionRule
    {
        private readonly Dictionary<string, string> _problematicTypes = GetProblematicTypes();

        public DoNotUseProblematicTaskTypesRule()
            : base("DoNotUseProblematicTaskTypes")
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

        public override void VisitMemberBinding(MemberBinding memberBinding)
        {
            var method = memberBinding.BoundMember as Method;
            if (method != null)
            {
                string message;
                if (_problematicTypes.TryGetValue(method.DeclaringType.FullName, out message))
                {
                    Problems.Add(new Problem(GetResolution(method.DeclaringType.FullName, message), memberBinding.UniqueKey.ToString()));
                }
            }

            base.VisitMemberBinding(memberBinding);
        }

        private static Dictionary<string, string> GetProblematicTypes()
        {
            return new Dictionary<string, string>
            {
                { "System.Threading.Tasks.Parallel", "The methods on this type are blocking operations." },
                { "System.Threading.Tasks.TaskExtensions", "The .Unwrap() method does not have good performance characteristics. Use the .FastUnwrap() extension method instead." },
                { "System.Threading.Tasks.TaskFactory", "If you need to create a Task, use the TaskHelpers class instead." },
                { "System.Threading.Tasks.TaskScheduler", "If you need to create a Task, use the TaskHelpers class instead." }
            };
        }
    }
}
