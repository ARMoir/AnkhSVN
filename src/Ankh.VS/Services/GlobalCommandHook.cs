using System;
using System.Collections.Generic;
using Ankh.Services;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Ankh.VS.Services
{
    [GlobalService(typeof(IAnkhGlobalCommandHook))]
    sealed class GlobalCommandHook : AnkhService, IAnkhGlobalCommandHook, IOleCommandTarget
    {
        readonly Dictionary<Guid, Dictionary<int, EventHandler>> _commandMap = new Dictionary<Guid,Dictionary<int,EventHandler>>();
        bool _hooked;
        uint _cookie;
        public GlobalCommandHook(IAnkhServiceProvider context)
            : base(context)
        {
        }

        public void HookCommand(System.ComponentModel.Design.CommandID command, EventHandler handler)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            else if (handler == null)
                throw new ArgumentNullException("handler");

            Dictionary<int, EventHandler> map;

            if (!_commandMap.TryGetValue(command.Guid, out map))
            {
                map = new Dictionary<int, EventHandler>();
                _commandMap[command.Guid] = map;
            }

            EventHandler handlers;
            if (!map.TryGetValue(command.ID, out handlers))
                handlers = null;

            map[command.ID] = (handlers + handler);

            if (!_hooked)
            {
                IVsRegisterPriorityCommandTarget svc = GetService<IVsRegisterPriorityCommandTarget>(typeof(SVsRegisterPriorityCommandTarget));

                if (svc != null
                    && VSErr.Succeeded(svc.RegisterPriorityCommandTarget(0, this, out _cookie)))
                {
                    _hooked = true;
                }
            }
        }

        public void UnhookCommand(System.ComponentModel.Design.CommandID command, EventHandler handler)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            else if (handler == null)
                throw new ArgumentNullException("handler");
            Dictionary<int, EventHandler> map;

            if (!_commandMap.TryGetValue(command.Guid, out map))
                return;

            if (!map.TryGetValue(command.ID, out EventHandler handlers))
                return;

            handlers -= handler;

            if (handlers == null)
            {
                map.Remove(command.ID);

                if (map.Count == 0)
                {
                    _commandMap.Remove(command.Guid);

                    if (_commandMap.Count == 0)
                    {
                        Unhook();
                    }
                }
            }

            map[command.ID] = (handlers + handler);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                    Unhook();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        void Unhook()
        {
            if (_hooked)
            {
                _hooked = false;
                GetService<IVsRegisterPriorityCommandTarget>(typeof(SVsRegisterPriorityCommandTarget)).UnregisterPriorityCommandTarget(_cookie);
            }
        }

        static unsafe bool GuidRefIsNull(ref Guid pguidCmdGroup)
        {
            // IOleCommandTarget permits a null native pointer for the default command group.
            // The COM interop signature exposes it as ref Guid, so inspect the address
            // without dereferencing it. We do not support the default command group.
            return &pguidCmdGroup == null;
        }


        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // Exit as quickly as possible without creating exceptions. This function is performance critical!

            if (GuidRefIsNull(ref pguidCmdGroup))
                return VSErr.OLECMDERR_E_NOTSUPPORTED;

            Dictionary<int, EventHandler> cmdMap;
            if (!_commandMap.TryGetValue(pguidCmdGroup, out cmdMap))
                return VSErr.OLECMDERR_E_UNKNOWNGROUP;

            EventHandler handler;
            if (cmdMap.TryGetValue(unchecked((int)nCmdID), out handler))
            {
                try
                {
                    handler(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    IAnkhErrorHandler eh = GetService<IAnkhErrorHandler>();

                    if (eh != null && eh.IsEnabled(ex))
                        eh.OnError(ex);
                    else
                        throw;
                }
            }

            return VSErr.OLECMDERR_E_NOTSUPPORTED;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // Don't do anything here. This function is 100% performance critical!
            return VSErr.OLECMDERR_E_NOTSUPPORTED;
        }
    }
}
