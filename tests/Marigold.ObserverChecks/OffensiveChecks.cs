using System;
using System.Collections.Generic;
using Marigold;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
internal static class OffensiveChecks
{
    private static int count;
    private static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
    public static void Run()
    {
        var manager=MyDefinitionManager.Static=new MyDefinitionManager();
        manager.Definitions.Add(new MyWeaponBlockDefinition{Id=new MyDefinitionId("MyObjectBuilder_ConveyorSorter/WeaponWithoutName")});
        manager.Definitions.Add(new MyCubeBlockDefinition{Id=new MyDefinitionId("MyObjectBuilder_CubeBlock/TargetGroupOnly"),WeaponTarget=true});
        manager.Definitions.Add(new MyCubeBlockDefinition{Id=new MyDefinitionId("MyObjectBuilder_ShipWelder/Utility"),WeaponTarget=true});
        var utilities=MyAPIGateway.Utilities;utilities.WeaponDefinitions.Clear();
        string pdc="MyObjectBuilder_ConveyorSorter/sdx_pdcImprovised";
        using(var catalog=new OffensiveDefinitions())
        {
            catalog.Update();
            Check(DisplayFilter.Category("MyObjectBuilder_ConveyorSorter/WeaponWithoutName")=="weapons","Weapon definitions override sorter type");
            Check(DisplayFilter.Category("MyObjectBuilder_CubeBlock/TargetGroupOnly")=="weapons","Targeting-group membership classifies offensive blocks");
            Check(DisplayFilter.Category("MyObjectBuilder_ShipWelder/Utility")=="tools","Vanilla target-group tools remain Utility");
            Check(DisplayFilter.Category(pdc)=="conveyors","No speculative subtype-name detection without registry");
            utilities.WeaponDefinitions.Add(new MyDefinitionId(pdc));
            for(int i=0;i<300;i++)catalog.Update();
            Check(ShipHealth.Group(pdc)==6 && DisplayFilter.Category("MyObjectBuilder_ConveyorSorter/Normal")=="conveyors","Late WeaponCore registry arrival overrides only registered definitions");
            var expected=new BlockSample{Definition=pdc};var health=ShipHealth.Build(new[]{expected},new BlockSample[0]);
            Check(health.Counts[6,3]==1 && health.Total(4)==0,"Saved missing weapon classifies without any live block");
            int revision=DisplayFilter.ClassificationRevision;for(int i=0;i<300;i++)catalog.Update();
            Check(DisplayFilter.ClassificationRevision==revision,"Unchanged registry does not force geometry rebuilds");
        }
        Check(utilities.ModHandlers==0 && DisplayFilter.Category(pdc)=="conveyors","Unload unregisters API and clears session classification");
        manager.Definitions.Clear();utilities.WeaponDefinitions.Clear();
        Console.WriteLine("PASS: "+count+" offensive definition/API lifecycle checks (API doubles).");
    }
}
namespace VRage.Game
{
    public struct MyDefinitionId
    {
        private string value;public MyDefinitionId(string value){this.value=value;}
        public string TypeId {get{return value.Split('/')[0];}}
        public override string ToString(){return value;}
    }
}
namespace VRage.Utils
{
    public struct MyStringHash {public static MyStringHash GetOrCompute(string value){return new MyStringHash();}}
}
namespace Sandbox.Definitions
{
    public class MyCubeBlockDefinition
    {
        public MyDefinitionId Id;public bool WeaponTarget;
        public bool MatchingTurretTargetingGroup(VRage.Utils.MyStringHash hash){return WeaponTarget;}
    }
    public class MyWeaponBlockDefinition : MyCubeBlockDefinition {}
    public class MyDefinitionManager
    {
        public static MyDefinitionManager Static=new MyDefinitionManager();
        public List<MyCubeBlockDefinition> Definitions=new List<MyCubeBlockDefinition>();
        public IEnumerable<MyCubeBlockDefinition> GetAllDefinitions(){return Definitions;}
    }
}
namespace VRage.Game.ModAPI
{
    public interface IMyUtilities
    {
        void RegisterMessageHandler(long channel,Action<object> handler);
        void UnregisterMessageHandler(long channel,Action<object> handler);
        void SendModMessage(long channel,object message);
    }
}
namespace Sandbox.ModAPI
{
    public sealed partial class Utilities
    {
        private event Action<object> handler;
        public int ModHandlers {get{return handler==null?0:handler.GetInvocationList().Length;}}
        public readonly List<MyDefinitionId> WeaponDefinitions=new List<MyDefinitionId>();
        public void RegisterMessageHandler(long channel,Action<object> callback){handler+=callback;}
        public void UnregisterMessageHandler(long channel,Action<object> callback){handler-=callback;}
        public void SendModMessage(long channel,object message)
        {
            if(WeaponDefinitions.Count==0)return;
            handler?.Invoke(new Dictionary<string,Delegate>{{"GetCoreWeapons",new Action<ICollection<MyDefinitionId>>(items=>{foreach(var id in WeaponDefinitions)items.Add(id);})}});
        }
    }
}
