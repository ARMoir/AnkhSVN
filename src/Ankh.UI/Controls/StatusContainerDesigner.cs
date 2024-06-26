// Copyright 2009 The AnkhSVN Project
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms.Design;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Microsoft;

namespace Ankh.UI.Controls
{
    class StatusContainerDesigner : ParentControlDesigner
    {
        StatusContainer _container;
        IDesignerHost _designerHost;
        bool _disableGrid;

        public override void Initialize(System.ComponentModel.IComponent component)
        {
            base.Initialize(component);
            AutoResizeHandles = true;

            _container = (StatusContainer)component;

            _container.ControlAdded += new System.Windows.Forms.ControlEventHandler(OnPanelAdded);
            _container.ControlRemoved += new System.Windows.Forms.ControlEventHandler(OnControlRemoved);

            _designerHost = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost));
            Assumes.Present(
            _designerHost);
        }


        void OnControlRemoved(object sender, System.Windows.Forms.ControlEventArgs e)
        {
        }

        void OnPanelAdded(object sender, System.Windows.Forms.ControlEventArgs e)
        {
        }

        protected override bool AllowControlLasso
        {
            get { return false; }
        }

        public override System.Collections.ICollection AssociatedComponents
        {
            get
            {
                List<Control> controls = new List<Control>();
                foreach (StatusPanel panel in _container.Controls)
                {

                    foreach (Control c in panel.Controls)
                    {
                        controls.Add(c);
                    }
                }
                return controls;
            }
        }

        public override int NumberOfInternalControlDesigners()
        {
            return _container.Controls.Count;
        }

        public override ControlDesigner InternalControlDesigner(int internalControlIndex)
        {
            if (internalControlIndex < 0 || internalControlIndex >= _container.Controls.Count)
                throw new ArgumentOutOfRangeException();

            StatusPanel panel = _container.Controls[internalControlIndex] as StatusPanel;

            return (ControlDesigner)_designerHost.GetDesigner(panel);
        }

        public override bool CanParent(System.Windows.Forms.Control control)
        {
            return (control is StatusPanel);
        }

        public override bool CanParent(ControlDesigner controlDesigner)
        {
            return base.CanParent(controlDesigner);
        }

        protected override void OnPaintAdornments(PaintEventArgs pe)
        {
            try
            {
                _disableGrid = true;
                base.OnPaintAdornments(pe);
            }
            finally
            {
                _disableGrid = false;
            }

        }

        protected override bool DrawGrid
        {
            get
            {
                return base.DrawGrid && !_disableGrid;
            }
            set
            {
                base.DrawGrid = value;
            }
        }

        DesignerVerbCollection _verbs;
        public override DesignerVerbCollection Verbs
        {
            get
            {
                if (_verbs == null)
                {
                    _verbs = new DesignerVerbCollection();
                    _verbs.Add(new DesignerVerb("Add Panel", OnAddPanel));
                }
                return _verbs;
            }
        }

        void OnAddPanel(object sender, EventArgs e)
        {
            IDesignerHost service = (IDesignerHost)this.GetService(typeof(IDesignerHost));

            StatusPanel panel = (StatusPanel)service.CreateComponent(typeof(StatusPanel));

            _container.Controls.Add(panel);
        }
    }
}
