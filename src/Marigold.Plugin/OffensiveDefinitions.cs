using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Marigold
{
    // All API calls run on the game update thread. The optional WeaponCore handshake
    // is local mod messaging, and only its read-only definition query is consumed.
    internal sealed class OffensiveDefinitions : IDisposable
    {
        private const long Channel = 67549756549;
        private const long ToolChannel = 2172757428;
        private volatile Action<ICollection<MyStringHash>> getTools;
        private readonly HashSet<string> vanilla = new HashSet<string>(StringComparer.Ordinal);
        private IMyUtilities utilities;
        private volatile Action<ICollection<MyDefinitionId>> getWeapons;
        private bool loaded, loggedFailure;
        private int ticks;

        public void Update()
        {
            try
            {
                if (!loaded)
                {
                    if (MyDefinitionManager.Static == null || MyAPIGateway.Utilities == null)
                        return;
                    var weapons = MyStringHash.GetOrCompute("Weapons");
                    foreach (var raw in MyDefinitionManager.Static.GetAllDefinitions())
                    {
                        var block = raw as MyCubeBlockDefinition;
                        if (block == null)
                            continue;
                        string id = block.Id.ToString(), type = block.Id.TypeId.ToString();
                        // The game's target group also includes utility tools and decoys.
                        bool utility = type.IndexOf("Drill", StringComparison.OrdinalIgnoreCase) >= 0
                            || type.IndexOf("Welder", StringComparison.OrdinalIgnoreCase) >= 0
                            || type.IndexOf("Grinder", StringComparison.OrdinalIgnoreCase) >= 0
                            || type.IndexOf("Decoy", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (block is MyWeaponBlockDefinition || (!utility && block.MatchingTurretTargetingGroup(weapons)))
                            vanilla.Add(id);
                    }

                    DisplayFilter.SetOffensiveDefinitions(vanilla);
                    utilities = MyAPIGateway.Utilities;
                    utilities.RegisterMessageHandler(Channel, Receive);
                    utilities.RegisterMessageHandler(ToolChannel, ReceiveTools);
                    loaded = true;
                }

                if (ticks++ % 300 != 0)
                    return;
                // Poll ToolCore independently, including worlds without WeaponCore.
                try
                {
                    var toolQuery = getTools;
                    if (toolQuery == null)
                    {
                        utilities.SendModMessage(ToolChannel, "ApiEndpointRequest");
                        toolQuery = getTools;
                    }

                    if (toolQuery != null)
                    {
                        var toolIds = new List<MyStringHash>();
                        toolQuery(toolIds);
                        DisplayFilter.SetToolSubtypes(toolIds.ConvertAll(id => id.String));
                    }
                }
                catch (Exception e)
                {
                    if (!loggedFailure)
                    {
                        loggedFailure = true;
                        MyLog.Default.WriteLineAndConsole("[Marigold] ToolCore lookup: " + e.Message);
                    }
                }

                var query = getWeapons;
                if (query == null)
                {
                    utilities.SendModMessage(Channel, "ApiEndpointRequest");
                    query = getWeapons;
                }

                if (query == null)
                    return;
                var ids = new List<MyDefinitionId>();
                query(ids);
                var combined = new HashSet<string>(vanilla, StringComparer.Ordinal);
                foreach (var id in ids)
                    combined.Add(id.ToString());
                DisplayFilter.SetOffensiveDefinitions(combined);
            }
            catch (Exception e)
            {
                if (!loggedFailure)
                {
                    loggedFailure = true;
                    MyLog.Default.WriteLineAndConsole("[Marigold] Offensive definition lookup: " + e.Message);
                }
            }
        }

        private void ReceiveTools(object message)
        {
            var endpoints = message as IReadOnlyDictionary<string, Delegate>;
            Delegate query;
            if (endpoints != null && endpoints.TryGetValue("GetTCDefs", out query))
                getTools = query as Action<ICollection<MyStringHash>>;
        }

        private void Receive(object message)
        {
            var endpoints = message as IReadOnlyDictionary<string, Delegate>;
            Delegate query;
            if (endpoints != null && endpoints.TryGetValue("GetCoreWeapons", out query))
                getWeapons = query as Action<ICollection<MyDefinitionId>>;
        }

        public void Dispose()
        {
            if (utilities != null)
            {
                utilities.UnregisterMessageHandler(Channel, Receive);
                utilities.UnregisterMessageHandler(ToolChannel, ReceiveTools);
            }

            getTools = null;
            DisplayFilter.SetToolSubtypes(new string[0]);
            utilities = null;
            getWeapons = null;
            loaded = false;
            ticks = 0;
            loggedFailure = false;
            vanilla.Clear();
            DisplayFilter.SetOffensiveDefinitions(new string[0]);
        }
    }
}
