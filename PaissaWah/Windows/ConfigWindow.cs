using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace PaissaWah.Windows
{
    public class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration;

        public ConfigWindow(Plugin plugin) : base("A Wonderful Configuration Window###With a constant ID")
        {
            Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoScrollWithMouse;

            Size = new Vector2(232, 90); // Adjusted size
            SizeCondition = ImGuiCond.Always;

            Configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public override void Draw()
        {
            // Auto-Download Interval Configuration
            var intervalHours = Configuration.DownloadIntervalHours;
            ImGui.Text("Auto-Download Interval (Hours):");
            if (ImGui.InputInt("Interval", ref intervalHours))
            {
                Configuration.DownloadIntervalHours = intervalHours;
                Configuration.Save();
            }
        }
    }
}
