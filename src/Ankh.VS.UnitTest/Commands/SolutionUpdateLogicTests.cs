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
using Ankh;
using Ankh.Commands;
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.Commands
{
    [TestFixture]
    public class SolutionUpdateLogicTests
    {
        [TestCase(AnkhCommand.SolutionUpdateLatest, UpdateCommandScope.Solution)]
        [TestCase(AnkhCommand.SolutionUpdateSpecific, UpdateCommandScope.Solution)]
        [TestCase(AnkhCommand.PendingChangesUpdateLatest, UpdateCommandScope.Solution)]
        [TestCase(AnkhCommand.FolderUpdateLatest, UpdateCommandScope.Folder)]
        [TestCase(AnkhCommand.FolderUpdateSpecific, UpdateCommandScope.Folder)]
        [TestCase(AnkhCommand.ProjectUpdateLatest, UpdateCommandScope.Project)]
        [TestCase(AnkhCommand.ProjectUpdateSpecific, UpdateCommandScope.Project)]
        public void GetScope_ClassifiesUpdateCommands(
            AnkhCommand command,
            UpdateCommandScope expected)
        {
            Assert.That(SolutionUpdateLogic.GetScope(command), Is.EqualTo(expected));
        }

        [TestCase(AnkhCommand.SolutionUpdateLatest, true)]
        [TestCase(AnkhCommand.ProjectUpdateLatest, true)]
        [TestCase(AnkhCommand.PendingChangesUpdateLatest, true)]
        [TestCase(AnkhCommand.FolderUpdateLatest, true)]
        [TestCase(AnkhCommand.SolutionUpdateSpecific, false)]
        [TestCase(AnkhCommand.ProjectUpdateSpecific, false)]
        [TestCase(AnkhCommand.FolderUpdateSpecific, false)]
        public void IsHeadCommand_ClassifiesLatestVsSpecific(
            AnkhCommand command,
            bool expected)
        {
            Assert.That(SolutionUpdateLogic.IsHeadCommand(command), Is.EqualTo(expected));
        }

        [Test]
        public void UsesImplicitHeadRevision_AlsoHonorsDontPrompt()
        {
            Assert.That(
                SolutionUpdateLogic.UsesImplicitHeadRevision(
                    AnkhCommand.ProjectUpdateSpecific,
                    true),
                Is.True);

            Assert.That(
                SolutionUpdateLogic.UsesImplicitHeadRevision(
                    AnkhCommand.ProjectUpdateSpecific,
                    false),
                Is.False);

            Assert.That(
                SolutionUpdateLogic.UsesImplicitHeadRevision(
                    AnkhCommand.ProjectUpdateLatest,
                    false),
                Is.True);
        }

        [Test]
        public void GetCommonAncestorUri_FindsSharedRepositoryDirectory()
        {
            Uri left = new Uri("https://example.invalid/svn/trunk/project-a/src");
            Uri right = new Uri("https://example.invalid/svn/trunk/project-b/tests");

            Uri common = SolutionUpdateLogic.GetCommonAncestorUri(left, right);

            Assert.That(
                common.AbsoluteUri,
                Is.EqualTo("https://example.invalid/svn/trunk/"));
        }

        [Test]
        public void GetCommonAncestorUri_PreservesDeeperSharedDirectory()
        {
            Uri left = new Uri("https://example.invalid/svn/trunk/shared/a");
            Uri right = new Uri("https://example.invalid/svn/trunk/shared/b");

            Uri common = SolutionUpdateLogic.GetCommonAncestorUri(left, right);

            Assert.That(
                common.AbsoluteUri,
                Is.EqualTo("https://example.invalid/svn/trunk/shared/"));
        }

        [Test]
        public void GetCommonAncestorUri_RejectsNullInputs()
        {
            Assert.Throws<ArgumentNullException>(
                () => SolutionUpdateLogic.GetCommonAncestorUri(
                    null,
                    new Uri("https://example.invalid/svn/")));

            Assert.Throws<ArgumentNullException>(
                () => SolutionUpdateLogic.GetCommonAncestorUri(
                    new Uri("https://example.invalid/svn/"),
                    null));
        }

        [Test]
        public void ShouldIncludeUpdateRoot_RejectsDuplicatesAndUnversionedItems()
        {
            Uri repo = new Uri("https://example.invalid/svn/");

            Assert.That(
                SolutionUpdateLogic.ShouldIncludeUpdateRoot(
                    true, true, true, repo, repo),
                Is.False);

            Assert.That(
                SolutionUpdateLogic.ShouldIncludeUpdateRoot(
                    false, false, true, repo, repo),
                Is.False);
        }

        [Test]
        public void ShouldIncludeUpdateRoot_SpecificRevisionRejectsAnotherRepository()
        {
            Uri selected = new Uri("https://example.invalid/svn-a/");
            Uri other = new Uri("https://example.invalid/svn-b/");

            Assert.That(
                SolutionUpdateLogic.ShouldIncludeUpdateRoot(
                    false, true, false, selected, other),
                Is.False);

            Assert.That(
                SolutionUpdateLogic.ShouldIncludeUpdateRoot(
                    false, true, false, selected, selected),
                Is.True);
        }

        [Test]
        public void ShouldIncludeUpdateRoot_HeadUpdateAllowsMultipleRepositories()
        {
            Assert.That(
                SolutionUpdateLogic.ShouldIncludeUpdateRoot(
                    false,
                    true,
                    true,
                    new Uri("https://example.invalid/svn-a/"),
                    new Uri("https://example.invalid/svn-b/")),
                Is.True);
        }

        [Test]
        public void ShouldIncludeUpdateRoot_AllowsUnknownRepositoryWhenOriginalCodeDid()
        {
            Assert.That(
                SolutionUpdateLogic.ShouldIncludeUpdateRoot(
                    false,
                    true,
                    false,
                    new Uri("https://example.invalid/svn-a/"),
                    null),
                Is.True);
        }
    }
}
