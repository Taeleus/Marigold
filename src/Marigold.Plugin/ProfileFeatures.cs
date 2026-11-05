using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private string DataRoot
        {
            get
            {
                return Path.GetDirectoryName(settingsPath);
            }
        }

        private ShipProfile SelectedProfile()
        {
            Reference binding;
            if (grid == null || !references.TryGetValue(grid, out binding))
                throw new InvalidOperationException("Load or capture a profile first.");
            return profileStore.Find(binding.ProfileId);
        }

        private static bool IsSafeName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "^[a-zA-Z0-9 _-]{1,40}$");
        }

        private static string SafeName(string value)
        {
            if (!IsSafeName(value))
                throw new InvalidOperationException("Use 1–40 letters, digits, spaces, hyphens or underscores.");
            return value.Trim();
        }

        private string ShipSettingsPath(string id)
        {
            return Path.Combine(DataRoot, "display-" + id + ".txt");
        }

        private string ActiveShipSettingsPath
        {
            get
            {
                return grid != null && references.TryGetValue(grid, out var binding)
                    ? ShipSettingsPath(binding.ProfileId) : null;
            }
        }

        private bool HasSavedShipLayout => File.Exists(ActiveShipSettingsPath);
        private bool UsesSavedShipLayout => DisplaySettings.UsesShipLayout(ActiveShipSettingsPath);

        private string[] SavedLayoutNames()
        {
            string directory = Path.Combine(DataRoot, "presets");
            if (!Directory.Exists(directory))
                return Array.Empty<string>();

            return Directory.EnumerateFiles(directory, "*.txt")
                .Where(path => string.Equals(Path.GetExtension(path), ".txt", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => IsSafeName(name) && name == name.Trim())
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private void LoadShipSettings(string id)
        {
            try
            {
                DisplaySettings.LoadShipLayout(settingsPath, ShipSettingsPath(id), controls);
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine("[Marigold] Ship display: " + e.Message);
            }
        }

        private string DisplayProfileAction(string verb, string name)
        {
            if (verb == "ship-save")
            {
                var p = SelectedProfile();
                string shipPath = ShipSettingsPath(p.Id);
                DisplaySettings.Save(shipPath, controls);
                SaveCustomBarLayouts(p);
                DisplaySettings.EnableShipLayout(shipPath);
                return "Saved this ship's layout. It will load on reconnect.";
            }

            if (verb == "ship-load" || verb == "ship-default")
            {
                bool useSaved = verb == "ship-load";
                DisplaySettings.SelectShipLayout(settingsPath, ShipSettingsPath(SelectedProfile().Id), controls, useSaved);
                if (useSaved)
                    ClearCustomBarLayouts();
                string message = useSaved ? "Saved ship layout loaded and selected for reconnect."
                    : "This ship will load the default display on reconnect.";
                RefreshReference(message);
                return message;
            }

            string path = Path.Combine(DataRoot, "presets", SafeName(name) + ".txt");
            if (verb == "preset-save")
            {
                DisplaySettings.Save(path, controls);
                return "Named layout saved.";
            }

            if (!File.Exists(path))
                return "Saved layout not found.";
            bool auto = controls.AutoAssociate, station = controls.Station;
            string language = controls.Language;
            DisplaySettings.Load(path, controls);
            controls.AutoAssociate = auto;
            controls.Station = station;
            controls.Language = language;
            RefreshReference("Named layout loaded.");
            return "Named layout loaded.";
        }

        private ProfileStore Revisions(ShipProfile p)
        {
            return new ProfileStore(Path.Combine(DataRoot, "revisions", p.Id));
        }

        private ProfileStore recoveryBindingStore;

        private ProfileStore RecoveryBindings()
        {
            return recoveryBindingStore ?? (recoveryBindingStore = new ProfileStore(Path.Combine(DataRoot, "recovery")));
        }

        private void ArchiveBaseline(ShipProfile p)
        {
            ReferenceHistory.Archive(Revisions(p), p);
        }

        private string RevisionAction(string verb, string name, string newName = null)
        {
            var p = SelectedProfile();
            var store = Revisions(p);
            if (verb == "rename")
            {
                var renamed = p.Copy();
                renamed.Name = SafeName(name);
                profileStore.Put(renamed);
                return "Profile renamed; number retained.";
            }

            if (verb == "save")
            {
                var saved = ReferenceHistory.SaveNamed(store, p, SafeName(name));
                var active = p.Copy();
                active.ReferenceId = saved.ReferenceId;
                active.ReferenceName = saved.ReferenceName;
                var remembered = RecoveryBindings();
                foreach (var item in remembered.Profiles)
                    if (item.Id == p.Id && item.ReferenceId == p.ReferenceId)
                    {
                        var changed = item.Copy();
                        changed.ReferenceId = active.ReferenceId;
                        remembered.Put(changed);
                        break;
                    }

                profileStore.Put(active);
                return "Saved version " + saved.Name + " is now identified as the active baseline.";
            }

            var revision = store.Find(name);
            if (verb == "rename-version")
            {
                var renamed = ReferenceHistory.Rename(store, revision, SafeName(newName));
                if (p.ReferenceId == revision.ReferenceId)
                {
                    var active = p.Copy();
                    active.ReferenceName = renamed.Name;
                    profileStore.Put(active);
                }

                return "Version renamed; baseline contents unchanged.";
            }

            if (verb == "replace")
            {
                var replacement = ReferenceHistory.Replace(store, revision, p);
                var active = p.Copy();
                active.ReferenceName = replacement.Name;
                profileStore.Put(active);
                return "Selected version replaced with the active baseline; its previous contents remain in Saved versions.";
            }

            if (verb == "delete")
            {
                store.Delete(revision.Id);
                return "Named revision deleted; active baseline unchanged.";
            }

            var updated = revision.Copy();
            updated.Id = p.Id;
            updated.Name = p.Name;
            updated.Station = p.Station;
            if (!ReferenceHistory.SameAnchor(p.Anchor, revision.Anchor))
                return StartRecovery(updated, true);
            ArchiveBaseline(p);
            Reference binding = references[grid];
            profileStore.Put(updated);
            BindProfile(updated, binding.Anchor);
            return "Loaded " + revision.Name + ". Previous baseline preserved in Saved versions.";
        }
    }
}
