using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

using Ankh.UI;
using Ankh.VS;
using Ankh.ExtensionPoints.UI;
using Ankh.Services;
using Ankh.Commands;

namespace Ankh.WpfPackage.Services
{
    [GlobalService(typeof(IWinFormsThemingService), MinVersion = VSInstance.VS2012)]
    sealed partial class ThemingService : AnkhService, IWinFormsThemingService
    {
        readonly ConditionalWeakTable<ComboBox, DarkComboBoxPainter> _comboPainters =
            new ConditionalWeakTable<ComboBox, DarkComboBoxPainter>();
        readonly ConditionalWeakTable<NumericUpDown, DarkNumericUpDownPainter> _numericPainters =
            new ConditionalWeakTable<NumericUpDown, DarkNumericUpDownPainter>();

        public ThemingService(IAnkhServiceProvider context)
            : base(context)
        {

        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }


        public Color GetThemedColorValue(ref Guid colorCategory, string colorName, bool foreground)
        {
            Type vsUIShell5 = typeof(IVsUIShell5);

            if (vsUIShell5 == null)
                throw new InvalidOperationException();

            IVsUIShell5 vs5 = GetService<IVsUIShell5>(typeof(SVsUIShell));
            MethodInfo method = vsUIShell5.GetMethod("GetThemedColor");
            
            uint clr = vs5.GetThemedColor(ref colorCategory, colorName, foreground ? (uint)1 : 0);
            // TODO: Use bitshifting

            byte[] colorComponents = BitConverter.GetBytes(clr);
            return System.Drawing.Color.FromArgb(colorComponents[3], colorComponents[0], colorComponents[1], colorComponents[2]);
        }

        bool VSThemeWindow(IntPtr hwnd, bool forDialog)
        {
            var ui6 = GetService<IVsUIShell6>(typeof(SVsUIShell));


            if (ui6 != null)
            {
                bool r = ui6.ThemeWindow(hwnd);
                if (r && forDialog)
                    ui6.SetFixedThemeColors(hwnd);
                return r;
            }
            else
                return false;
        }

        delegate IVsUIObject GetIconForFile(string filename, __VSUIDATAFORMAT desiredFormat);
        GetIconForFile _giff;

        delegate IVsUIObject GetIconForFileEx(string filename, __VSUIDATAFORMAT desiredFormat, out uint iconSource);
        GetIconForFileEx _giffEx;

        [Obsolete]
        public bool TryGetIcon(string path, out IntPtr hIcon)
        {
            hIcon = IntPtr.Zero;

            if (_giff == null)
            {
                var imgs = GetService<IVsImageService2>(typeof(SVsImageService));

                if (imgs != null)
                {
                    _giff = imgs.GetIconForFile;
                    _giffEx = imgs.GetIconForFileEx;
                }
            }

            try
            {
                IVsUIObject uiOb;
                uint src = 0;

                if (_giffEx != null)
                    uiOb = _giffEx(path, __VSUIDATAFORMAT.VSDF_WIN32, out src);
                else
                    uiOb = _giff(path, __VSUIDATAFORMAT.VSDF_WIN32);

                if (src == 2 || uiOb == null)
                    return false; // Just use the os directly. (Allows caching)

                object data;
                if (!VSErr.Succeeded(uiOb.get_Data(out data)))
                    return false;

                IVsUIWin32Icon vsIcon = data as IVsUIWin32Icon;

                if (vsIcon == null)
                    return false;

                if (!VSErr.Succeeded(vsIcon.GetHICON(out var iconHandle)))
                    return false;

                hIcon = (IntPtr)iconHandle;

                return (hIcon != IntPtr.Zero);
            }
            catch { }

            return false;
        }

        public void ThemeRecursive(System.Windows.Forms.Control control, bool forDialog)
        {
            bool recurse = true;
            bool autoTheme = true;
            ISupportsVSTheming themeControl = control as ISupportsVSTheming;
            if (themeControl != null)
            {
                CancelEventArgs ca = new CancelEventArgs(false);
                if (themeControl != null)
                    themeControl.OnThemeChange(this, ca);

                if (ca.Cancel)
                    recurse = autoTheme = false; // No recurse!
            }

            IThemedControl themed = control as IThemedControl;
            if (themed != null)
            {
                ApplyThemeEventArgs atea = new ApplyThemeEventArgs(this, forDialog);

                themed.OnApplyTheme(atea);

                recurse = !atea.NoRecurse;
                autoTheme = !atea.DontTheme;
                forDialog = atea.ForDialog;
            }

            if (autoTheme && control.IsHandleCreated)
            {
                VSThemeWindow(control, forDialog);
            }

            if (recurse)
                foreach (Control c in control.Controls)
                {
                    ThemeRecursive(c, forDialog);
                }
        }

