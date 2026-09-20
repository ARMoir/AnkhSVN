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

using Ankh.Scc;
using Microsoft.VisualStudio.Shell.Interop;
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.Scc
{
    [TestFixture]
    public class SccGlyphDisplayLogicTests
    {
        [Test]
        public void GetStateIcon_ShouldBeAddedUsesBuiltInVisibleIcon()
        {
            Assert.That(
                SccGlyphDisplayLogic.GetStateIcon(
                    AnkhGlyph.ShouldBeAdded,
                    16),
                Is.EqualTo(VsStateIcon.STATEICON_CHECKEDOUT));
        }

        [TestCase(AnkhGlyph.MustLock, 17)]
        [TestCase(AnkhGlyph.Modified, 18)]
        [TestCase(AnkhGlyph.Normal, 22)]
        [TestCase(AnkhGlyph.LockedNormal, 25)]
        [TestCase(AnkhGlyph.LockedModified, 26)]
        [TestCase(AnkhGlyph.Added, 28)]
        [TestCase(AnkhGlyph.InConflict, 30)]
        public void GetStateIcon_PreservesExistingMapping(
            AnkhGlyph glyph,
            int expected)
        {
            Assert.That(
                (int)SccGlyphDisplayLogic.GetStateIcon(glyph, 16),
                Is.EqualTo(expected));
        }

        [Test]
        public void GetStateIcon_LeavesBlankStatesUnshifted()
        {
            Assert.That(
                SccGlyphDisplayLogic.GetStateIcon(AnkhGlyph.None, 16),
                Is.EqualTo(VsStateIcon.STATEICON_NOSTATEICON));

            Assert.That(
                SccGlyphDisplayLogic.GetStateIcon(AnkhGlyph.Blank, 16),
                Is.EqualTo(VsStateIcon.STATEICON_BLANK));
        }
    }
}
