using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Marigold
{
    internal sealed class ProfileStore
    {
        private const int Version=3,MaxProfiles=16,MaxBlocks=250000;
        private readonly string path;
        private List<ShipProfile> profiles=new List<ShipProfile>();
        public IList<ShipProfile> Profiles { get { return profiles.AsReadOnly(); } }
        public string Warning { get; private set; }
        private bool canRecover;
        private int nextNumber=1;
        public ProfileStore(string directory)
        {
            path=Path.Combine(directory,"profiles.bin");
            if(!File.Exists(path))
            {
                if(!File.Exists(path+".bak"))return;
                try { profiles=Read(path+".bak",out nextNumber);canRecover=true;Warning="Primary profile file is missing. Backup loaded read-only; use /mg recover."; }
                catch(Exception e) when(e is IOException || e is InvalidDataException || e is InvalidOperationException || e is UnauthorizedAccessException || e is ArgumentException)
                { Warning="Profile backup could not be loaded: "+e.Message+" Files preserved; profile writes disabled."; }
                return;
            }
            try { profiles=Read(path,out nextNumber); }
            catch(Exception e) when(e is IOException || e is InvalidDataException || e is InvalidOperationException || e is UnauthorizedAccessException || e is ArgumentException)
            {
                Warning="Profile file could not be loaded: "+e.Message;
                try { profiles=Read(path+".bak",out nextNumber);canRecover=true;Warning+=" Backup loaded read-only; use /mg recover to restore it."; }
                catch(Exception backup) when(backup is IOException || backup is InvalidDataException || backup is InvalidOperationException || backup is UnauthorizedAccessException || backup is ArgumentException)
                { Warning+=" No usable backup. Existing files were preserved; profile writes are disabled."; }
            }
        }
        public ShipProfile Find(string id)
        {
            int number;
            if(int.TryParse(id,out number))
            {foreach(var p in profiles)if(p.Number==number)return p;if(id.Length<8)throw new InvalidOperationException("Profile number not found. Use /mg profiles.");}
            if(string.IsNullOrWhiteSpace(id) || id.Length<8)throw new InvalidOperationException("Use the profile number from /mg profiles (old IDs still work).");
            ShipProfile match=null;
            foreach(var p in profiles)if(p.Id.StartsWith(id,StringComparison.OrdinalIgnoreCase))
            { if(match!=null)throw new InvalidOperationException("Ambiguous profile ID; use the full ID.");match=p; }
            if(match==null)throw new InvalidOperationException("Profile not found. Use /mg profiles.");
            return match;
        }
        public void Put(ShipProfile profile,bool updateStation=false)
        {
            var existing=profiles.Find(p=>p.Id==profile.Id);
            if(existing!=null && profile.Blocks==existing.Blocks && string.IsNullOrEmpty(profile.ReferenceId))
            {profile.ReferenceId=existing.ReferenceId;profile.ReferenceName=existing.ReferenceName;profile.Source=existing.Source;profile.BlueprintIdentity=existing.BlueprintIdentity;profile.CreatedUtcTicks=existing.CreatedUtcTicks;}
            if(profile.Blocks!=null && string.IsNullOrEmpty(profile.ReferenceId)){profile.ReferenceId=Guid.NewGuid().ToString("N");profile.CreatedUtcTicks=DateTime.UtcNow.Ticks;if(string.IsNullOrEmpty(profile.Source))profile.Source="capture";if(string.IsNullOrEmpty(profile.ReferenceName))profile.ReferenceName="Baseline "+DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");}
            profile.Number=existing==null?LowestAvailable(profiles):existing.Number;
            if(existing!=null && !updateStation)profile.Station=existing.Station;
            var next=new List<ShipProfile>(profiles);
            next.RemoveAll(p=>p.Id==profile.Id);next.Add(profile);
            Save(next);
        }
        private static int LowestAvailable(IList<ShipProfile> list)
        {
            var used=new HashSet<int>();foreach(var p in list)used.Add(p.Number);
            int number=1;while(used.Contains(number))number++;return number;
        }
        public void Delete(string id)
        { var next=new List<ShipProfile>(profiles);next.RemoveAll(p=>p.Id==id);Save(next); }
        public void Recover()
        {
            if(!canRecover)throw new InvalidOperationException("No backup recovery is pending.");
            // Preserve the unreadable original separately; never replace it with an empty database.
            int restoredNext;var recovered=Read(path+".bak",out restoredNext);
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            File.Copy(path+".bak",temp);
            try { if(File.Exists(path))File.Replace(temp,path,path+".unreadable-"+Guid.NewGuid().ToString("N"));else File.Move(temp,path); }
            finally { if(File.Exists(temp))File.Delete(temp); }
            profiles=recovered;nextNumber=restoredNext;Warning=null;canRecover=false;
        }
        private void Save(List<ShipProfile> next)
        {
            if(Warning!=null)throw new InvalidOperationException(Warning);
            Validate(next);
            int following=LowestAvailable(next);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
                {
                    using(var w=new BinaryWriter(stream,Encoding.UTF8,true))
                    {
                        w.Write(0x4D475046);w.Write(Version);w.Write(next.Count);w.Write(following);
                        foreach(var p in next)
                        {
                            w.Write(p.Number);w.Write(p.Station);Text(w,p.Id,32);Text(w,p.Name,1024);w.Write(p.GridSize);
                            Point(w,p.Anchor.Position);Point(w,p.Anchor.Right);Point(w,p.Anchor.Up);Point(w,p.Anchor.Backward);
                            Text(w,p.Anchor.Definition,2048);Text(w,p.Anchor.Name??"",1024);
                            Text(w,p.ReferenceId??"",32);Text(w,p.ReferenceName??"",1024);Text(w,p.Source??"",128);Text(w,p.BlueprintIdentity??"",2048);w.Write(p.CreatedUtcTicks);
                            w.Write(p.Blocks==null?-1:p.Blocks.Length);
                            if(p.Blocks!=null)foreach(var b in p.Blocks)
                            {
                                Point(w,b.Position);Point(w,b.Bounds.Min);Point(w,b.Bounds.Max);Text(w,b.Definition,2048);
                                w.Write(b.Forward);w.Write(b.Up);w.Write(b.HasFunctionalState);w.Write(b.IsFunctional);
                                w.Write(b.Integrity);w.Write(b.BuildIntegrity);w.Write(b.MaxIntegrity);
                            }
                        }
                    }
                    stream.Flush(true);
                }
                if(new FileInfo(temp).Length>64*1024*1024)throw new InvalidOperationException("Profile database exceeds 64 MB.");
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
                profiles=next;nextNumber=following;
            }
            finally { if(File.Exists(temp))File.Delete(temp); }
        }
        private static void Validate(List<ShipProfile> list)
        {
            if(list.Count>MaxProfiles)throw new InvalidOperationException("Limit: 16 profiles.");
            int total=0;var ids=new HashSet<string>();var numbers=new HashSet<int>();
            foreach(var p in list)
            {
                ProfileAlignment.Validate(p);
                Guid referenceId;if((!string.IsNullOrEmpty(p.ReferenceId) && !Guid.TryParseExact(p.ReferenceId,"N",out referenceId)) || p.CreatedUtcTicks<0 || p.CreatedUtcTicks>DateTime.MaxValue.Ticks)throw new InvalidOperationException("Invalid reference metadata.");
                if(p.Number<=0 || p.Number==int.MaxValue || !numbers.Add(p.Number))throw new InvalidOperationException("Invalid/duplicate profile number.");
                if(!ids.Add(p.Id))throw new InvalidOperationException("Duplicate profile ID.");
                total+=p.Blocks==null?0:p.Blocks.Length;
                if(total>MaxBlocks)throw new InvalidOperationException("Limit: 250,000 saved blocks.");
            }
        }
        private static List<ShipProfile> Read(string file,out int next)
        {
            next=1;
            if(new FileInfo(file).Length>64*1024*1024)throw new InvalidDataException("Profile file exceeds 64 MB.");
            using(var stream=File.OpenRead(file))using(var r=new BinaryReader(stream,Encoding.UTF8))
            {
                if(r.ReadInt32()!=0x4D475046)throw new InvalidDataException("Unsupported profile format.");
                int version=r.ReadInt32();if(version!=1 && version!=2 && version!=Version)throw new InvalidDataException("Unsupported profile version.");
                int count=r.ReadInt32(),total=0;if(count<0 || count>MaxProfiles)throw new InvalidDataException("Invalid profile count.");
                if(version>=2){next=r.ReadInt32();if(next<1)throw new InvalidDataException("Invalid profile counter.");}
                var list=new List<ShipProfile>();
                for(int i=0;i<count;i++)
                {
                    int number=version>=2?r.ReadInt32():i+1;bool station=version>=2 && r.ReadBoolean();
                    var p=new ShipProfile { Number=number,Station=station,Id=Text(r,32),Name=Text(r,1024),GridSize=r.ReadDouble() };
                    if(number<=0 || number==int.MaxValue)throw new InvalidDataException("Invalid profile number.");
                    next=Math.Max(next,number+1);
                    p.Anchor=new AnchorFrame { Position=Point(r),Right=Point(r),Up=Point(r),Backward=Point(r),Definition=Text(r,2048),Name=Text(r,1024) };
                    if(version>=3){p.ReferenceId=Text(r,32);p.ReferenceName=Text(r,1024);p.Source=Text(r,128);p.BlueprintIdentity=Text(r,2048);p.CreatedUtcTicks=r.ReadInt64();}
                    else if(p.ReferenceId.Length==0){p.ReferenceId=p.Id;p.ReferenceName="Legacy baseline";p.Source="legacy";}
                    int blocks=r.ReadInt32();if(blocks< -1 || blocks>MaxBlocks || (total+=Math.Max(0,blocks))>MaxBlocks)throw new InvalidDataException("Invalid block count.");
                    if(blocks>=0)
                    {
                        p.Blocks=new BlockSample[blocks];
                        for(int b=0;b<blocks;b++)p.Blocks[b]=new BlockSample { Position=Point(r),Bounds=new Box3(Point(r),Point(r)),Definition=Text(r,2048),Forward=r.ReadInt32(),Up=r.ReadInt32(),HasFunctionalState=r.ReadBoolean(),IsFunctional=r.ReadBoolean(),Integrity=r.ReadDouble(),BuildIntegrity=r.ReadDouble(),MaxIntegrity=r.ReadDouble() };
                    }
                    list.Add(p);
                }
                if(stream.Position!=stream.Length)throw new InvalidDataException("Unexpected trailing profile data.");
                Validate(list);return list;
            }
        }
        private static void Point(BinaryWriter w,Point3 p){w.Write(p.X);w.Write(p.Y);w.Write(p.Z);}
        private static Point3 Point(BinaryReader r){return new Point3(r.ReadDouble(),r.ReadDouble(),r.ReadDouble());}
        private static void Text(BinaryWriter w,string text,int max)
        { var bytes=Encoding.UTF8.GetBytes(text);if(bytes.Length>max)throw new InvalidOperationException("Profile text is too long.");w.Write(bytes.Length);w.Write(bytes); }
        private static string Text(BinaryReader r,int max)
        { int length=r.ReadInt32();if(length<0 || length>max)throw new InvalidDataException("Invalid profile text length.");var bytes=r.ReadBytes(length);if(bytes.Length!=length)throw new EndOfStreamException();return new UTF8Encoding(false,true).GetString(bytes); }
    }
}