        bool MaybeTheme<T>(Action<T> how, Control control, bool forDialog) where T : class
        {
            T value = control as T;
            if (value != null)
            {
                how(value);
                return true;
            }
            return false;
        }

        bool MaybeTheme<T>(Action<T, bool> how, Control control, bool forDialog) where T : class
        {
            T value = control as T;
            if (value != null)
            {
                how(value, forDialog);
                return true;
            }
            return false;
        }

        bool UseDarkNativeTheme
        {
            get
            {
                IAnkhCommandStates states = GetService<IAnkhCommandStates>();

                return WinFormsNativeThemeLogic.ShouldUseDarkTheme(
                    states != null && states.ThemeDark,
                    SystemInformation.HighContrast);
            }
        }

        void ApplyNativeControlTheme(IntPtr handle, string darkTheme, bool forDialog)
        {
            if (handle == IntPtr.Zero)
                return;

            if (UseDarkNativeTheme)
            {
                VSThemeWindow(handle, forDialog);
                NativeMethods.SetWindowTheme(handle, darkTheme, null);
            }
            else
            {
                // Remove a dark native sub-theme after a VS theme switch, then
                // let the shell apply its current light/blue/high-contrast theme.
                NativeMethods.SetWindowTheme(handle, null, null);
                VSThemeWindow(handle, forDialog);
            }
        }

