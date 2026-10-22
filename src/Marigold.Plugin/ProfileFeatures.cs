using System;
using System.IO;
using System.Text.RegularExpressions;
namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private string DataRoot {get{return Path.GetDirectoryName(settingsPath);}}
        private ShipProfile SelectedProfile()
        {Reference binding;if(grid==null || !references.TryGetValue(grid,out binding))throw new InvalidOperationException("Load or capture a profile first.");return profileStore.Find(binding.ProfileId);}
        private static string SafeName(string value)
        {if(string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value??"","^[a-zA-Z0-9 _-]{1,40}$"))throw new InvalidOperationException("Use 1–40 letters, digits, spaces, hyphens or underscores.");return value.Trim();}
        private string ShipSettingsPath(string id){return Path.Combine(DataRoot,"display-"+id+".txt");}
        private void LoadShipSettings(string id)
        {
            bool auto=controls.AutoAssociate,station=controls.Station;string language=controls.Language;
            try{DisplaySettings.Load(ShipSettingsPath(id),controls);}catch(Exception e){VRage.Utils.MyLog.Default.WriteLine("[Marigold] Ship display: "+e.Message);}
            finally{controls.AutoAssociate=auto;controls.Station=station;controls.Language=language;}
        }
        private string DisplayProfileAction(string verb,string name)
        {
            if(verb=="ship-save"){var p=SelectedProfile();DisplaySettings.Save(ShipSettingsPath(p.Id),controls);return "Display saved for profile "+p.Number+". Global defaults unchanged.";}
            if(verb=="ship-load"){LoadShipSettings(SelectedProfile().Id);RefreshReference("Ship display loaded.");return comparisonReport;}
            string path=Path.Combine(DataRoot,"presets",SafeName(name)+".txt");
            if(verb=="preset-save")DisplaySettings.Save(path,controls);
            else{if(!File.Exists(path))return "Preset not found.";bool auto=controls.AutoAssociate,station=controls.Station;string language=controls.Language;DisplaySettings.Load(path,controls);controls.AutoAssociate=auto;controls.Station=station;controls.Language=language;RefreshReference("Preset loaded.");}
            return "Preset "+name+(verb=="preset-save"?" saved.":" loaded.");
        }
        private ProfileStore Revisions(ShipProfile p){return new ProfileStore(Path.Combine(DataRoot,"revisions",p.Id));}
        private string RevisionAction(string verb,string name)
        {
            var p=SelectedProfile();var store=Revisions(p);
            if(verb=="rename")
            {profileStore.Put(new ShipProfile{Id=p.Id,Name=SafeName(name),GridSize=p.GridSize,Anchor=p.Anchor,Blocks=p.Blocks,Station=p.Station});return "Profile renamed; number retained.";}
            if(verb=="save")
            {
                if(p.Blocks==null)return "No baseline to save. Capture or import one first.";
                store.Put(new ShipProfile{Id=Guid.NewGuid().ToString("N"),Name=SafeName(name),GridSize=p.GridSize,Anchor=p.Anchor,Blocks=p.Blocks});
                return "Named baseline saved. It will not be changed by capture/import. Up to 16 revisions per profile.";
            }
            var revision=store.Find(name);
            if(verb=="delete"){store.Delete(revision.Id);return "Named revision deleted; active baseline unchanged.";}
            var a=p.Anchor;var b=revision.Anchor;
            if(!a.Position.Equals(b.Position) || !a.Right.Equals(b.Right) || !a.Up.Equals(b.Up) || !a.Backward.Equals(b.Backward))return "This revision uses a different saved anchor. Restore through a verified alignment workflow; automatic revision loading is refused.";
            // Preserve the previous active layout before replacing it, unless already archived.
            bool archived=false;foreach(var existing in store.Profiles)if(ObservationCache.Same(existing.Blocks,p.Blocks)){archived=true;break;}
            if(p.Blocks!=null && !archived)store.Put(new ShipProfile{Id=Guid.NewGuid().ToString("N"),Name="Before restore "+DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss"),GridSize=p.GridSize,Anchor=p.Anchor,Blocks=p.Blocks});
            var updated=new ShipProfile{Id=p.Id,Name=p.Name,GridSize=p.GridSize,Anchor=p.Anchor,Blocks=revision.Blocks,Station=p.Station};
            Reference binding=references[grid];profileStore.Put(updated);BindProfile(updated,binding.Anchor);
            return "Loaded revision "+revision.Name+" as the active baseline. The named revision remains saved.";
        }
    }
}
