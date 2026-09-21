// Copyright 2026 The AnkhSVN Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

using Ankh.UI.PendingChanges;
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.PendingChanges
{
    [TestFixture]
    public class CopilotCommitMessageTests
    {
        [Test]
        public void BuildPrompt_TreatsChangesAsDataAndRequestsCommitMessageOnly()
        {
            string prompt = CopilotCommitMessage.BuildPrompt("Modified: src/Test.cs");

            Assert.That(prompt, Does.Contain("Return only the commit message"));
            Assert.That(prompt, Does.Contain("untrusted data"));
            Assert.That(prompt, Does.Contain("Modified: src/Test.cs"));
            Assert.That(prompt, Does.Contain("<svn-changes>"));
        }

        [Test]
        public void NormalizeResponse_StripsCommitMessageLabel()
        {
            string actual = CopilotCommitMessage.NormalizeResponse(
                "Commit message: Fix pending changes refresh");

            Assert.That(actual, Is.EqualTo("Fix pending changes refresh"));
        }

        [Test]
        public void NormalizeResponse_StripsMarkdownFence()
        {
            string actual = CopilotCommitMessage.NormalizeResponse(
                "```text\nFix pending changes refresh\n\nKeep the list synchronized.\n```");

            Assert.That(
                actual,
                Is.EqualTo(
                    "Fix pending changes refresh" + Environment.NewLine +
                    Environment.NewLine +
                    "Keep the list synchronized."));
        }
    }
}