        void ApplyNativeCaptionTheme(Form form)
        {
            if (form == null || !form.IsHandleCreated)
                return;

            int enabled = UseDarkNativeTheme ? 1 : 0;

            try
            {
                int hr = NativeMethods.DwmSetWindowAttribute(
                    form.Handle,
                    NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref enabled,
                    Marshal.SizeOf(typeof(int)));

                if (hr != 0)
                {
                    NativeMethods.DwmSetWindowAttribute(
                        form.Handle,
                        NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1,
                        ref enabled,
                        Marshal.SizeOf(typeof(int)));
                }
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        IAnkhVSColor _colorSvc;
        public IAnkhVSColor ColorSvc
        {
            get { return _colorSvc ?? (_colorSvc = GetService<IAnkhVSColor>()); }
        }

        IUIService _uiService;
        public IUIService UIService
        {
            get { return _uiService ?? (_uiService = GetService<IUIService>()); }
        }

        Font _dialogFont;
        public Font DialogFont
        {
            get { return _dialogFont ?? (_dialogFont = (Font)UIService.Styles["DialogFont"]); }
        }

        void ThemeOne(Label label)
        {
            if (label.Font != DialogFont)
                label.Font = DialogFont;

            if (label.BackColor != label.Parent.BackColor)
                label.BackColor = label.Parent.BackColor;

            if (label.ForeColor != label.Parent.ForeColor)
                label.ForeColor = label.Parent.ForeColor;

            LinkLabel ll = label as LinkLabel;
            if (ll != null)
            {
                Color clrLink;

                if (VSColors.TryGetColor((__VSSYSCOLOREX)__VSSYSCOLOREX3.VSCOLOR_STARTPAGE_TEXT_CONTROL_LINK_SELECTED, out clrLink))
                    ll.LinkColor = clrLink;
            }

            if (label.BorderStyle == BorderStyle.Fixed3D)
                label.BorderStyle = BorderStyle.FixedSingle;
        }

        void ThemeOne(TextBox textBox)
        {
            if (textBox.Font != DialogFont)
                textBox.Font = DialogFont;

            Color backColor;
            if (!textBox.ReadOnly
                || !ColorSvc.TryGetColor((__VSSYSCOLOREX)__VSSYSCOLOREX3.VSCOLOR_COMBOBOX_BACKGROUND, out backColor))
            {
                backColor = textBox.Parent.BackColor;
            }

            if (textBox.BackColor != backColor)
                textBox.BackColor = backColor;

            if (textBox.ForeColor != textBox.Parent.ForeColor)
                textBox.ForeColor = textBox.Parent.ForeColor;

            if (textBox.BorderStyle == BorderStyle.Fixed3D)
                textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        void ThemeOne(ListView listView, bool forDialog)
        {
            ApplyNativeControlTheme(
                listView.Handle,
                WinFormsNativeThemeLogic.DarkExplorerTheme,
                forDialog);

            if (listView.Font != DialogFont)
                listView.Font = DialogFont;

            Color oldBack = listView.BackColor;
            Color oldFore = listView.ForeColor;
            Color newBack = listView.Parent.BackColor;
            Color newFore = listView.Parent.ForeColor;
            bool updateBack= false, updateFore = false;

            if (oldBack != newBack)
            {
                listView.BackColor = newBack;
                updateBack = true;
            }

            if (oldFore != newFore)
            {
                listView.ForeColor = newFore;
                updateFore = true;
            }

            // In some cases we can iterate over third party components here,
            // so make sure we don't fail because we try to iterate a virtual
            // listview
            if ((updateBack || updateFore) && !listView.VirtualMode)
            {
                foreach(ListViewItem lvi in listView.Items)
                {
                    if (updateFore && lvi.ForeColor == oldFore)
                        lvi.ForeColor = newFore;

                    if (updateBack && lvi.BackColor == oldBack)
                        lvi.BackColor = newBack;
                }
            }


            if (listView.BorderStyle == BorderStyle.Fixed3D)
                listView.BorderStyle = BorderStyle.FixedSingle;

            IntPtr header = NativeMethods.SendMessage(listView.Handle, NativeMethods.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);

            if (header != IntPtr.Zero)
            {
                ApplyNativeControlTheme(
                    header,
                    WinFormsNativeThemeLogic.DarkItemsViewTheme,
                    forDialog);
            }
        }

        void ThemeOne(TreeView treeView, bool forDialog)
        {
            ApplyNativeControlTheme(
                treeView.Handle,
                WinFormsNativeThemeLogic.DarkExplorerTheme,
                forDialog);

            if (treeView.Font != DialogFont)
                treeView.Font = DialogFont;

            if (treeView.BackColor != treeView.Parent.BackColor)
                treeView.BackColor = treeView.Parent.BackColor;

            if (treeView.ForeColor != treeView.Parent.ForeColor)
                treeView.ForeColor = treeView.Parent.ForeColor;

            if (treeView.BorderStyle == BorderStyle.Fixed3D)
                treeView.BorderStyle = BorderStyle.FixedSingle;
        }

        void ThemeOne(UserControl userControl)
        {
            if (userControl.Parent != null && userControl.Font != userControl.Parent.Font)
                userControl.Font = userControl.Parent.Font;

            Color color;
            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND, out color))
            {
                if (userControl.BackColor != color)
                    userControl.BackColor = color;
            }

            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT, out color))
            {
                if (userControl.ForeColor != color)
                    userControl.ForeColor = color;
            }

            if (userControl.BorderStyle == BorderStyle.Fixed3D)
                userControl.BorderStyle = BorderStyle.FixedSingle;
        }

        private void ThemeOne(ContainerControl container)
        {
            if (container.Parent != null)
            {
            }
            else
                ThemeForm(container);
        }

