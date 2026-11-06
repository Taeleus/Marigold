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
                CheckField("Show Marigold HUD", owner.controls.Visible, visible => Run(visible ? "show" : "hide"));

                Heading("Inspect damage");
                string[] states = DisplayFilter.States.Split(' ');
                var labels = Array.ConvertAll(states, state => state == "all" ? "All states"
                    : state == "faults" ? "Damage and missing" : state == "broken" ? "Non-functional"
                    : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(state));
                ChoiceField("Show these block states", labels, Math.Max(0, Array.IndexOf(states, owner.controls.StateFilter)),
                    selected => Run("filter " + states[selected]));
                ActionRow("Systems and filters", () => Switch(5), "Show damage report", () => Run("report"));

                Heading("View and zoom");
                var spin = CheckField("Rotate the schematic automatically", owner.controls.Spin, spinning =>
                {
                    if (owner.controls.Spin != spinning)
                        Run("spin");
                });
                ActionRow("Left side", () => SetView("left"), "Right side", () => SetView("right"));
                ActionRow("Top", () => SetView("top"), "Bottom", () => SetView("bottom"));
                ActionRow("Zoom out", () => Run("out"), "Zoom in", () => Run("in"));
                ActionControl("Reset rotating view", () => SetView("reset"));

                if (!advancedControls)
                    return;
                Heading("Fine rotation");
                Note("Fine adjustments pause automatic rotation.");
                ActionRow("Turn left", () => RotateView(-Math.PI / 12, 0), "Turn right", () => RotateView(Math.PI / 12, 0));
                ActionRow("Tilt up", () => RotateView(0, -Math.PI / 12), "Tilt down", () => RotateView(0, Math.PI / 12));

                Heading("Diagnostics");
                ActionControl("Show performance statistics", () => Run("stats"));
                Heading("Damage marker pages");
                Note("Use these when individual markers are spread over several pages. Health bars still count the whole baseline.");
                ActionRow("Previous page", () => Run("page prev"), "Next page", () => Run("page next"));

                void SetView(string command)
                {
                    Run(command);
                    spin.IsChecked = owner.controls.Spin;
                }

                void RotateView(double yaw, double pitch)
                {
                    owner.controls.RotateView(yaw, pitch);
                    spin.IsChecked = owner.controls.Spin;
                }
            }
        }
    }
}
