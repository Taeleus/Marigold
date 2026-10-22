using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;
namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private Panel panel;
        private void OpenPanel()
        {
            if(panel!=null)return;
            var opened=new Panel(this);panel=opened;opened.Closed+=(screen,unloading)=>{if(ReferenceEquals(panel,opened))panel=null;};MyGuiSandbox.AddScreen(opened);
        }
        private void ClosePanel(){var previous=panel;panel=null;previous?.CloseScreen();}
        private string PanelCommand(string command)
        {
            try
            {
                string before=controls.GeometryOptions;string response=controls.Execute("/mg "+command);
                if(controls.ReferenceAction=="ui"){OpenPanel();response="Marigold controls opened.";}
                else if(controls.ReferenceAction!=null)response=ReferenceCommand(controls.ReferenceAction);
                if(controls.StatsRequested)response=Statistics();
                if(before!=controls.GeometryOptions){generation++;dirty=true;cooldown=0;}
                return response;
            }
            catch(Exception e){return "Could not complete action: "+e.Message;}
        }
        private sealed class Panel : MyGuiScreenBase
        {
            private readonly SchematicOverlay owner;
            private int blueprintUiRevision=-1;
            private bool refreshBlueprint;
            public void NotifyBlueprint(string message){Status(message);refreshBlueprint=true;}
            private int captureShortcut=-1;private MyGuiControlTextbox shortcutBox;
            private int tab;private string status="",path="",confirm;
            private MyGuiControlMultilineText output;
            private MyGuiControlTextbox pathBox;
            private string[] localFiles=new string[0];private Task<string[]> catalog;
            public Panel(SchematicOverlay owner):base(new Vector2(.5f,.5f),new Vector4(.03f,.06f,.08f,1),new Vector2(.92f,.92f),backgroundTransition:1f,guiTransition:1f)
            {this.owner=owner;CanHideOthers=false;RecreateControls(true);}
            public override string GetFriendlyName(){return "MarigoldControls";}
            private string T(string text){return UiText.Get(owner.controls.Language,text);}
            private void Label(float x,float y,string text)
            {Controls.Add(new MyGuiControlLabel(new Vector2(x,y),text:T(text),textScale:.65f,maxWidth:.78f,isAutoEllipsisEnabled:true));}
            private void Button(float x,float y,string text,Action action)
            {Controls.Add(new MyGuiControlButton(new Vector2(x,y),size:new Vector2(.19f,.042f),text:new StringBuilder(T(text)),textScale:.62f,onButtonClick:b=>{try{action();}catch(Exception e){Status(e.Message);}}));}
            private MyGuiControlCombobox Combo(float x,float y,string[] items,int selected,Action<int> change=null)
            {
                var combo=new MyGuiControlCombobox(new Vector2(x,y),new Vector2(.34f,.04f),openAreaItemsCount:6);
                for(int i=0;i<items.Length;i++)combo.AddItem(i,T(items[i]),sort:false);
                if(items.Length>0)combo.SelectItemByKey(Math.Max(0,Math.Min(selected,items.Length-1)),false);
                if(change!=null)combo.ItemSelected+=()=>change((int)combo.GetSelectedKey());Controls.Add(combo);return combo;
            }
            public void NotifyStatus(string text){Status(text);}
            private void Status(string text){status=text;if(output!=null){output.Clear();output.AppendText(text);}}
            private void Run(string command,bool destructive=false)
            {
                if(destructive && confirm!=command){confirm=command;Status(T("Click the same action again to confirm. No changes yet."));return;}
                confirm=null;Status(owner.PanelCommand(command));
            }
            private void Switch(int next){if(pathBox!=null)path=pathBox.Text;tab=next;confirm=null;RecreateControls(false);}
            public override void RecreateControls(bool constructor)
            {
                base.RecreateControls(constructor);pathBox=null;captureShortcut=-1;shortcutBox=null;
                Label(-.4f,-.40f,"Marigold");
                Button(-.29f,-.33f,"Display",()=>Switch(0));Button(-.08f,-.33f,"Profiles",()=>Switch(1));Button(.13f,-.33f,"Blueprints",()=>Switch(2));Button(.34f,-.33f,"Views",()=>Switch(3));
                Button(-.31f,.40f,"Appearance",()=>Switch(4));Button(-.10f,.40f,"Systems",()=>Switch(5));Button(.11f,.40f,"Library",()=>Switch(6));Button(.33f,.40f,"Close",()=>CloseScreen());
                output=new MyGuiControlMultilineText(new Vector2(0,.29f),new Vector2(.8f,.14f),textScale:.60f,drawScrollbarH:false,contents:new StringBuilder(status));Controls.Add(output);
                if(tab==0)Display();else if(tab==1)Profiles();else if(tab==2)Blueprints();else if(tab==3)Views();else if(tab==4)Appearance();else if(tab==5)Systems();else if(tab==6)Library();else Alignment();
            }
            private void Choice(float x,float y,string label,string values,string selected,string command)
            {Label(x-.17f,y-.032f,label);var items=values.Split(' ');Combo(x,y,items,Array.IndexOf(items,selected),i=>Run(command+" "+items[i]));}
            private void Display()
            {
                var c=owner.controls;
                Label(-.38f,-.22f,"Damage comparison always on");Choice(.21f,-.22f,"System",DisplayFilter.Categories,c.Category,"isolate");
                Choice(-.21f,-.13f,"Markers",DisplayFilter.States,c.StateFilter,"filter");Choice(.21f,-.13f,"Detail","low medium high",c.DetailName,"detail");
                Choice(-.21f,-.04f,"Layout","compact expanded",c.Expanded?"expanded":"compact","layout");Choice(.21f,-.04f,"Station","off on",c.Station?"on":"off","station");
                Choice(-.21f,.05f,"Auto match","off on",c.AutoAssociate?"on":"off","auto");
                Label(.04f,.018f,"Language");Combo(.21f,.05f,new[]{"English","EspaÃ±ol"},c.Language=="es"?1:0,i=>{Run("language "+(i==1?"es":"en"));RecreateControls(false);});
                Button(-.31f,.12f,c.Visible?"Hide":"Show",()=>{Run(c.Visible?"hide":"show");RecreateControls(false);});Button(-.10f,.12f,"Spin",()=>Run("spin"));Button(.11f,.12f,"Reset view",()=>Run("reset"));Button(.32f,.12f,"Save settings",()=>Run("settings save"));
                Button(-.31f,.18f,"Top",()=>Run("top"));Button(-.10f,.18f,"Bottom",()=>Run("bottom"));Button(.11f,.18f,"Zoom in",()=>Run("in"));Button(.32f,.18f,"Zoom out",()=>Run("out"));

            }
            private void Views()
            {
                Button(-.3f,-.2f,"Left",()=>Run("left"));Button(-.1f,-.2f,"Right",()=>Run("right"));Button(.1f,-.2f,"Top",()=>Run("top"));Button(.3f,-.2f,"Bottom",()=>Run("bottom"));
                Button(-.3f,-.1f,"Spin",()=>Run("spin"));Button(-.1f,-.1f,"Reset view",()=>Run("reset"));Button(.1f,-.1f,"Zoom in",()=>Run("in"));Button(.3f,-.1f,"Zoom out",()=>Run("out"));
                Button(-.3f,0,"Previous page",()=>Run("page prev"));Button(-.1f,0,"Next page",()=>Run("page next"));Button(.1f,0,"Report",()=>Run("report"));Button(.3f,0,"Statistics",()=>Run("stats"));
                Label(-.39f,.05f,"Shortcuts (blank = unbound; e.g. Ctrl+Shift+F6)");
                owner.LoadHotkeys();var action=Combo(-.20f,.10f,new[]{"Cycle system","Cycle markers","Toggle expanded","Show / hide","Open panel"},0);
                var binding=new MyGuiControlTextbox(new Vector2(.12f,.10f),owner.hotkeys[0]??"",maxLength:80,textScale:.6f){Size=new Vector2(.25f,.04f)};Controls.Add(binding);
                shortcutBox=binding;
                Button(-.31f,.16f,"Press a key",()=>{captureShortcut=(int)action.GetSelectedKey();Status("Press your shortcut, including modifiers. Escape cancels.");});
                action.ItemSelected+=()=>binding.Text=owner.hotkeys[(int)action.GetSelectedKey()]??"";
                Button(.32f,.16f,"Save shortcut",()=>Status(owner.SaveHotkey((int)action.GetSelectedKey(),binding.Text)));
                Label(-.39f,.18f,"Live preview appears at the middle-left edge.");
            }
            private void Appearance()
            {
                Label(-.39f,-.26f,"HUD values: 0–1; size: 0.12–0.7; colors: seven RRGGBB values");
                var fields=new[]{"x","y","size","background","outline","fill","pulse","colors"};
                var choice=Combo(-.20f,-.20f,fields,0);
                var value=new MyGuiControlTextbox(new Vector2(.17f,-.20f),owner.controls.Appearance.Encode().Split(';')[0],maxLength:100,textScale:.60f){Size=new Vector2(.34f,.04f)};Controls.Add(value);
                choice.ItemSelected+=()=>value.Text=owner.controls.Appearance.Encode().Split(';')[(int)choice.GetSelectedKey()];
                Button(-.31f,-.12f,"Apply value",()=>{owner.controls.Appearance.Set(fields[(int)choice.GetSelectedKey()],value.Text);Status("Applied. Save globally or for this ship to keep it.");});
                Button(-.10f,-.12f,"Reset HUD",()=>{owner.controls.Appearance=new HudAppearance();RecreateControls(false);});
                Button(.11f,-.12f,"Turn left",()=>owner.controls.RotateView(-Math.PI/12,0));Button(.32f,-.12f,"Turn right",()=>owner.controls.RotateView(Math.PI/12,0));
                Button(-.31f,-.04f,"Tilt up",()=>owner.controls.RotateView(0,-Math.PI/12));Button(-.10f,-.04f,"Tilt down",()=>owner.controls.RotateView(0,Math.PI/12));
                Button(.11f,-.04f,"Save global",()=>Run("settings save"));Button(.32f,-.04f,"Save ship",()=>Status(owner.DisplayProfileAction("ship-save",null)));
                Label(-.39f,.025f,"Preset name (letters, numbers, spaces, - and _)");
                var name=new MyGuiControlTextbox(new Vector2(-.18f,.08f),"My preset",maxLength:40,textScale:.65f){Size=new Vector2(.4f,.04f)};Controls.Add(name);
                Button(.11f,.08f,"Save preset",()=>Status(owner.DisplayProfileAction("preset-save",name.Text)));Button(.32f,.08f,"Load preset",()=>Status(owner.DisplayProfileAction("preset-load",name.Text)));
                Button(-.31f,.16f,"Load ship",()=>Status(owner.DisplayProfileAction("ship-load",null)));Button(-.10f,.16f,"Load global",()=>Run("settings load"));
                Label(.01f,.16f,"Colors: hull, missing, damage, replaced, added, unknown, broken");
            }
            private void Systems()
            {
                Label(-.39f,-.25f,"Choose several systems to display together");
                var items=DisplayFilter.Categories.Split(' ');var choice=Combo(0,-.17f,items,1);
                Action<bool> toggle=add=>{string item=items[(int)choice.GetSelectedKey()];if(item=="all"){Run(add?"isolate all":"isolate none");return;}var selected=new List<string>(owner.controls.Category=="all"?items:owner.controls.Category.Split(','));selected.Remove("all");selected.Remove("none");if(add && !selected.Contains(item))selected.Add(item);if(!add)selected.Remove(item);Run("isolate "+(selected.Count==0?"none":string.Join(",",selected)));};
                Button(-.31f,-.08f,"Add system",()=>toggle(true));Button(-.10f,-.08f,"Remove system",()=>toggle(false));Button(.11f,-.08f,"All systems",()=>Run("isolate all"));Button(.32f,-.08f,"No systems",()=>Run("isolate none"));
                Button(-.31f,.02f,"Damage overview",()=>{Run("isolate all");Run("filter faults");});Button(-.10f,.02f,"Propulsion",()=>Run("isolate thrusters,power,gas,control"));Button(.11f,.02f,"Conveyors",()=>Run("isolate conveyors,cargo"));Button(.32f,.02f,"Show all states",()=>Run("filter all"));
                var style=Combo(-.16f,.12f,new[]{"hidden","wireframe","transparent","highlighted"},1);
                Button(.21f,.12f,"Apply style",()=>{int index=(int)choice.GetSelectedKey();var values=owner.controls.SystemStyles.ToCharArray();if(index==0){for(int i=1;i<values.Length;i++)values[i]=(char)('0'+style.GetSelectedKey());}else values[index]=(char)('0'+style.GetSelectedKey());owner.controls.SystemStyles=new string(values);owner.RefreshReference("System presentation updated; damage markers retain their state colors.");Status(owner.comparisonReport);});
                Label(-.39f,.19f,"Styles affect hull outlines; station uses visibility. Save presets in Appearance.");
            }
            private void Library()
            {
                ShipProfile profile;try{profile=owner.SelectedProfile();}catch(Exception e){Label(-.39f,-.20f,e.Message);return;}
                Label(-.39f,-.26f,"Profile "+profile.Number+": "+profile.Name);
                var name=new MyGuiControlTextbox(new Vector2(-.15f,-.19f),"Pre-combat",maxLength:40,textScale:.65f){Size=new Vector2(.45f,.04f)};Controls.Add(name);
                Button(.12f,-.19f,"Save revision",()=>{Status(owner.RevisionAction("save",name.Text));RecreateControls(false);});Button(.33f,-.19f,"Rename profile",()=>{Status(owner.RevisionAction("rename",name.Text));RecreateControls(false);});
                var revisions=new List<ShipProfile>(owner.Revisions(profile).Profiles);var names=new List<string>();foreach(var r in revisions)names.Add(r.Number+": "+r.Name);
                Label(-.39f,-.10f,"Named baselines (capture/import does not change these)");var selected=Combo(0,-.04f,names.ToArray(),0);
                Action<string> revision=verb=>{int i=(int)selected.GetSelectedKey();if(i<0 || i>=revisions.Count){Status("Save or select a revision first.");return;}string key=verb+revisions[i].Id;if(confirm!=key){confirm=key;Status("Click again to confirm "+verb+" of "+revisions[i].Name+".");return;}confirm=null;Status(owner.RevisionAction(verb,revisions[i].Id));RecreateControls(false);};
                Button(-.21f,.06f,"Load revision",()=>revision("load"));Button(0,.06f,"Delete revision",()=>revision("delete"));Button(.21f,.06f,"Refresh",()=>RecreateControls(false));
                Label(-.39f,.15f,"Loading preserves the previous baseline as a revision when needed.");
            }
            private void Alignment()
            {
                Label(-.39f,-.25f,"Recovery preview: adjust translation in live-grid cells");
                Label(-.39f,-.18f,"Use Force apply in Profiles first. Baseline stays unchanged.");
                Button(-.21f,-.08f,"X -",()=>Status(owner.RecoveryCommand("move x-")));Button(0,-.08f,"Y -",()=>Status(owner.RecoveryCommand("move y-")));Button(.21f,-.08f,"Z -",()=>Status(owner.RecoveryCommand("move z-")));
                Button(-.21f,0,"X +",()=>Status(owner.RecoveryCommand("move x+")));Button(0,0,"Y +",()=>Status(owner.RecoveryCommand("move y+")));Button(.21f,0,"Z +",()=>Status(owner.RecoveryCommand("move z+")));
                Button(-.21f,.10f,"Next alignment",()=>Run("recovery next"));Button(0,.10f,"Accept recovery",()=>Run("recovery accept",true));Button(.21f,.10f,"Cancel",()=>Run("recovery cancel"));
                Label(-.39f,.18f,"At least 3 exact block matches are required to accept.");
            }
            private void Profiles()
            {
                var profiles=new List<ShipProfile>(owner.profileStore.Profiles);var names=new List<string>();foreach(var p in profiles)names.Add(p.Name+" ["+p.Number.ToString()+"]");
                Label(-.39f,-.25f,"Saved profiles");var choice=Combo(0,-.19f,names.ToArray(),0);
                Func<string> id=()=>{int i=(int)choice.GetSelectedKey();if(i<0 || i>=profiles.Count)throw new InvalidOperationException("Select a saved profile first.");return profiles[i].Id;};
                Button(-.31f,-.10f,"Load profile",()=>Run("profile load "+id()));Button(-.10f,-.10f,"Detach",()=>Run("profile detach"));Button(.11f,-.10f,"Delete",()=>Run("profile delete "+id(),true));Button(.32f,-.10f,"Refresh",()=>RecreateControls(false));
                Button(-.31f,-.02f,"Set anchor",()=>Run("anchor"));Button(-.10f,-.02f,"Capture",()=>Run("capture",true));Button(.11f,-.02f,"Clear baseline",()=>Run("clear",true));Button(.32f,-.02f,"Report",()=>Run("report"));
                Button(-.31f,.06f,"Suggest",()=>Run("profile suggest"));Button(-.10f,.06f,"Auto status",()=>Run("auto status"));Button(.11f,.06f,"Retry auto",()=>Run("auto retry"));Button(.32f,.06f,"Recover backup",()=>Run("recover",true));
                Button(0,.20f,"Alignment tools",()=>Switch(7));
                Button(-.31f,.14f,"Force apply",()=>Run("profile force "+id()));Button(-.10f,.14f,"Next alignment",()=>Run("recovery next"));Button(.11f,.14f,"Accept recovery",()=>Run("recovery accept",true));Button(.32f,.14f,"Cancel recovery",()=>Run("recovery cancel"));
            }
            private void Blueprints()
            {
                Label(-.39f,-.26f,"1. Blueprint source");
                Button(.10f,-.26f,"Workshop cache",()=>
                {
                    if(catalog!=null)return;
                    string content=VRage.FileSystem.MyFileSystem.ContentPath;
                    string root=Path.Combine(new DirectoryInfo(content).Parent.Parent.Parent.FullName,"workshop","content","244850");
                    catalog=Task.Run(()=>BlueprintCatalog.Find(root));Status("Reading locally cached Workshop XML blueprints. Archives/PB-only entries are not listed.");
                });
                var names=new string[localFiles.Length];for(int i=0;i<names.Length;i++)names[i]=localFiles[i].StartsWith("cloud:")?localFiles[i].Substring(6):localFiles[i].Replace(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"SpaceEngineers","Blueprints","local")+Path.DirectorySeparatorChar,"");
                var files=Combo(-.12f,-.20f,names,0,i=>{path=localFiles[i];if(pathBox!=null)pathBox.Text=path;});
                Button(.32f,-.20f,"Local folders",()=>
                {
                    if(catalog!=null)return;
                    catalog=Task.Run(()=>BlueprintCatalog.Find(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"SpaceEngineers","Blueprints","local")));
                    Status(T("Reading local blueprint list..."));
                });
                Button(.32f,-.26f,"Cloud",()=>{localFiles=CloudBlueprints.List();if(localFiles.Length>0)path=localFiles[0];Status(localFiles.Length+" cloud blueprints (XML). Select one and Load.");RecreateControls(false);});
                pathBox=new MyGuiControlTextbox(new Vector2(-.08f,-.13f),path,maxLength:2048,textScale:.65f){Size=new Vector2(.60f,.04f)};Controls.Add(pathBox);
                Button(.32f,-.13f,"Load",()=>{path=pathBox.Text;Run("blueprint load \""+path+"\"");});
                var grids=new List<string>();if(owner.blueprint!=null)foreach(var g in owner.blueprint.Grids)grids.Add(g.Name);
                Label(-.39f,-.075f,"2. Grid (prepares automatically)");var gridChoice=Combo(-.12f,-.035f,grids.ToArray(),owner.blueprintGrid,i=>Run("blueprint grid "+(i+1)));
                Button(.32f,-.035f,"Prepare grid",()=>Run("blueprint grid "+(gridChoice.GetSelectedKey()+1)));
                var anchors=new List<string>();foreach(var a in owner.blueprintAnchors)anchors.Add(a.Name);
                Label(-.39f,.025f,"3. Match this cockpit to your seated cockpit");var anchorChoice=Combo(-.12f,.065f,anchors.ToArray(),0);
                Button(.32f,.065f,"Omitted blocks",()=>Status(owner.OmittedReport()));
                Button(-.31f,.15f,"Preview",()=>Run("blueprint preview "+(anchorChoice.GetSelectedKey()+1)));
                Button(-.10f,.15f,"Save profile",()=>Run("blueprint accept",true));Button(.11f,.15f,"Cancel preview",()=>Run("blueprint cancel"));Button(.32f,.15f,"Report",()=>Run("report"));
            }
            public override bool Draw(){bool visible=base.Draw();if(visible)owner.DrawSafely();return visible;}
            public override bool Update(bool hasFocus)
            {
                if(hasFocus && captureShortcut>=0 && VRage.Input.MyInput.Static!=null)
                {
                    var input=VRage.Input.MyInput.Static;
                    foreach(VRage.Input.MyKeys key in Enum.GetValues(typeof(VRage.Input.MyKeys)))
                    {
                        string name=key.ToString();if(key==VRage.Input.MyKeys.None || name.Contains("Shift") || name.Contains("Control") || name.Contains("Alt") || name.Contains("Menu") || !input.IsNewKeyPressed(key))continue;
                        if(key==VRage.Input.MyKeys.Escape){captureShortcut=-1;Status("Shortcut capture canceled.");break;}
                        string value=(input.IsAnyCtrlKeyPressed()?"Ctrl+":"")+(input.IsAnyAltKeyPressed()?"Alt+":"")+(input.IsAnyShiftKeyPressed()?"Shift+":"")+name;
                        Status(owner.SaveHotkey(captureShortcut,value));if(shortcutBox!=null)shortcutBox.Text=value;captureShortcut=-1;break;
                    }
                }
                if(catalog!=null && catalog.IsCompleted)
                {
                    try{localFiles=catalog.GetAwaiter().GetResult();if(string.IsNullOrWhiteSpace(path) && localFiles.Length>0)path=localFiles[0];Status(localFiles.Length+" "+T("local blueprints found."));}catch(Exception e){Status(e.Message);}catalog=null;
                    if(tab==2){RecreateControls(false);}
                }
                if(tab==2 && (refreshBlueprint || blueprintUiRevision!=owner.blueprintRevision))
                {refreshBlueprint=false;blueprintUiRevision=owner.blueprintRevision;if(pathBox!=null)path=pathBox.Text;Status(owner.blueprintStatus);RecreateControls(false);}
                return base.Update(hasFocus);
            }
        }
    }
}