        private void ThemeForm(ContainerControl form)
        {
            if (form.Parent != null && form.Font != form.Parent.Font)
                form.Font = form.Parent.Font;

            Color color;
            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND, out color))
            {
                if (form.BackColor != color)
                    form.BackColor = color;
            }

            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT, out color))
            {
                if (form.ForeColor != color)
                    form.ForeColor = color;
            }

            ApplyNativeCaptionTheme(form as Form);
        }

        private void ThemeOne(ScrollableControl one)
        {

        }


        void ThemeOne(Panel panel)
        {
            if (panel.Parent != null && panel.Font != panel.Parent.Font)
                panel.Font = panel.Parent.Font;

            Color color;
            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND, out color))
            {
                if (panel.BackColor != color)
                    panel.BackColor = color;
            }

            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT, out color))
            {
                if (panel.ForeColor != color)
                    panel.ForeColor = color;
            }

            if (panel.BorderStyle == BorderStyle.Fixed3D)
                panel.BorderStyle = BorderStyle.FixedSingle;
        }

        void ThemeOne(ToolStrip toolBar)
        {
            if (toolBar.Font != toolBar.Parent.Font)
                toolBar.Font = toolBar.Parent.Font;

            ToolStripRenderer renderer = UIService.Styles["VsRenderer"] as ToolStripRenderer;

            if (renderer != null)
                toolBar.Renderer = renderer;
        }

        private void ThemeOne(Button button, bool forDialog)
        {
            if (button.Parent != null && button.Font != button.Parent.Font)
                button.Font = button.Parent.Font;

            bool darkButton = WinFormsNativeThemeLogic.ShouldUseDarkButtonRendering(
                UseDarkNativeTheme);

            if (darkButton && button.Parent != null)
            {
                // Native WinForms button painting can retain a light face even
                // when IVsUIShell6.ThemeWindow reports success. In VS dark mode
                // use explicit VS-derived colors so enabled and disabled buttons
                // remain consistent with the dialog surface.
                button.UseVisualStyleBackColor = false;
                button.FlatStyle = FlatStyle.Flat;

                Color backColor = ControlPaint.Light(button.Parent.BackColor, 0.05f);
                Color borderColor = ControlPaint.Light(button.Parent.BackColor, 0.22f);
                Form owner = button.FindForm();

                if (owner != null
                    && ReferenceEquals(owner.AcceptButton, button)
                    && button.Enabled)
                {
                    borderColor = SystemColors.Highlight;
                }

                if (button.BackColor != backColor)
                    button.BackColor = backColor;

                if (button.ForeColor != button.Parent.ForeColor)
                    button.ForeColor = button.Parent.ForeColor;

                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = borderColor;
                button.FlatAppearance.MouseOverBackColor =
                    ControlPaint.Light(button.Parent.BackColor, 0.10f);
                button.FlatAppearance.MouseDownBackColor =
                    ControlPaint.Light(button.Parent.BackColor, 0.16f);
            }
            else
            {
                button.FlatStyle = FlatStyle.Standard;
                button.UseVisualStyleBackColor = true;

                if (button.IsHandleCreated)
                    VSThemeWindow(button.Handle, forDialog);
            }
        }

        const __VSSYSCOLOREX VSCOLOR_BRANDEDUI_TITLE = (__VSSYSCOLOREX)__VSSYSCOLOREX2.VSCOLOR_BRANDEDUI_TITLE;
        const __VSSYSCOLOREX VSCOLOR_BRANDEDUI_BORDER = (__VSSYSCOLOREX)__VSSYSCOLOREX2.VSCOLOR_BRANDEDUI_BORDER;
        const __VSSYSCOLOREX VSCOLOR_BRANDEDUI_TEXT = (__VSSYSCOLOREX)__VSSYSCOLOREX2.VSCOLOR_BRANDEDUI_TEXT;
        const __VSSYSCOLOREX VSCOLOR_BRANDEDUI_BACKGROUND = (__VSSYSCOLOREX)__VSSYSCOLOREX2.VSCOLOR_BRANDEDUI_BACKGROUND;
        const __VSSYSCOLOREX VSCOLOR_BRANDEDUI_FILL = (__VSSYSCOLOREX)__VSSYSCOLOREX2.VSCOLOR_BRANDEDUI_FILL;
        const __VSSYSCOLOREX VSCOLOR_GRAYTEXT = (__VSSYSCOLOREX)__VSSYSCOLOREX3.VSCOLOR_GRAYTEXT;
        const __VSSYSCOLOREX VSCOLOR_COMMANDBAR_TOOLBAR_SEPARATOR = (__VSSYSCOLOREX)__VSSYSCOLOREX3.VSCOLOR_COMMANDBAR_TOOLBAR_SEPARATOR;
        const __VSSYSCOLOREX VSCOLOR_THREEDFACE = (__VSSYSCOLOREX)__VSSYSCOLOREX3.VSCOLOR_THREEDFACE;

        IAnkhVSColor _vsColors;
        IAnkhVSColor VSColors
        {
            get { return _vsColors ?? (_vsColors = GetService<IAnkhVSColor>()); }
        }

        void ThemeOne(PropertyGrid grid)
        {
            Color clrTitle, clrBorder, clrText, clrBackground, clrFill, clrGrayText;

            if (!VSColors.TryGetColor(VSCOLOR_BRANDEDUI_TITLE, out clrTitle))
                clrTitle = SystemColors.WindowText;
            if (!VSColors.TryGetColor(VSCOLOR_BRANDEDUI_BORDER, out clrBorder))
                clrBorder = SystemColors.WindowFrame;
            if (!VSColors.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT, out clrText))
                clrText = SystemColors.WindowText;
            if (!VSColors.TryGetColor(VSCOLOR_BRANDEDUI_BACKGROUND, out clrBackground))
                clrBackground = SystemColors.InactiveBorder;
            if (!VSColors.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND, out clrFill))
                clrFill = SystemColors.Control;
            if (!VSColors.TryGetColor(VSCOLOR_GRAYTEXT, out clrGrayText))
                clrGrayText = SystemColors.WindowText;

            grid.BackColor = clrFill;

            grid.HelpBackColor = clrFill;
            grid.ViewBackColor = clrFill;

            grid.ViewForeColor = clrText;
            grid.HelpForeColor = clrText;
            grid.LineColor = clrBackground;
            grid.CategoryForeColor = clrTitle;

            if (VSVersion.VS2012OrLater)
            {
                // New in 4.5 properties. Properly added for VS2012.
                SetProperty(grid, "HelpBorderColor", clrFill);
                SetProperty(grid, "ViewBorderColor", clrFill);
                SetProperty(grid, "DisabledItemForeColor", clrGrayText);
                SetProperty(grid, "CategorySplitterColor", clrBackground);

                // The OS glyphs don't work in the dark theme. VS uses the same trick. (Unavailable in 4.0)
                SetProperty(grid, "CanShowVisualStyleGlyphs", false);
            }
        }

        void ThemeOne(ComboBox combo, bool forDialog)
        {
            ApplyNativeControlTheme(
                combo.Handle,
                WinFormsNativeThemeLogic.DarkComboTheme,
                forDialog);

            if (combo.Font != DialogFont)
                combo.Font = DialogFont;

            if (combo.BackColor != combo.Parent.BackColor)
                combo.BackColor = combo.Parent.BackColor;

            if (combo.ForeColor != combo.Parent.ForeColor)
                combo.ForeColor = combo.Parent.ForeColor;

            combo.DrawItem -= ThemeComboDrawItem;

            if (UseDarkNativeTheme)
            {
                combo.FlatStyle = FlatStyle.Flat;
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem += ThemeComboDrawItem;
            }
            else
            {
                combo.DrawMode = DrawMode.Normal;
                combo.FlatStyle = FlatStyle.Standard;
            }

            DarkComboBoxPainter painter = _comboPainters.GetValue(
                combo,
                delegate(ComboBox value) { return new DarkComboBoxPainter(value); });
            painter.SetDarkMode(UseDarkNativeTheme);
        }

        void ThemeComboDrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null)
                return;

            bool editPortion = (e.State & DrawItemState.ComboBoxEdit) != 0;
            bool selected = (e.State & DrawItemState.Selected) != 0 && !editPortion;

            Color backColor = selected ? SystemColors.Highlight : combo.BackColor;
            Color foreColor = selected ? SystemColors.HighlightText : combo.ForeColor;

            using (SolidBrush background = new SolidBrush(backColor))
                e.Graphics.FillRectangle(background, e.Bounds);

            string text = e.Index >= 0 && e.Index < combo.Items.Count
                ? combo.GetItemText(combo.Items[e.Index])
                : combo.Text;

            Rectangle textBounds = new Rectangle(
                e.Bounds.Left + 3,
                e.Bounds.Top,
                Math.Max(0, e.Bounds.Width - 6),
                e.Bounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                combo.Font,
                textBounds,
                foreColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix |
                TextFormatFlags.SingleLine);

            if ((e.State & DrawItemState.Focus) != 0 && !editPortion)
                e.DrawFocusRectangle();
        }

        void ThemeOne(NumericUpDown numeric, bool forDialog)
        {
            ApplyNativeControlTheme(
                numeric.Handle,
                WinFormsNativeThemeLogic.DarkComboTheme,
                forDialog);

            if (numeric.Font != DialogFont)
                numeric.Font = DialogFont;

            if (numeric.Parent != null)
            {
                if (numeric.BackColor != numeric.Parent.BackColor)
                    numeric.BackColor = numeric.Parent.BackColor;

                if (numeric.ForeColor != numeric.Parent.ForeColor)
                    numeric.ForeColor = numeric.Parent.ForeColor;
            }

            if (numeric.BorderStyle == BorderStyle.Fixed3D)
                numeric.BorderStyle = BorderStyle.FixedSingle;

            foreach (Control child in numeric.Controls)
            {
                child.BackColor = numeric.BackColor;
                child.ForeColor = numeric.ForeColor;

                if (child.IsHandleCreated)
                {
                    ApplyNativeControlTheme(
                        child.Handle,
                        WinFormsNativeThemeLogic.DarkComboTheme,
                        forDialog);
                }
            }

            DarkNumericUpDownPainter painter = _numericPainters.GetValue(
                numeric,
                delegate(NumericUpDown value) { return new DarkNumericUpDownPainter(value); });
            painter.SetDarkMode(UseDarkNativeTheme);
        }

        void ThemeOne(SplitContainer panel)
        {
            IHasSplitterColor ex = panel as IHasSplitterColor;
            if (ex != null)
                ThemeOne(ex);

            if (panel.Parent != null && panel.Font != panel.Parent.Font)
                panel.Font = panel.Parent.Font;

            Color color;
            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_BACKGROUND, out color))
            {
                if (panel.BackColor != color)
                {
                    panel.BackColor = color;
                    panel.Panel1.BackColor = color;
                    panel.Panel2.BackColor = color;
                }
            }

            if (ColorSvc.TryGetColor(__VSSYSCOLOREX.VSCOLOR_TOOLWINDOW_TEXT, out color))
            {
                if (panel.ForeColor != color)
                {
                    panel.ForeColor = color;
                    panel.Panel1.ForeColor = color;
                    panel.Panel2.ForeColor = color;
                }
            }

            if (panel.BorderStyle == BorderStyle.Fixed3D)
                panel.BorderStyle = BorderStyle.FixedSingle;
        }

        void ThemeOne(IHasSplitterColor splitter)
        {
            Color clrSplitter;

            if (!VSColors.TryGetColor(VSCOLOR_THREEDFACE, out clrSplitter))
                clrSplitter = SystemColors.InactiveBorder;

            splitter.SplitterColor = clrSplitter;
        }

        private void SetProperty(PropertyGrid grid, string propertyName, object value)
        {
            PropertyInfo pi = typeof(PropertyGrid).GetProperty(propertyName);

            Debug.Assert(pi != null, "Grid Property exists");
            if (pi != null)
                pi.SetValue(grid, value, null);
        }

        static class NativeMethods
        {
            public const Int32 LVM_GETHEADER = 0x1000 + 31; // LVM_FIRST + 31;
            public const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
            public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

            [DllImport("user32.dll")]
            public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
            public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

            [DllImport("dwmapi.dll")]
            public static extern int DwmSetWindowAttribute(
                IntPtr hwnd,
                int dwAttribute,
                ref int pvAttribute,
                int cbAttribute);
        }

        void VSThemeWindow(Control control, bool forDialog)
        {
            bool ok =
                MaybeTheme<ToolStrip>(ThemeOne, control, forDialog)
                || MaybeTheme<Label>(ThemeOne, control, forDialog)
                || MaybeTheme<TextBox>(ThemeOne, control, forDialog)
                || MaybeTheme<ListView>(ThemeOne, control, forDialog)
                || MaybeTheme<TreeView>(ThemeOne, control, forDialog)
                || MaybeTheme<Panel>(ThemeOne, control, forDialog)
                || MaybeTheme<UserControl>(ThemeOne, control, forDialog)
                || MaybeTheme<PropertyGrid>(ThemeOne, control, forDialog)
                || MaybeTheme<ComboBox>(ThemeOne, control, forDialog)
                || MaybeTheme<NumericUpDown>(ThemeOne, control, forDialog)
                || MaybeTheme<SplitContainer>(ThemeOne, control, forDialog)
                || MaybeTheme<IHasSplitterColor>(ThemeOne, control, forDialog)
                || MaybeTheme<Button>(ThemeOne, control, forDialog)
                || MaybeTheme<ContainerControl>(ThemeOne, control, forDialog)
                || MaybeTheme<ScrollableControl>(ThemeOne, control, forDialog);

            // Controls without an Ankh-specific color adapter (for example
            // CheckBox, RadioButton, GroupBox and TabControl) should still use
            // Visual Studio's native theming instead of retaining Windows
            // light-theme rendering inside a themed dialog.
            if (!ok && control.IsHandleCreated)
                VSThemeWindow(control.Handle, forDialog);
        }
    }
}
