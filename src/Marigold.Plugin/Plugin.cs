using System;
using System.Reflection;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Plugins;
using VRage.Utils;

#if !LOCAL_BUILD
[assembly: AssemblyVersion("0.5.0.0")]
[assembly: AssemblyFileVersion("0.5.0.0")]
#endif

namespace Marigold
{
    public sealed class Plugin : IPlugin
    {
        private MySession session;
        private MySession unloadingSession;
        private MyCubeGrid grid;
        private bool initialized;
        private bool failed;
        private SchematicOverlay schematic;

        public void Init(object gameInstance)
        {
            if (initialized)
                return;

            initialized = true;
            MySession.OnUnloading += OnUnloading;
            try
            {
                schematic = new SchematicOverlay();
                schematic.Init();
            }
            catch (Exception exception)
            {
                schematic?.Dispose();
                schematic = null;
                MyLog.Default.WriteLineAndConsole("[Marigold] Schematic initialization failed: " + exception);
            }
            MyLog.Default.WriteLineAndConsole("[Marigold] Ship observer initialized (0.5.0).");
        }

        public void Update()
        {
            if (!initialized)
                return;

            try
            {
                var currentSession = MySession.Static;
                schematic?.BindSessionCommands(currentSession != null && !ReferenceEquals(currentSession, unloadingSession));
                if (!ReferenceEquals(currentSession, unloadingSession))
                    unloadingSession = null;
                if (!ReferenceEquals(session, currentSession))
                {
                    schematic?.BindSessionCommands(false);
                    ClearDisplay();
                    session = currentSession;
                    schematic?.BindSessionCommands(currentSession != null && !ReferenceEquals(currentSession, unloadingSession));
                    failed = false;
                }

                if (session == null || ReferenceEquals(session, unloadingSession) || failed)
                    return;

                var controller = session.ControlledEntity?.Entity as IMyShipController;
                var currentGrid = controller?.CubeGrid as MyCubeGrid;
                if (currentGrid == null || currentGrid.Closed || currentGrid.MarkedForClose)
                {
                    ClearDisplay();
                    return;
                }

                if (!ReferenceEquals(grid, currentGrid))
                {
                    ClearDisplay();
                    grid = currentGrid;
                }

                schematic?.Update(grid, controller);

            }
            catch (Exception exception)
            {
                // Stop this observer for the session instead of spamming errors every frame.
                failed = true;
                ClearDisplay();
                MyLog.Default.WriteLineAndConsole("[Marigold] Observer disabled until the next session: " + exception);
            }
        }

        private void OnUnloading()
        {
            schematic?.BindSessionCommands(false);
            unloadingSession = MySession.Static;
            ClearDisplay();
            session = null;
        }

        private void ClearDisplay()
        {
            schematic?.SetGrid(null);
            grid = null;
        }

        public void Dispose()
        {
            if (!initialized)
                return;

            initialized = false;
            MySession.OnUnloading -= OnUnloading;
            ClearDisplay();
            schematic?.Dispose();
            schematic = null;
            session = null;
            unloadingSession = null;
        }
    }
}


