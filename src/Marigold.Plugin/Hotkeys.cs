using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Input;
namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private readonly string[] hotkeys=new string[3];
        private bool hotkeysLoaded;
        private static string HotkeyPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"Marigold","hotkeys.txt"); } }
        private static bool ParseHotkey(string text,out MyKeys key,out int modifiers)
        {
            key=MyKeys.None;modifiers=0;if(string.IsNullOrWhiteSpace(text))return true;
            var parts=text.Trim().Split('+');
            for(int i=0;i<parts.Length-1;i++)
            {
                string part=parts[i].Trim().ToLowerInvariant();int bit=part=="ctrl"?1:part=="alt"?2:part=="shift"?4:0;
                if(bit==0 || (modifiers&bit)!=0)return false;modifiers|=bit;
            }
            string last=parts[parts.Length-1].Trim();
            return !int.TryParse(last,out _) && Enum.TryParse(last,true,out key) && Enum.IsDefined(typeof(MyKeys),key) && key!=MyKeys.None && !last.ToLowerInvariant().Contains("shift") && !last.ToLowerInvariant().Contains("control") && !last.ToLowerInvariant().Contains("menu");
        }
        private void LoadHotkeys()
        {
            if(hotkeysLoaded)return;hotkeysLoaded=true;
            try { if(File.Exists(HotkeyPath)){var lines=File.ReadAllLines(HotkeyPath);if(lines.Length!=3)return;for(int i=0;i<3;i++){MyKeys key;int modifiers;if(ParseHotkey(lines[i],out key,out modifiers))hotkeys[i]=lines[i];}} }
            catch(Exception e){VRage.Utils.MyLog.Default.WriteLine("[Marigold] Shortcut load: "+e.Message);}
        }
        private string SaveHotkey(int action,string value)
        {
            LoadHotkeys();MyKeys key;int modifiers;
            if(!ParseHotkey(value,out key,out modifiers))return "Use a key name such as F6 or Ctrl+Shift+F6. Leave blank to unbind.";
            for(int i=0;i<3;i++)if(i!=action && key!=MyKeys.None){MyKeys other;int flags;ParseHotkey(hotkeys[i],out other,out flags);if(key==other && flags==modifiers)return "That shortcut is already assigned to another Marigold action.";}
            var next=(string[])hotkeys.Clone();next[action]=value.Trim();
            Directory.CreateDirectory(Path.GetDirectoryName(HotkeyPath));string temp=HotkeyPath+".tmp";
            try{File.WriteAllLines(temp,next);if(File.Exists(HotkeyPath))File.Replace(temp,HotkeyPath,HotkeyPath+".bak");else File.Move(temp,HotkeyPath);Array.Copy(next,hotkeys,3);}
            finally{if(File.Exists(temp))File.Delete(temp);}
            return "Shortcut saved. Game and other plugin bindings still apply; choose an unused combination.";
        }
        private void UpdateHotkeys()
        {
            LoadHotkeys();if(grid==null || panel!=null || MyAPIGateway.Gui==null || MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible || MyInput.Static==null)return;
            int flags=(MyInput.Static.IsAnyCtrlKeyPressed()?1:0)|(MyInput.Static.IsAnyAltKeyPressed()?2:0)|(MyInput.Static.IsAnyShiftKeyPressed()?4:0);
            for(int i=0;i<3;i++){MyKeys key;int modifiers;if(!ParseHotkey(hotkeys[i],out key,out modifiers) || key==MyKeys.None || modifiers!=flags || !MyInput.Static.IsNewKeyPressed(key))continue;
                string command;
                if(i==2)command="layout "+(controls.Expanded?"compact":"expanded");
                else{var values=(i==0?DisplayFilter.Categories:DisplayFilter.States).Split(' ');int index=Array.IndexOf(values,i==0?controls.Category:controls.StateFilter);command=(i==0?"isolate ":"filter ")+values[(index+1)%values.Length];}
                commandUtilities?.ShowMessage("Marigold",PanelCommand(command));
            }
        }
    }
}
