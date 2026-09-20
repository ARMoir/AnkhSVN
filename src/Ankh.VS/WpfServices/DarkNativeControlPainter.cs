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
using System.Windows.Forms;

namespace Ankh.WpfPackage.Services
{
    internal sealed class DarkComboBoxPainter : NativeWindow, IDisposable
    {
        const int WM_PAINT = 0x000F;
        const int WM_PRINTCLIENT = 0x0318;

        readonly ComboBox _combo;
        bool _darkMode;

        internal DarkComboBoxPainter(ComboBox combo)
        {
            if (combo == null)
                throw new ArgumentNullException("combo");

            _combo = combo;
            _combo.HandleCreated += OnHandleCreated;
            _combo.HandleDestroyed += OnHandleDestroyed;
            _combo.Disposed += OnDisposed;

            if (_combo.IsHandleCreated)
                AssignHandle(_combo.Handle);
        }

        internal void SetDarkMode(bool darkMode)
        {
            _darkMode = darkMode;
            if (_combo.IsHandleCreated)
                _combo.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (_darkMode
                && _combo.DropDownStyle != ComboBoxStyle.Simple
                && (m.Msg == WM_PAINT || m.Msg == WM_PRINTCLIENT))
            {
                PaintDropDownButton();
            }
        }

        void PaintDropDownButton()
        {
            Rectangle client = _combo.ClientRectangle;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            int buttonWidth = Math.Min(
                client.Width,
                Math.Max(18, SystemInformation.VerticalScrollBarWidth));

            Rectangle button = new Rectangle(
                client.Right - buttonWidth,
                client.Top,
                buttonWidth,
                client.Height);

            Color backColor = _combo.BackColor;
            Color borderColor = ControlPaint.Light(backColor, 0.22f);
            Color arrowColor = _combo.Enabled
                ? _combo.ForeColor
                : ControlPaint.Dark(_combo.ForeColor, 0.25f);

            using (Graphics graphics = Graphics.FromHwnd(_combo.Handle))
            using (SolidBrush background = new SolidBrush(backColor))
            using (Pen border = new Pen(borderColor))
            using (SolidBrush arrow = new SolidBrush(arrowColor))
            {
                graphics.FillRectangle(background, button);
                graphics.DrawLine(
                    border,
                    button.Left,
                    button.Top + 1,
                    button.Left,
                    button.Bottom - 2);

                int centerX = button.Left + button.Width / 2;
                int centerY = button.Top + button.Height / 2;
                int halfWidth = Math.Max(3, Math.Min(5, button.Width / 4));
                int halfHeight = Math.Max(2, halfWidth / 2);

                Point[] points =
                {
                    new Point(centerX - halfWidth, centerY - halfHeight),
                    new Point(centerX + halfWidth, centerY - halfHeight),
                    new Point(centerX, centerY + halfHeight)
                };

                graphics.FillPolygon(arrow, points);
            }
        }

        void OnHandleCreated(object sender, EventArgs e)
        {
            if (Handle == IntPtr.Zero && _combo.IsHandleCreated)
                AssignHandle(_combo.Handle);
        }

        void OnHandleDestroyed(object sender, EventArgs e)
        {
            if (Handle != IntPtr.Zero)
                ReleaseHandle();
        }

        void OnDisposed(object sender, EventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            _combo.HandleCreated -= OnHandleCreated;
            _combo.HandleDestroyed -= OnHandleDestroyed;
            _combo.Disposed -= OnDisposed;

            if (Handle != IntPtr.Zero)
                ReleaseHandle();
        }
    }

    internal sealed class DarkNumericUpDownPainter : NativeWindow, IDisposable
    {
        const int WM_PAINT = 0x000F;
        const int WM_PRINTCLIENT = 0x0318;

        readonly NumericUpDown _owner;
        Control _buttons;
        bool _darkMode;

