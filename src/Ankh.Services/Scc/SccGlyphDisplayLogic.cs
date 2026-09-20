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

using Microsoft.VisualStudio.Shell.Interop;

namespace Ankh.Scc
{
    internal static class SccGlyphDisplayLogic
    {
        public static VsStateIcon GetStateIcon(AnkhGlyph glyph, int glyphOffset)
        {
            // Issue #56 diagnostic: bypass all custom image-list handling for
            // ShouldBeAdded and use a Visual Studio built-in icon that is known
            // to render for modified files.
            if (glyph == AnkhGlyph.ShouldBeAdded)
                return VsStateIcon.STATEICON_CHECKEDOUT;

            VsStateIcon icon = (VsStateIcon)glyph;

            if (icon == VsStateIcon.STATEICON_BLANK
                || icon == VsStateIcon.STATEICON_NOSTATEICON)
            {
                return icon;
            }

            return (VsStateIcon)((int)icon + glyphOffset);
        }
    }
}
