using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ankh.Services;
using Microsoft.VisualStudio.Shell.Interop;

namespace Ankh.Scc
{
    /// <summary>
    /// Identical to Microsoft.VisualStudio.Shell.Interop.__SccStatus 
    /// in Microsoft.VisualStudio.Shell.Interop.9.0
    /// </summary>
    enum SccStatus
    {
        SCC_STATUS_INVALID = -1,
        SCC_STATUS_NOTCONTROLLED = 0x0000,
        SCC_STATUS_CONTROLLED = 0x0001,
        SCC_STATUS_CHECKEDOUT = 0x0002,
        SCC_STATUS_OUTOTHER = 0x0004,
        SCC_STATUS_OUTEXCLUSIVE = 0x0008,
        SCC_STATUS_OUTMULTIPLE = 0x0010,
        SCC_STATUS_OUTOFDATE = 0x0020,
        SCC_STATUS_DELETED = 0x0040,
        SCC_STATUS_LOCKED = 0x0080,
        SCC_STATUS_MERGED = 0x0100,
        SCC_STATUS_SHARED = 0x0200,
        SCC_STATUS_PINNED = 0x0400,
        SCC_STATUS_MODIFIED = 0x0800,
        SCC_STATUS_OUTBYUSER = 0x1000,
        SCC_STATUS_NOMERGE = 0x2000,
        SCC_STATUS_RESERVED_1 = 0x4000,
        SCC_STATUS_RESERVED_2 = 0x8000
    }
    partial class SccProvider : IVsSccGlyphs
    {
        IStatusImageMapper _statusImages;
        protected IStatusImageMapper StatusImages
        {
            [DebuggerStepThrough]
            get { return _statusImages ?? (_statusImages = GetService<IStatusImageMapper>()); }
        }

        [CLSCompliant(false)]
        protected uint GlyphToStatus(AnkhGlyph glyph)
        {
            SccStatus status;
            switch (glyph)
            {
                case AnkhGlyph.None:
                case AnkhGlyph.Blank:
                case AnkhGlyph.Ignored:
                case AnkhGlyph.FileMissing:
                    // Not versioned
                    status = SccStatus.SCC_STATUS_NOTCONTROLLED;
                    break;
                case AnkhGlyph.Normal:
                case AnkhGlyph.LockedNormal:
                case AnkhGlyph.ChildChanged:
                    // Not changed / no real pending change by itself
                    // (Some tools keep track of checked out as a pending change)
                    status = SccStatus.SCC_STATUS_CONTROLLED;
                    break;
                case AnkhGlyph.MustLock:
                    // Not changed, but needs a lock before editting
                    status = SccStatus.SCC_STATUS_CONTROLLED | SccStatus.SCC_STATUS_LOCKED;
                    break;
                case AnkhGlyph.LockedModified:
                    // Modified under a lock
                    status = SccStatus.SCC_STATUS_CONTROLLED | SccStatus.SCC_STATUS_CHECKEDOUT
                        | SccStatus.SCC_STATUS_OUTBYUSER | SccStatus.SCC_STATUS_OUTEXCLUSIVE;
                    break;
                case AnkhGlyph.InConflict:
                    // Needs fixups after merging. Probably ignored
                    status = SccStatus.SCC_STATUS_CONTROLLED | SccStatus.SCC_STATUS_CHECKEDOUT
                        | SccStatus.SCC_STATUS_OUTBYUSER | SccStatus.SCC_STATUS_MERGED;
                    break;
                //case AnkhGlyph.Added:
                //case AnkhGlyph.ShouldBeAdded:
                //case AnkhGlyph.Deleted:
                //case AnkhGlyph.FileDirty:
                //case AnkhGlyph.CopiedOrMoved:
                default:
                    // Pending change + Checked out
                    status = SccStatus.SCC_STATUS_CONTROLLED | SccStatus.SCC_STATUS_CHECKEDOUT
                        | SccStatus.SCC_STATUS_OUTBYUSER;
                    break;
            }

            return (uint)status;
        }

        /// <summary>
        /// This function determines which glyph to display, given a combination of status flags.
        /// </summary>
        /// <param name="dwSccStatus">The dw SCC status.</param>
        /// <param name="psiGlyph">The psi glyph.</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public int GetSccGlyphFromStatus(uint dwSccStatus, VsStateIcon[] psiGlyph)
        {
            // This method is called when some user (e.g. like classview) wants to combine icons
            // (Unfortunately classview uses a hardcoded mapping)
            psiGlyph[0] = VsStateIcon.STATEICON_BLANK;

            return VSErr.S_OK;
        }