        internal DarkNumericUpDownPainter(NumericUpDown owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");

            _owner = owner;
            _owner.HandleCreated += OnOwnerHandleCreated;
            _owner.HandleDestroyed += OnOwnerHandleDestroyed;
            _owner.Disposed += OnOwnerDisposed;

            HookButtons();
        }

        internal void SetDarkMode(bool darkMode)
        {
            _darkMode = darkMode;
            HookButtons();

            if (_buttons != null && _buttons.IsHandleCreated)
                _buttons.Invalidate();
        }

        void HookButtons()
        {
            Control buttons = null;
            foreach (Control child in _owner.Controls)
            {
                if (child.GetType().Name.IndexOf(
                    "UpDownButtons",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    buttons = child;
                    break;
                }
            }

            if (ReferenceEquals(_buttons, buttons)
                && Handle != IntPtr.Zero)
            {
                return;
            }

            if (Handle != IntPtr.Zero)
                ReleaseHandle();

            _buttons = buttons;

            if (_buttons != null && _buttons.IsHandleCreated)
                AssignHandle(_buttons.Handle);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (_darkMode
                && _buttons != null
                && (m.Msg == WM_PAINT || m.Msg == WM_PRINTCLIENT))
            {
                PaintButtons();
            }
        }

        void PaintButtons()
        {
            Rectangle bounds = _buttons.ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            Color backColor = _owner.BackColor;
            Color borderColor = ControlPaint.Light(backColor, 0.22f);
            Color glyphColor = _owner.Enabled
                ? _owner.ForeColor
                : ControlPaint.Dark(_owner.ForeColor, 0.25f);

            int splitY = bounds.Top + bounds.Height / 2;

            using (Graphics graphics = Graphics.FromHwnd(_buttons.Handle))
            using (SolidBrush background = new SolidBrush(backColor))
            using (Pen border = new Pen(borderColor))
            using (SolidBrush glyph = new SolidBrush(glyphColor))
            {
                graphics.FillRectangle(background, bounds);
                graphics.DrawLine(border, bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
                graphics.DrawLine(border, bounds.Left, splitY, bounds.Right, splitY);

                DrawArrow(
                    graphics,
                    glyph,
                    new Rectangle(bounds.Left, bounds.Top, bounds.Width, splitY - bounds.Top),
                    true);

                DrawArrow(
                    graphics,
                    glyph,
                    new Rectangle(bounds.Left, splitY, bounds.Width, bounds.Bottom - splitY),
                    false);
            }
        }

        static void DrawArrow(
            Graphics graphics,
            Brush brush,
            Rectangle bounds,
            bool up)
        {
            int centerX = bounds.Left + bounds.Width / 2;
            int centerY = bounds.Top + bounds.Height / 2;
            int halfWidth = Math.Max(2, Math.Min(4, bounds.Width / 4));
            int halfHeight = Math.Max(1, halfWidth / 2);

            Point[] points;
            if (up)
            {
                points = new[]
                {
                    new Point(centerX - halfWidth, centerY + halfHeight),
                    new Point(centerX + halfWidth, centerY + halfHeight),
                    new Point(centerX, centerY - halfHeight)
                };
            }
            else
            {
                points = new[]
                {
                    new Point(centerX - halfWidth, centerY - halfHeight),
                    new Point(centerX + halfWidth, centerY - halfHeight),
                    new Point(centerX, centerY + halfHeight)
                };
            }

            graphics.FillPolygon(brush, points);
        }

        void OnOwnerHandleCreated(object sender, EventArgs e)
        {
            HookButtons();
        }

        void OnOwnerHandleDestroyed(object sender, EventArgs e)
        {
            if (Handle != IntPtr.Zero)
                ReleaseHandle();
        }

        void OnOwnerDisposed(object sender, EventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            _owner.HandleCreated -= OnOwnerHandleCreated;
            _owner.HandleDestroyed -= OnOwnerHandleDestroyed;
            _owner.Disposed -= OnOwnerDisposed;

            if (Handle != IntPtr.Zero)
                ReleaseHandle();

            _buttons = null;
        }
    }
}
