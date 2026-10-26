using System;
using System.IO;
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

        private static string SafeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value ?? "", "^[a-zA-Z0-9 _-]{1,40}$"))
                throw new InvalidOperationException("Use 1–40 letters, digits, spaces, hyphens or underscores.");
            return value.Trim();
        }

        private string ShipSettingsPath(string id)
        {
            return Path.Combine(DataRoot, "display-" + id + ".txt");
        }

        private void LoadShipSettings(string id)
        {
            bool auto = controls.AutoAssociate, station = controls.Station;
            string language = controls.Language;
            try
            {
                DisplaySettings.Load(ShipSettingsPath(id), controls);
            }
            catch (Exception e)
            {
                VRage.Utils.MyLog.Default.WriteLine("[Marigold] Ship display: " + e.Message);
            }
            finally
            {
                controls.AutoAssociate = auto;
                controls.Station = station;
                controls.Language = language;
            }
        }

        private string DisplayProfileAction(string verb, string name)
        {
            if (verb == "ship-save")
            {
                var p = SelectedProfile();
                DisplaySettings.Save(ShipSettingsPath(p.Id), controls);
                return "Display saved for profile " + p.Number + ". Global defaults unchanged.";
            }

            if (verb == "ship-load")
            {
                LoadShipSettings(SelectedProfile().Id);
                RefreshReference("Ship display loaded.");
                return comparisonReport;
            }

            string path = Path.Combine(DataRoot, "presets", SafeName(name) + ".txt");
            if (verb == "preset-save")
                DisplaySettings.Save(path, controls);
            else
            {
                if (!File.Exists(path))
                    return "Preset not found.";
                bool auto = controls.AutoAssociate, station = controls.Station;
                string language = controls.Language;
                DisplaySettings.Load(path, controls);
                controls.AutoAssociate = auto;
                controls.Station = station;
                controls.Language = language;
                RefreshReference("Preset loaded.");
            }

            return "Preset " + name + (verb == "preset-save" ? " saved." : " loaded.");
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
