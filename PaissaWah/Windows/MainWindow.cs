using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using PaissaWah.Models;
using PaissaWah.Data;
using PaissaWah.Handlers;

namespace PaissaWah.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly CsvManager csvManager;
        private readonly IChatGui chatGui;
        private readonly LifestreamIpcHandler lifestreamIpcHandler;

        private Dictionary<string, bool> selectedWorlds = new Dictionary<string, bool>();
        private Dictionary<string, bool> selectedDistricts = new Dictionary<string, bool>();
        private int selectedWard = 1;
        private int days = 30;
        private bool isOwned = false;
        private string selectedHouseSize = "Any";
        private bool isInLotto = false;
        private bool allWardsSelected = false;

        private WorldSelectionSection worldSelectionSection;
        private DistrictSelectionSection districtSelectionSection;
        private PlotSettingsSection plotSettingsSection;

        private string statusMessage = "Ready";
        private List<HousingData> queryResults = new List<HousingData>();
        private bool isDownloadInProgress = false;
        private int autoDownloadIntervalHours;
        private bool showSeeResultsMessage = false;
        private string saveFilePath;

        public MainWindow(Plugin plugin, string goatImagePath)
            : base("PaissaWah##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.plugin = plugin;
            chatGui = Plugin.ChatGui;
            csvManager = plugin.CsvManager;
            lifestreamIpcHandler = plugin.LifestreamIpcHandler;

            saveFilePath = Path.Combine(csvManager.GetCsvDirectoryPath(), "results.csv");

            csvManager.StatusUpdated += OnStatusUpdated;

            LoadWorldsAndDistricts();

            autoDownloadIntervalHours = csvManager.AutoDownloadIntervalHours;
            Task.Run(() => csvManager.DownloadLatestCsv());

            worldSelectionSection = new WorldSelectionSection(selectedWorlds);
            districtSelectionSection = new DistrictSelectionSection(selectedDistricts);
            plotSettingsSection = new PlotSettingsSection(selectedWard, days, selectedHouseSize, isOwned, isInLotto, allWardsSelected);
        }

        private void LoadWorldsAndDistricts()
        {
            selectedWorlds.Clear();
            var datacenters = WorldData.GetWorldsByDatacenter();
            foreach (var dc in datacenters)
            {
                foreach (var world in dc.Value)
                {
                    selectedWorlds[world] = false;
                }
            }

            selectedDistricts.Clear();
            var districts = GetDistricts();
            foreach (var district in districts)
            {
                selectedDistricts[district] = false;
            }
        }

        public void Dispose()
        {
            csvManager.StatusUpdated -= OnStatusUpdated;
        }

        private void OnStatusUpdated(string message)
        {
            statusMessage = message;
            isDownloadInProgress = message.Contains("In Progress", StringComparison.OrdinalIgnoreCase);
        }

        public override void Draw()
        {
            ImGui.SetWindowSize(new Vector2(1200, 900), ImGuiCond.FirstUseEver);

            // Begin scrollable area for the entire window content
            ImGui.BeginChild("MainScrollableArea", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), true, ImGuiWindowFlags.AlwaysVerticalScrollbar);

            if (ImGui.BeginTabBar("MainTabs"))
            {
                if (ImGui.BeginTabItem("Settings"))
                {
                    DrawSettingsTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Results"))
                {
                    DrawResultsTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.EndChild(); // End of scrollable area
        }

        private void DrawSettingsTab()
        {
            var colorAccent = new Vector4(0.1f, 0.6f, 0.8f, 1.0f);
            var colorSectionBackground = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
            var colorGreen = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
            var colorButton = new Vector4(0.1f, 0.6f, 0.8f, 1.0f);
            var colorButtonHovered = new Vector4(0.1f, 0.8f, 1.0f, 1.0f);
            var colorButtonActive = new Vector4(0.0f, 0.5f, 0.7f, 1.0f);

            ImGui.PushStyleColor(ImGuiCol.Text, colorAccent);
            ImGui.Text("Instructions:");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.TextWrapped("1. Select the desired worlds and districts.\n" +
                              "2. Configure the filters in the settings below.\n" +
                              "3. Click 'Execute Query' to get results.");
            ImGui.Separator();

            ImGui.Separator();

            // Execute Query Button
            ImGui.PushStyleColor(ImGuiCol.Button, colorButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, colorButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, colorButtonActive);
            if (ImGui.Button("Execute Query", new Vector2(200, 40)))
            {
                try
                {
                    var selectedWorldsList = selectedWorlds.Where(w => w.Value).Select(w => w.Key).ToList();
                    var selectedDistrictsList = selectedDistricts.Where(d => d.Value).Select(d => d.Key).ToList();

                    var results = csvManager.QueryHousingData(selectedWorldsList, selectedDistrictsList, allWardsSelected ? 30 : selectedWard, days, isOwned, selectedHouseSize)
                                            .Where(r => !isInLotto || r.LottoPhaseUntil.HasValue)
                                            .ToList();

                    queryResults = results;
                    showSeeResultsMessage = true;

                    OnStatusUpdated("Ready");
                }
                catch (Exception ex)
                {
                    chatGui.PrintError(ex.Message);
                    OnStatusUpdated($"Error: {ex.Message}");
                }
            }
            ImGui.PopStyleColor(3);

            if (showSeeResultsMessage)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.0f, 1.0f), "Query executed. Please check the 'Results' tab for details.");
            }

            ImGui.Separator();

            ImGui.Separator();

            ImGui.Text($"Last Download: {csvManager.LastDownloadTime:g} UTC");

            ImGui.TextColored(colorAccent, "CSV Download:");
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, colorButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, colorButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, colorButtonActive);
            if (ImGui.Button("Download CSV"))
            {
                Task.Run(() => csvManager.DownloadLatestCsv(true));
            }
            ImGui.PopStyleColor(3);

            ImGui.SameLine();

            if (isDownloadInProgress)
            {
                ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), "Download In Progress...");
            }
            else if (statusMessage.Equals("Ready", StringComparison.OrdinalIgnoreCase))
            {
                ImGui.TextColored(colorGreen, "Ready ✔");
            }
            else
            {
                ImGui.TextColored(new Vector4(1.0f, 0.0f, 0.0f, 1.0f), statusMessage);
            }

            ImGui.Separator();

            ImGui.Text("Auto-Download Interval (Hours):");
            ImGui.TextWrapped("Controls how often we check for a new housing csv.");

            ImGui.InputInt("Interval (Hours)", ref autoDownloadIntervalHours);

            ImGui.PushStyleColor(ImGuiCol.Button, colorButton);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, colorButtonHovered);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, colorButtonActive);
            if (ImGui.Button("Set Interval"))
            {
                csvManager.AutoDownloadIntervalHours = autoDownloadIntervalHours;
            }
            ImGui.PopStyleColor(3);

            ImGui.Separator();

            ImGui.Separator();

            ImGui.Separator();

            worldSelectionSection.Draw();

            ImGui.Separator();

            districtSelectionSection.Draw();

            ImGui.Separator();

            plotSettingsSection.Draw();

            ImGui.Separator();
        }

        private void DrawResultsTab()
        {
            var colorAccent = new Vector4(0.1f, 0.6f, 0.8f, 1.0f);
            var colorButton = new Vector4(0.1f, 0.6f, 0.8f, 1.0f);
            var colorButtonHovered = new Vector4(0.1f, 0.8f, 1.0f, 1.0f);
            var colorButtonActive = new Vector4(0.0f, 0.5f, 0.7f, 1.0f);

            ImGui.TextColored(colorAccent, "Query Results:");

            if (queryResults.Any())
            {
                ImGui.Text("Export Results to CSV:");
                ImGui.InputText("Save File Path", ref saveFilePath, 512);

                ImGui.PushStyleColor(ImGuiCol.Button, colorButton);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, colorButtonHovered);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, colorButtonActive);
                if (ImGui.Button("Export to CSV"))
                {
                    if (SaveResultsToCsv(saveFilePath))
                    {
                        chatGui.Print($"Results successfully saved to {saveFilePath}.");
                    }
                }
                ImGui.PopStyleColor(3);

                ImGui.Separator();

                ImGui.BeginChild("Results", new Vector2(0, 600), true);
                if (ImGui.BeginTable("ResultsTable", 13, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.NoHide);
                    ImGui.TableSetupColumn("Datacenter");
                    ImGui.TableSetupColumn("World");
                    ImGui.TableSetupColumn("District");
                    ImGui.TableSetupColumn("Ward");
                    ImGui.TableSetupColumn("Plot");
                    ImGui.TableSetupColumn("Size");
                    ImGui.TableSetupColumn("Price");
                    ImGui.TableSetupColumn("Is Owned");
                    ImGui.TableSetupColumn("Last Seen");
                    ImGui.TableSetupColumn("Lotto Phase Until");
                    ImGui.TableSetupColumn("Is in Lotto");
                    ImGui.TableSetupColumn("Travel");
                    ImGui.TableHeadersRow();

                    foreach (var result in queryResults)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(result.Id.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(GetDatacenter(result.World));

                        ImGui.TableNextColumn();
                        ImGui.Text(result.World);

                        ImGui.TableNextColumn();
                        ImGui.Text(result.District);

                        ImGui.TableNextColumn();
                        ImGui.Text(result.WardNumber.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(result.PlotNumber.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(result.HouseSize);

                        ImGui.TableNextColumn();
                        ImGui.Text(result.Price);

                        ImGui.TableNextColumn();
                        ImGui.Text(result.IsOwned ? "Yes" : "No");

                        ImGui.TableNextColumn();
                        ImGui.Text(result.LastSeen.ToString("yyyy-MM-dd HH:mm:ss"));

                        ImGui.TableNextColumn();
                        ImGui.Text(result.LottoPhaseUntil?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");

                        ImGui.TableNextColumn();
                        ImGui.Text(result.LottoPhaseUntil.HasValue ? "Yes" : "No");

                        // Travel Button
                        ImGui.TableNextColumn();
                        ImGui.PushStyleColor(ImGuiCol.Button, colorButton);
                        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, colorButtonHovered);
                        ImGui.PushStyleColor(ImGuiCol.ButtonActive, colorButtonActive);
                        if (ImGui.Button($"Travel##{result.Id}"))
                        {
                            var commandArgs = $"{result.World}, {result.District}, w{result.WardNumber} p{result.PlotNumber}";
                            try
                            {
                                lifestreamIpcHandler.ExecuteLiCommand(commandArgs);
                            }
                            catch (Exception ex)
                            {
                                chatGui.PrintError($"Error invoking Lifestream teleport: {ex.Message}");
                            }
                        }
                        ImGui.PopStyleColor(3);
                    }

                    ImGui.EndTable();
                }
                ImGui.EndChild();
            }
            else
            {
                ImGui.Text("No results found.");
            }
        }

        private bool SaveResultsToCsv(string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("ID,Datacenter,World,District,Ward,Plot,Size,Price,Is Owned,Last Seen,Lotto Phase Until");

                    foreach (var result in queryResults)
                    {
                        var line = $"{result.Id},{GetDatacenter(result.World)},{result.World},{result.District},{result.WardNumber},{result.PlotNumber},{result.HouseSize},{result.Price},{result.IsOwned},{result.LastSeen:yyyy-MM-dd HH:mm:ss},{result.LottoPhaseUntil?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}";
                        writer.WriteLine(line);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                chatGui.PrintError($"Failed to save CSV: {ex.Message}");
                return false;
            }
        }

        private string GetDatacenter(string world)
        {
            foreach (var dc in WorldData.GetWorldsByDatacenter())
            {
                if (dc.Value.Contains(world))
                {
                    return dc.Key;
                }
            }
            return "Unknown";
        }

        private List<string> GetDistricts()
        {
            return new List<string> { "Mist", "The Goblet", "The Lavender Beds", "Empyreum", "Shirogane" };
        }
    }
}
