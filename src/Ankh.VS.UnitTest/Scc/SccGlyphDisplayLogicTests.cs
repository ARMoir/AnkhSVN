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
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.Scc
{
    [TestFixture]
    public class SccGlyphDisplayLogicTests
    {
        [Test]
        public void GetDisplayGlyph_UsesAddedImageForShouldBeAdded()
        {
            Assert.That(
                SccGlyphDisplayLogic.GetDisplayGlyph(
                    AnkhGlyph.ShouldBeAdded),
                Is.EqualTo(AnkhGlyph.Added));
        }

        [TestCase(AnkhGlyph.None)]
        [TestCase(AnkhGlyph.Blank)]
        [TestCase(AnkhGlyph.MustLock)]
        [TestCase(AnkhGlyph.Modified)]
        [TestCase(AnkhGlyph.Normal)]
        [TestCase(AnkhGlyph.Added)]
        [TestCase(AnkhGlyph.InConflict)]
        public void GetDisplayGlyph_PreservesOtherGlyphs(AnkhGlyph glyph)
        {
            Assert.That(
                SccGlyphDisplayLogic.GetDisplayGlyph(glyph),
                Is.EqualTo(glyph));
        }
    }
}
