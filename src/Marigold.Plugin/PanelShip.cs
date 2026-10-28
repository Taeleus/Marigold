using System;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private void Display()
            {
                if (owner.grid == null)
                {
                    Intro("Sit in a cockpit to inspect a ship. You can still change display preferences here.");
                }
                else
                    Intro(owner.grid.DisplayName);
                Note("See the ship's condition, focus on a system or choose a viewing angle.");
                CheckField("Show Marigold HUD", owner.controls.Visible, visible => Run(visible ? "show" : "hide"));
                CheckField("Rotate the schematic automatically", owner.controls.Spin, spinning =>
                {
                    if (owner.controls.Spin != spinning)
                        Run("spin");
                });
                Heading("Inspect your ship");
                ActionRow("View and zoom", () => Switch(3), "Systems and filters", () => Switch(5));
                ActionRow("Damage report", () => Switch(14), "Manage baseline", () => Switch(1));
                Heading("Block states");
                string[] states = DisplayFilter.States.Split(' ');
                var labels = Array.ConvertAll(states, state => state == "all" ? "All states"
                    : state == "faults" ? "Damage and missing" : state == "broken" ? "Non-functional"
                    : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(state));
                ChoiceField("Show these block states", labels, Math.Max(0, Array.IndexOf(states, owner.controls.StateFilter)),
                    selected => Run("filter " + states[selected]));
            }

            private void Views()
            {
                Intro("Views follow the saved cockpit orientation. Changing the view never moves the baseline.");
                Heading("Look from a side");
                ActionRow("Left side", () => Run("left"), "Right side", () => Run("right"));
                ActionRow("Top", () => Run("top"), "Bottom", () => Run("bottom"));
                Heading("Zoom");
                ActionRow("Zoom out", () => Run("out"), "Zoom in", () => Run("in"));
                Heading("Fine rotation");
                Note("Fine adjustments pause automatic rotation.");
                ActionRow("Turn left", () => owner.controls.RotateView(-Math.PI / 12, 0),
                    "Turn right", () => owner.controls.RotateView(Math.PI / 12, 0));
                ActionRow("Tilt up", () => owner.controls.RotateView(0, -Math.PI / 12),
                    "Tilt down", () => owner.controls.RotateView(0, Math.PI / 12));
                ActionRow("Reset rotating view", () => Run("reset"));
            }

            private void Customize()
            {
                Intro("Choose what you want to change. Display settings apply immediately; Save display keeps them.");
                ActionRow("Ship display layout", () => Switch(4), "State colors", () => Switch(16));
                ActionRow("Health bars", () => Switch(8), "Systems and filters", () => Switch(5));
                ActionRow("Preferences", () => Switch(12), "Save and share", () => Switch(13));
            }

            private void Diagnostics()
            {
                Intro("Read the damage report or inspect Marigold's rendering timings.");
                PrimaryAction("Show damage report", () => Run("report"));
                ActionRow("Show performance statistics", () => Run("stats"));
                Heading("Damage marker pages");
                Note("Use these when individual markers are spread over several pages. Health bars still count the whole baseline.");
                ActionRow("Previous page", () => Run("page prev"), "Next page", () => Run("page next"));
            }
        }
    }
}
