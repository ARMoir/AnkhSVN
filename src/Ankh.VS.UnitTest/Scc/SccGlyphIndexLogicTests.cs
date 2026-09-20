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
    public class SccGlyphIndexLogicTests
    {
        [Test]
        public void GetStateIcon_LeavesBuiltInBlankStatesUnshifted()
        {
            Assert.That(
                SccGlyphIndexLogic.GetStateIcon(AnkhGlyph.None, 12),
                Is.EqualTo(VsStateIcon.STATEICON_NOSTATEICON));

            Assert.That(
                SccGlyphIndexLogic.GetStateIcon(AnkhGlyph.Blank, 12),
                Is.EqualTo(VsStateIcon.STATEICON_BLANK));
        }

        [TestCase(12u, 25)]
        [TestCase(20u, 33)]
        [TestCase(32u, 45)]
        public void GetStateIcon_ShouldBeAddedUsesShellProvidedBaseIndex(
            uint baseIndex,
            int expectedIndex)
        {
            Assert.That(
                (int)SccGlyphIndexLogic.GetStateIcon(
                    AnkhGlyph.ShouldBeAdded,
                    baseIndex),
                Is.EqualTo(expectedIndex));
        }

        [TestCase(AnkhGlyph.MustLock, 13)]
        [TestCase(AnkhGlyph.Normal, 18)]
        [TestCase(AnkhGlyph.Added, 24)]
        [TestCase(AnkhGlyph.InConflict, 26)]
        [TestCase(AnkhGlyph.ChildChanged, 27)]
        public void GetStateIcon_MapsCustomImagePositionFromBaseIndex(
            AnkhGlyph glyph,
            int expectedIndex)
        {
            Assert.That(
                (int)SccGlyphIndexLogic.GetStateIcon(glyph, 12),
                Is.EqualTo(expectedIndex));
        }
    }
}
