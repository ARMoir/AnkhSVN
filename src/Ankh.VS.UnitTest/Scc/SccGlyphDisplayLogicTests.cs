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
        [TestCase(AnkhGlyph.Added, 12)]
        [TestCase(AnkhGlyph.ShouldBeAdded, 13)]
        [TestCase(AnkhGlyph.InConflict, 14)]
        [TestCase(AnkhGlyph.ChildChanged, 15)]
        public void GetStateIcon_UsesNativeCustomSlots(
            AnkhGlyph glyph,
            int expected)
        {
            Assert.That(
                (int)SccGlyphDisplayLogic.GetStateIcon(glyph, 16),
                Is.EqualTo(expected));
        }

        [TestCase(AnkhGlyph.MustLock, 17)]
        [TestCase(AnkhGlyph.Modified, 18)]
        [TestCase(AnkhGlyph.Deleted, 19)]
        [TestCase(AnkhGlyph.FileDirty, 20)]
        [TestCase(AnkhGlyph.Normal, 22)]
        [TestCase(AnkhGlyph.LockedNormal, 25)]
        [TestCase(AnkhGlyph.LockedModified, 26)]
        public void GetStateIcon_PreservesWorkingExtendedGlyphPath(
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

        [Test]
        public void GetStateIcon_HonorsOffsetForStandardGlyphs()
        {
            Assert.That(
                (int)SccGlyphDisplayLogic.GetStateIcon(
                    AnkhGlyph.Modified,
                    24),
                Is.EqualTo(26));
        }
    }
}
