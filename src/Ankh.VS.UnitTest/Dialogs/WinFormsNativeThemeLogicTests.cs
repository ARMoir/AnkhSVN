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

using Ankh.WpfPackage.Services;
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.Dialogs
{
    [TestFixture]
    public class WinFormsNativeThemeLogicTests
    {
        [TestCase(true, false, true)]
        [TestCase(false, false, false)]
        [TestCase(true, true, false)]
        [TestCase(false, true, false)]
        public void DarkNativeThemeRequiresVsDarkThemeAndNoHighContrast(
            bool themeDark,
            bool highContrast,
            bool expected)
        {
            Assert.That(
                WinFormsNativeThemeLogic.ShouldUseDarkTheme(themeDark, highContrast),
                Is.EqualTo(expected));
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void DarkButtonsFollowDarkNativeTheme(bool useDarkNativeTheme, bool expected)
        {
            Assert.That(
                WinFormsNativeThemeLogic.ShouldUseDarkButtonRendering(useDarkNativeTheme),
                Is.EqualTo(expected));
        }

        [Test]
        public void NativeThemeClassNamesMatchSupportedWindowsDarkSubThemes()
        {
            Assert.That(WinFormsNativeThemeLogic.DarkExplorerTheme, Is.EqualTo("DarkMode_Explorer"));
            Assert.That(WinFormsNativeThemeLogic.DarkComboTheme, Is.EqualTo("DarkMode_CFD"));
            Assert.That(WinFormsNativeThemeLogic.DarkItemsViewTheme, Is.EqualTo("DarkMode_ItemsView"));
        }
    }
}
