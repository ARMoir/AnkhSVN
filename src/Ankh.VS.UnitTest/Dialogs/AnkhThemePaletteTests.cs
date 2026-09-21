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
using System.Drawing;

using Ankh.UI;
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.Dialogs
{
    [TestFixture]
    public class AnkhThemePaletteTests
    {
        [TestCase(30, 30, 30, true)]
        [TestCase(245, 245, 245, false)]
        [TestCase(127, 127, 127, true)]
        [TestCase(128, 128, 128, false)]
        [TestCase(0, 120, 215, true)]
        public void DarknessComesFromSurfaceLuminance(
            int red,
            int green,
            int blue,
            bool expected)
        {
            Assert.That(
                AnkhThemePalette.IsDark(Color.FromArgb(red, green, blue)),
                Is.EqualTo(expected));
        }

        [Test]
        public void BlendProducesNeutralColorFromActualThemePair()
        {
            Color result = AnkhThemePalette.Blend(
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(0, 0, 0),
                0.5);

            Assert.That(result, Is.EqualTo(Color.FromArgb(128, 128, 128)));
        }

        [TestCase(-0.01)]
        [TestCase(1.01)]
        public void BlendRejectsInvalidWeights(double weight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                delegate
                {
                    AnkhThemePalette.Blend(Color.White, Color.Black, weight);
                });
        }
    }
}
