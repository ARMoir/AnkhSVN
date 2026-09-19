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

using System.Windows.Forms;

using Ankh.Commands;
using NUnit.Framework;

namespace AnkhSvn_UnitTestProject.Commands
{
    [TestFixture]
    public class AddToSccLogicTests
    {
        [TestCase(false, false, false, false, "Skip")]
        [TestCase(false, true, false, true, "Skip")]
        [TestCase(true, true, false, false, "ManageExisting")]
        [TestCase(true, false, true, true, "Skip")]
        [TestCase(true, false, false, true, "PromptAddOrCheckout")]
        [TestCase(true, false, false, false, "Checkout")]
        public void GetProjectAction_ClassifiesProjectState(
            bool isBindable,
            bool sameWorkingCopy,
            bool isVersioned,
            bool isVersionable,
            string expected)
        {
            Assert.That(
                AddToSccLogic.GetProjectAction(
                    isBindable,
                    sameWorkingCopy,
                    isVersioned,
                    isVersionable).ToString(),
                Is.EqualTo(expected));
        }

        [TestCase(DialogResult.Yes, "AddToExisting")]
        [TestCase(DialogResult.No, "Checkout")]
        [TestCase(DialogResult.Cancel, "Cancel")]
        [TestCase(DialogResult.OK, "None")]
        [TestCase(DialogResult.None, "None")]
        public void GetPromptAction_MapsDialogResults(
            DialogResult result,
            string expected)
        {
            Assert.That(
                AddToSccLogic.GetPromptAction(result).ToString(),
                Is.EqualTo(expected));
        }
    }
}
