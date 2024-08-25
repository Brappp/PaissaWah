using System.Numerics;
using ImGuiNET;

namespace PaissaWah.Windows
{
    public class PlotSettingsSection
    {
        private int selectedWard;
        private int days;
        private string selectedHouseSize;
        private bool isOwned;
        private bool isInLotto;
        private bool allWardsSelected;

        public PlotSettingsSection(int selectedWard, int days, string selectedHouseSize, bool isOwned, bool isInLotto, bool allWardsSelected)
        {
            this.selectedWard = selectedWard;
            this.days = days;
            this.selectedHouseSize = selectedHouseSize;
            this.isOwned = isOwned;
            this.isInLotto = isInLotto;
            this.allWardsSelected = allWardsSelected;
        }

        public void Draw()
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            ImGui.BeginChild("PlotSettingsSection", new Vector2(ImGui.GetContentRegionAvail().X, 400), false); 
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.1f, 0.6f, 0.8f, 1.0f));
            ImGui.Text("Plot Settings");
            ImGui.PopStyleColor();
            ImGui.Separator();

            // Is in Lotto Checkbox 
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f)); 
            ImGui.Checkbox("In Lotto", ref isInLotto);
            ImGui.PopStyleColor();

            // All Wards Checkbox
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f)); 
            ImGui.Checkbox("All Wards", ref allWardsSelected);
            ImGui.PopStyleColor();

            ImGui.Text("Select Ward:");
            ImGui.TextWrapped("Choose the ward number (1-30) to narrow down your search to a specific ward.");
            ImGui.BeginDisabled(allWardsSelected);
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.SliderInt("Ward Number", ref selectedWard, 1, 30);
            ImGui.EndDisabled();

            ImGui.Separator();

            ImGui.Text("Days:");
            ImGui.TextWrapped("Specify how many days back you want to include in your search.");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputInt("Number of Days", ref days);

            ImGui.Separator();

            ImGui.Text("House Size:");
            ImGui.TextWrapped("Filter the search results by house size (SMALL, MEDIUM, LARGE).");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.BeginCombo("House Size", selectedHouseSize))
            {
                foreach (var size in new[] { "Any", "SMALL", "MEDIUM", "LARGE" })
                {
                    bool isSelected = (size == selectedHouseSize);
                    if (ImGui.Selectable(size, isSelected))
                    {
                        selectedHouseSize = size;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.Separator();

            ImGui.Text("Ownership:");
            ImGui.TextWrapped("Check this box if you only want to see plots that are currently owned.");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.2f, 0.2f, 0.2f, 1.0f)); 
            ImGui.Checkbox("Owned", ref isOwned);
            ImGui.PopStyleColor();

            ImGui.Separator();

            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }
}
