using System;
using Marigold;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;

// Small API doubles exercise observer behavior; they do not validate the game runtime.
internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }

    private static void Tick(Plugin plugin)
    {
        for (int i = 0; i < 15; i++) plugin.Update();
    }

    public static void Main()
    {
        var plugin = new Plugin();
        plugin.Init(null);
        plugin.Init(null);
        Check(MySession.Handlers == 1, "Initialization must not duplicate subscriptions");
        plugin.Update();
        Check(MyAPIGateway.Utilities.Created == 0, "Menu must not create HUD");

        var grid = new MyCubeGrid { DisplayName = "Miner {A}", BlocksCount = 42 };
        var controller = new Controller { CubeGrid = grid };
        MySession.Static = new MySession { ControlledEntity = new Control { Entity = controller } };
        plugin.Update();
        var overlay=SchematicOverlay.Last;
        Check(ReferenceEquals(overlay.Grid,grid), "Controlled grid reaches overlay");
        Tick(plugin);
        Check(MyAPIGateway.Utilities.Created==0,"Legacy notification must never be created");
        var secondGrid = new MyCubeGrid { DisplayName="Corvette" };
        controller.CubeGrid=secondGrid;
        plugin.Update();
        Check(ReferenceEquals(overlay.Grid,secondGrid),"Grid switch reaches overlay");
        MySession.Static.ControlledEntity.Entity=new object();
        plugin.Update();
        Check(overlay.Grid==null,"Leaving controller clears overlay");
        MySession.Static.ControlledEntity.Entity=controller;
        plugin.Update();
        secondGrid.MarkedForClose=true;
        plugin.Update();
        Check(overlay.Grid==null,"Closing grid clears overlay");
        secondGrid.MarkedForClose=false;
        plugin.Update();
        MySession.Unload();
        plugin.Update();
        Check(overlay.Grid==null && !overlay.CommandsBound,"Unload must not reattach");
        MySession.Static=new MySession { ControlledEntity=new Control { Entity=controller } };
        plugin.Update();
        Check(ReferenceEquals(overlay.Grid,secondGrid),"New session resumes");
        overlay.ThrowOnUpdate=true;
        Tick(plugin);
        int errors=VRage.Utils.MyLog.Default.Errors;
        Tick(plugin);
        Check(errors==1 && VRage.Utils.MyLog.Default.Errors==1 && overlay.Grid==null,"Failure clears and logs once");
        overlay.ThrowOnUpdate=false;
        MySession.Static=new MySession { ControlledEntity=new Control { Entity=controller } };
        plugin.Update();
        Check(ReferenceEquals(overlay.Grid,secondGrid),"New session recovers");
        plugin.Dispose();plugin.Dispose();
        Check(MySession.Handlers==0 && overlay.Grid==null && overlay.Disposed,"Disposal cleans up");
        Console.WriteLine("PASS: " + checks + " observer lifecycle checks (API doubles, not in-game validation).");
        FillChecks.Run();
        SnapshotChecks.Run();
        GeometryChecks.Run();
        ControlChecks.Run();
    }
}

namespace VRage.Plugins { public interface IPlugin : IDisposable { void Init(object game); void Update(); } }
namespace Marigold
{
    internal sealed class SchematicOverlay : IDisposable
    {
        public static SchematicOverlay Last;
        public MyCubeGrid Grid;
        public bool CommandsBound, ThrowOnUpdate, Disposed;
        public void Init() { Last=this; }
        public void BindSessionCommands(bool available) { CommandsBound=available; }
        public void SetGrid(MyCubeGrid grid) { Grid=grid; }
        public void Update(MyCubeGrid grid, IMyShipController controller) { if(ThrowOnUpdate) throw new Exception("Simulated failure"); Grid=grid; }
        public void Dispose() { Grid=null; Disposed=true; }
    }
}
namespace VRage.Utils
{
    public sealed class MyLog
    {
        public static readonly MyLog Default = new MyLog();
        public int Errors;
        public void WriteLineAndConsole(string text) { if (text.Contains("Observer disabled")) Errors++; }
    }
}
namespace Sandbox.Game.Entities
{
    public sealed class MyCubeGrid { public string DisplayName; public int BlocksCount; public bool Closed; public bool MarkedForClose; }
}
namespace Sandbox.Game.World
{
    public sealed class Control { public object Entity; }
    public sealed class MySession
    {
        public static MySession Static;
        public Control ControlledEntity;
        public static event Action OnUnloading;
        public static int Handlers { get { return OnUnloading?.GetInvocationList().Length ?? 0; } }
        public static void Unload() { OnUnloading?.Invoke(); }
    }
}
namespace VRage.Game.ModAPI
{
    public interface IMyHudNotification { string Text { get; set; } void Show(); void Hide(); }
}
namespace Sandbox.ModAPI
{
    public interface IMyShipController { MyCubeGrid CubeGrid { get; } }
    public sealed class Controller : IMyShipController { public MyCubeGrid CubeGrid { get; set; } }
    public static class MyAPIGateway { public static Utilities Utilities = new Utilities(); }
    public sealed class Utilities
    {
        public int Created;
        public Notification Last;
        public VRage.Game.ModAPI.IMyHudNotification CreateNotification(string text, int duration, string font)
        {
            Created++;
            Last = new Notification { Text = text };
            return Last;
        }
    }
    public sealed class Notification : VRage.Game.ModAPI.IMyHudNotification
    {
        public string Text { get; set; }
        public string Rendered;
        public bool Visible;
        public bool ThrowOnShow;
        public void Show()
        {
            if (ThrowOnShow) throw new InvalidOperationException("Simulated HUD failure");
            if (!Visible) Rendered = Text;
            Visible = true;
        }
        public void Hide() { Visible = false; }
    }
}
