using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PaissaWah.Data;

namespace PaissaWah.Windows
{
    public class WorldSelectionSection
    {
        private readonly Dictionary<string, bool> selectedWorlds;

        public WorldSelectionSection(Dictionary<string, bool> selectedWorlds)
        {
            this.selectedWorlds = selectedWorlds;
        }

        public void Draw()
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            ImGui.BeginChild("WorldSelectionSection", new Vector2(ImGui.GetContentRegionAvail().X, 350), false); 
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.6f, 0.8f, 1.0f));
            ImGui.Text("Select Worlds");
            ImGui.PopStyleColor();
            ImGui.Separator();

            var datacenters = WorldData.GetWorldsByDatacenter();
            foreach (var dc in datacenters)
            {
                ImGui.BeginGroup();
                ImGui.TextColored(new Vector4(0.1f, 0.6f, 0.8f, 1.0f), dc.Key);
                foreach (var world in dc.Value)
                {
                    bool selected = selectedWorlds[world];
                    ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f)); 
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X); 
                    if (ImGui.Checkbox(world, ref selected))
                    {
                        selectedWorlds[world] = selected;
                    }
                    ImGui.PopStyleColor();
                }
                ImGui.EndGroup();
                ImGui.SameLine();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }
}
