using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRageMath;
namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private string stationStatus="";
        private readonly Dictionary<MyCubeGrid,GridObservation> stationObservers=new Dictionary<MyCubeGrid,GridObservation>();
        private void ClearStationObservers(){foreach(var value in stationObservers.Values)value.Dispose();stationObservers.Clear();}
        private void UpdateStationObservers()
        {
            if(!controls.Station){ClearStationObservers();return;}
            if(!controls.Visible)return;
            int budget=System.Math.Max(32,512/System.Math.Max(1,stationObservers.Count));
            foreach(var observer in stationObservers.Values)observer.Step(budget);
        }
        private StationGrid[] SampleStation()
        {
            if(!controls.Station){stationStatus="";return new StationGrid[0];}
            var seen=new HashSet<MyCubeGrid>();var queue=new Queue<MyCubeGrid>();var result=new List<StationGrid>();seen.Add(grid);queue.Enqueue(grid);
            int blocks=0;bool limited=false;
            while(queue.Count>0)
            {
                var current=queue.Dequeue();
                foreach(var block in current.GetBlocks())
                {
                    var connector=block.FatBlock as Sandbox.ModAPI.IMyShipConnector;
                    if(connector==null || connector.Status!=MyShipConnectorStatus.Connected)continue;
                    var next=connector.OtherConnector?.CubeGrid as MyCubeGrid;
                    if(next==null || next.Closed || next.MarkedForClose || seen.Contains(next))continue;
                    if(seen.Count>=9){limited=true;continue;}
                    seen.Add(next);queue.Enqueue(next);
                }
                if(current==grid)continue;
                GridObservation observer;
                if(!stationObservers.TryGetValue(current,out observer)){observer=new GridObservation(current);stationObservers.Add(current,observer);}
                if(!observer.Ready){limited=true;continue;}
                var snapshot=observer.Snapshot();
                if(blocks+snapshot.Length>100000){limited=true;continue;}blocks+=snapshot.Length;
                MatrixD transform=current.WorldMatrix*grid.PositionComp.WorldMatrixNormalizedInv;
                double scale=current.GridSize/grid.GridSize;
                var origin=transform.Translation/grid.GridSize;var x=transform.Right*scale;var y=transform.Up*scale;var z=transform.Backward*scale;
                result.Add(new StationGrid { Blocks=snapshot,Origin=new Point3(origin.X,origin.Y,origin.Z),X=new Point3(x.X,x.Y,x.Z),Y=new Point3(y.X,y.Y,y.Z),Z=new Point3(z.X,z.Y,z.Z) });
            }
            var stale=new List<MyCubeGrid>();foreach(var pair in stationObservers)if(!seen.Contains(pair.Key) || pair.Key.Closed || pair.Key.MarkedForClose)stale.Add(pair.Key);
            foreach(var old in stale){stationObservers[old].Dispose();stationObservers.Remove(old);}
            stationStatus="Station context: "+result.Count+" connector-linked grids"+(limited?" (loading or display limit reached)":"")+". Baseline and counts cover the controlled grid only.";
            return result.ToArray();
        }
    }
}
