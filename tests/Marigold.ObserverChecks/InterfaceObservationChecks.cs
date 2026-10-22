using System;
using System.Collections.Generic;
using System.IO;
using Marigold;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
internal static class InterfaceObservationChecks
{
    private static int count;
    private static void Check(bool value,string message){if(!value)throw new Exception(message);count++;}
    public static void Run()
    {
        var scheduler=new ObservationSchedule<int>();var items=new List<int>();for(int i=0;i<50000;i++)items.Add(i);scheduler.Reset(items);
        var seen=new HashSet<int>();int reads=0;
        for(int i=0;i<200;i++){scheduler.Mark(7);scheduler.Mark(7);var batch=scheduler.Take(512);Check(batch.Count<=512,"Read budget bounded");foreach(int item in batch)seen.Add(item);reads+=batch.Count;}
        Check(seen.Count==50000 && scheduler.Ready,"Event traffic cannot starve reconciliation");
        scheduler.Reset(new int[0]);Check(scheduler.Ready && scheduler.Take(512).Count==0,"Empty grid ready");
        var grid=new MyCubeGrid();for(int i=0;i<1100;i++)grid.Add(new MySlimBlock {Position=new Point3(i,0,0),Min=new Point3(i,0,0),Max=new Point3(i,0,0)});
        var observer=new GridObservation(grid);observer.Step(512);Check(!observer.Ready,"Partial initial scan never published");observer.Step(512);observer.Step(512);Check(observer.Ready && observer.Snapshot().Length==1100,"Initial scan complete");
        var block=grid.GetCubeBlock(new Point3(4,0,0));block.Integrity=1;grid.Damage(block);observer.Step(512);
        Check(Array.Find(observer.Snapshot(),b=>b.Position.Equals(block.Position)).Integrity==1,"Integrity event prioritized");
        grid.Remove(block);Check(observer.Snapshot().Length==1099,"Removal reflected immediately");
        var replacement=new MySlimBlock {Position=block.Position,Min=block.Min,Max=block.Max,Integrity=75};grid.Add(replacement);observer.Step(512);
        Check(observer.Snapshot().Length==1100 && Array.Find(observer.Snapshot(),b=>b.Position.Equals(block.Position)).Integrity==75,"Replacement does not use old block object");
        grid.Blocks.Remove(replacement.Position);for(int i=0;i<610;i++)observer.Step(512);Check(observer.Snapshot().Length==1099,"Reconciliation catches missed removal events");
        observer.Dispose();Check(grid.HandlerCount==0,"Dispose releases all observation events");
        var c=new SchematicControls();c.Execute("/mg ui");Check(c.ReferenceAction=="ui","UI command local routing");c.Execute("/mg language es");Check(c.ReferenceAction=="language es","Language command routing");
        Check(UiText.Get("es","Profiles")=="Perfiles" && UiText.Get("es","Untranslated") == "Untranslated","Translations and fallback");
        string root=Path.Combine(Directory.GetCurrentDirectory(),".cache","InterfaceTests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            string sub=Path.Combine(root,"Ship Name");Directory.CreateDirectory(sub);File.WriteAllText(Path.Combine(sub,"bp.sbc"),"unchanged");Directory.CreateDirectory(Path.Combine(root,"Empty"));
            var files=BlueprintCatalog.Find(root);Check(files.Length==1 && files[0].EndsWith("bp.sbc") && File.ReadAllText(files[0])=="unchanged","Catalog reads only local blueprint paths");
            Check(BlueprintCatalog.Find(Path.Combine(root,"Missing")).Length==0,"Absent catalog root supported");
            c.Language="es";string settings=Path.Combine(root,"display.txt");DisplaySettings.Save(settings,c);var loaded=new SchematicControls();DisplaySettings.Load(settings,loaded);Check(loaded.Language=="es","Language persists");
            var lines=File.ReadAllLines(settings);lines[10]="unsupported";File.WriteAllLines(settings,lines);bool failed=false;try{DisplaySettings.Load(settings,loaded);}catch(InvalidDataException){failed=true;}Check(failed && loaded.Language=="es","Invalid language preserves settings");
        }
        finally{Directory.Delete(root,true);}
        Console.WriteLine("PASS: "+count+" interface/observation checks (GUI rendering remains in-game).");
    }
}
namespace Sandbox.ModAPI { public interface IMyFunctionalBlock {} }
namespace Sandbox.Game.Entities.Cube
{
    public sealed class TestDefinition {public string Id="CubeBlock/Armor";}
    public sealed class TestOrientation {public int Forward=0,Up=4;}
    public class TestFatBlock {public bool IsFunctional=true;}
    public sealed class MySlimBlock
    {internal Point3 Position,Min,Max;public TestDefinition BlockDefinition=new TestDefinition();public TestOrientation Orientation=new TestOrientation();public TestFatBlock FatBlock;public double Integrity=100,BuildIntegrity=100,MaxIntegrity=100;}
}
namespace Sandbox.Game.Entities
{
    public sealed partial class MyCubeGrid
    {
        internal readonly Dictionary<Point3,MySlimBlock> Blocks=new Dictionary<Point3,MySlimBlock>();
        public event Action<MySlimBlock> OnBlockAdded,OnBlockRemoved,OnBlockIntegrityChanged;
        public int HandlerCount {get{return (OnBlockAdded?.GetInvocationList().Length??0)+(OnBlockRemoved?.GetInvocationList().Length??0)+(OnBlockIntegrityChanged?.GetInvocationList().Length??0);}}
        public IEnumerable<MySlimBlock> GetBlocks(){return Blocks.Values;}
        internal MySlimBlock GetCubeBlock(Point3 point){MySlimBlock value;Blocks.TryGetValue(point,out value);return value;}
        public void Add(MySlimBlock b){Blocks[b.Position]=b;OnBlockAdded?.Invoke(b);}
        public void Remove(MySlimBlock b){Blocks.Remove(b.Position);OnBlockRemoved?.Invoke(b);}
        public void Damage(MySlimBlock b){OnBlockIntegrityChanged?.Invoke(b);}
    }
}