        /// <summary>
        /// This method is called by projects to discover the source control glyphs 
        /// to use on files and the files' source control status; this is the only way to get status.
        /// </summary>
        /// <param name="cFiles">The c files.</param>
        /// <param name="rgpszFullPaths">The RGPSZ full paths.</param>
        /// <param name="rgsiGlyphs">The rgsi glyphs.</param>
        /// <param name="rgdwSccStatus">The RGDW SCC status.</param>
        /// <returns></returns>
        [CLSCompliant(false)] // Implements 2 interfaces
        public int GetSccGlyph(int cFiles, string[] rgpszFullPaths, VsStateIcon[] rgsiGlyphs, uint[] rgdwSccStatus)
        {
            try
            {
                if (rgpszFullPaths == null || rgsiGlyphs == null)
                    return VSErr.E_POINTER; // Documented as impossible

                if (!IsActive)
                {
                    for (int i = 0; i < cFiles; i++)
                    {
                        if (rgsiGlyphs != null)
                            rgsiGlyphs[i] = VsStateIcon.STATEICON_NOSTATEICON;
                        if (rgdwSccStatus != null)
                            rgdwSccStatus[i] = (uint)SccStatus.SCC_STATUS_NOTCONTROLLED;
                    }
                    return VSErr.S_OK;
                }

                for (int i = 0; i < cFiles; i++)
                {
                    string file = rgpszFullPaths[i];
                    if (!IsSafeSccPath(file))
                    {
                        rgsiGlyphs[i] = VsStateIcon.STATEICON_BLANK;
                        if (rgdwSccStatus != null)
                            rgdwSccStatus[i] = (uint)SccStatus.SCC_STATUS_NOTCONTROLLED;
                        continue;
                    }

                    AnkhGlyph glyph = GetPathGlyph(file);

                    if (rgsiGlyphs != null)
                    {
                        VsStateIcon icon = (VsStateIcon)glyph;

                        if (icon == VsStateIcon.STATEICON_BLANK || icon == VsStateIcon.STATEICON_NOSTATEICON)
                            rgsiGlyphs[i] = icon;
                        else
                            rgsiGlyphs[i] = SccGlyphIndexLogic.GetStateIcon(glyph, _baseIndex);
                    }

                    if (rgdwSccStatus != null)
                    {
                        // This will make VS use the right texts on refactor, replace, etc.
                        rgdwSccStatus[i] = GlyphToStatus(glyph);
                    }
                }

                return VSErr.S_OK;
            }
            catch(Exception e)
            {
                return VSErr.GetHRForException(e);
            }
        }

        public abstract AnkhGlyph GetPathGlyph(string path);

        public void UpdateSolutionGlyph()
        {
            if (!IsActive)
                return;

            string sf = SolutionFilename;
            if (string.IsNullOrEmpty(sf))
                return;

            IVsHierarchy hier = GetService<IVsHierarchy>(typeof(SVsSolution));

            if (hier == null)
                return;

            int glyph = (int)SccGlyphIndexLogic.GetStateIcon(
                GetPathGlyph(sf),
                _baseIndex);

            hier.SetProperty(VSItemId.Root, (int)__VSHPROPID.VSHPROPID_StateIconIndex, glyph);
        }

        public void ClearSolutionGlyph()
        {
            IVsHierarchy hier = GetService<IVsHierarchy>(typeof(SVsSolution));

            if (hier == null)
                return;

            hier.SetProperty(VSItemId.Root, (int)__VSHPROPID.VSHPROPID_StateIconIndex, (int)AnkhGlyph.Blank);
        }

        uint _baseIndex = (uint)VsStateIcon.STATEICON_MAXINDEX;
        System.Windows.Forms.ImageList _glyphList;

        protected void DisposeGlyphList()
        {
            if (_glyphList != null)
                try
                {
                    _glyphList.Dispose();
                }
                finally
                {
                    _glyphList = null;
                }
        }

        // Legacy VS Wrapper for GetCustomGlyphList
        [CLSCompliant(false)]
        public int GetCustomGlyphList(uint baseIndex, out uint pdwImageListHandle)
        {
            IntPtr realHandle;

            int r = GetCustomGlyphList(baseIndex, out realHandle);
            pdwImageListHandle = unchecked((uint)realHandle);
            return r;
        }


        // pdwImageListHandle was int in VS < 2022
        [CLSCompliant(false)]
        public int GetCustomGlyphList(uint baseIndex, out IntPtr pdwImageListHandle)
        {
            if (baseIndex == _baseIndex && _glyphList != null)
            {
                pdwImageListHandle = _glyphList.Handle;

                return VSErr.S_OK;
            }

            if (_glyphList != null)
            {
                _glyphList.Dispose();
                _glyphList = null;
            }

            // Visual Studio supplies the first state-icon index reserved for
            // this provider's custom image list. Image 0 maps to baseIndex,
            // image 1 to baseIndex + 1, and so on. Use that contract directly
            // instead of the old VS11-era +16 overflow trick.
            if (StatusImages == null)
            {
                pdwImageListHandle = IntPtr.Zero;
                return VSErr.E_FAIL; // Vital service missing
            }

            _glyphList = StatusImages.CreateStatusImageList();
            _baseIndex = baseIndex;
            pdwImageListHandle = _glyphList.Handle;

            return VSErr.S_OK;
        }
    }
}

