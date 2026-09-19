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

namespace Ankh.Commands
{
    internal enum AddToSccProjectAction
    {
        Skip,
        ManageExisting,
        PromptAddOrCheckout,
        Checkout
    }

    internal enum AddToSccPromptAction
    {
        None,
        AddToExisting,
        Checkout,
        Cancel
    }

    internal static class AddToSccLogic
    {
        public static AddToSccProjectAction GetProjectAction(
            bool isSccBindable,
            bool sameWorkingCopy,
            bool isVersioned,
            bool isVersionable)
        {
            if (!isSccBindable)
                return AddToSccProjectAction.Skip;

            if (sameWorkingCopy)
                return AddToSccProjectAction.ManageExisting;

            if (isVersioned)
                return AddToSccProjectAction.Skip;

            return isVersionable
                ? AddToSccProjectAction.PromptAddOrCheckout
                : AddToSccProjectAction.Checkout;
        }

        public static AddToSccPromptAction GetPromptAction(DialogResult result)
        {
            switch (result)
            {
                case DialogResult.Yes:
                    return AddToSccPromptAction.AddToExisting;

                case DialogResult.No:
                    return AddToSccPromptAction.Checkout;

                case DialogResult.Cancel:
                    return AddToSccPromptAction.Cancel;

                default:
                    return AddToSccPromptAction.None;
            }
        }
    }
}
