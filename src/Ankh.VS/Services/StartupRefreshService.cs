using System;
using Ankh.Commands;
using Ankh.Scc;
using Ankh.Services;
using Ankh.UI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Ankh.VS.Services
{
    [GlobalService(typeof(StartupRefreshService))]
    sealed class StartupRefreshService : AnkhService, IAnkhIdleProcessor
    {
        bool _started;
        bool _themeRefreshed;
        bool _statusPending = true;
        AnkhServiceEvents _events;
        IAnkhPackage _package;

        public StartupRefreshService(IAnkhServiceProvider context) : base(context) { }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _events = GetService<AnkhServiceEvents>();
            _events.RuntimeStarted += OnStarted;
            _events.SolutionOpened += OnStatusNeeded;
            _events.SccProviderActivated += OnStatusNeeded;
            _package = GetService<IAnkhPackage>();
            _package.RegisterIdleProcessor(this);
        }

        void OnStarted(object sender, EventArgs e) { _started = true; }
        void OnStatusNeeded(object sender, EventArgs e) { _statusPending = true; }

        public void OnIdle(AnkhIdleArgs e)
        {
            if (!_started || e.Priority || (_themeRefreshed && !_statusPending))
                return;

            IAnkhCommandStates states = GetService<IAnkhCommandStates>();
            if (states == null || !states.UIShellAvailable)
                return;

            if (!_themeRefreshed)
            {
                IVsShell shell = GetService<IVsShell>(typeof(SVsShell));
                if (shell == null)
                    return;

                object initialized;
                // Older shells need only the non-zombie check above. Newer
                // shells expose a separate completion signal; query it so a
                // package loaded after the notification is handled as well.
                if (VSErr.Succeeded(shell.GetProperty(-9053, out initialized))
                    && initialized is bool && !(bool)initialized)
                    return;

                _themeRefreshed = true;
                IAnkhServiceEvents events = GetService<IAnkhServiceEvents>();
                events.OnThemeChanged(EventArgs.Empty);
                events.OnUIShellActivate(EventArgs.Empty);
            }

            if (!_statusPending || !states.SccProviderActive)
                return;

            IProjectFileMapper mapper = GetService<IProjectFileMapper>();
            IFileStatusMonitor monitor = GetService<IFileStatusMonitor>();
            IPendingChangesManager pending = GetService<IPendingChangesManager>();
            IAnkhOpenDocumentTracker documents = GetService<IAnkhOpenDocumentTracker>();
            if (mapper == null || monitor == null || pending == null || documents == null)
                return;

            _statusPending = false;
            // Refresh the entire solution, independent of the current selection.
            // The monitor schedules status work and its dependent glyph updates.
            pending.FullRefresh(true);
            monitor.ScheduleSvnStatus(mapper.GetAllFilesOfAllProjects());
            monitor.ScheduleGlyphOnlyUpdate(mapper.GetAllSccProjects());
            documents.RefreshDirtyState();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_events != null)
                {
                    _events.RuntimeStarted -= OnStarted;
                    _events.SolutionOpened -= OnStatusNeeded;
                    _events.SccProviderActivated -= OnStatusNeeded;
                }
                if (_package != null)
                    _package.UnregisterIdleProcessor(this);
            }
            base.Dispose(disposing);
        }
    }
}
