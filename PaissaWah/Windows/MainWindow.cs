using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;

namespace PaissaWah.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly CsvManager csvManager;
        private readonly IChatGui chatGui;

        private Dictionary<string, bool> selectedWorlds = new Dictionary<string, bool>();
        private Dictionary<string, bool> selectedDistricts = new Dictionary<string, bool>();
        private int selectedWard = 1;
        private int days = 30;
        private bool isOwned = false;
        private string selectedHouseSize = "Any";
        private bool isInLotto = false;

        private bool allWardsSelected = false;
        private string statusMessage = "Ready";
        private List<CsvManager.HousingData> queryResults = new List<CsvManager.HousingData>();
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

            saveFilePath = Path.Combine(csvManager.GetCsvDirectoryPath(), "results.csv");

            csvManager.StatusUpdated += OnStatusUpdated;

            LoadWorldsAndDistricts();

            autoDownloadIntervalHours = csvManager.AutoDownloadIntervalHours;
            Task.Run(() => csvManager.DownloadLatestCsv());
        }

        private void LoadWorldsAndDistricts()
        {
            var datacenters = GetWorldsByDatacenter();
            foreach (var dc in datacenters)
            {
                foreach (var world in dc.Value)
                {
                    selectedWorlds[world] = false;
                }
            }

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
        }

        private void DrawSettingsTab()
        {
            var colorAccent = new Vector4(0.1f, 0.6f, 0.8f, 1.0f);
            var colorSectionBackground = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
            var colorGreen = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);

            ImGui.PushStyleColor(ImGuiCol.Text, colorAccent);
            ImGui.Text("Instructions:");
            ImGui.PopStyleColor();
            ImGui.Separator();
            ImGui.TextWrapped("1. Select the desired worlds and districts.\n" +
                              "2. Configure the filters in the settings below.\n" +
                              "3. Click 'Execute Query' to get results.");
            ImGui.Separator();

            ImGui.TextColored(colorAccent, "CSV Download:");
            ImGui.SameLine();
            if (ImGui.Button("Download CSV"))
            {
                Task.Run(() => csvManager.DownloadLatestCsv(true));
            }
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
            ImGui.InputInt("Interval (Hours)", ref autoDownloadIntervalHours);
            if (ImGui.Button("Set Interval"))
            {
                csvManager.AutoDownloadIntervalHours = autoDownloadIntervalHours;
            }

            ImGui.Separator();

            ImGui.TextWrapped("The auto-download feature allows the plugin to automatically download the latest CSV data at the specified interval. " +
                              "The interval is checked every hour, and if the specified number of hours has passed since the last download, a new download will be triggered automatically.");
            ImGui.TextWrapped("For example, if you set the interval to 2 hours, the plugin will attempt to download the latest CSV data every 2 hours.");

            ImGui.Separator();

            ImGui.Text($"Last Download: {csvManager.LastDownloadTime:g} UTC");

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, colorSectionBackground);
            ImGui.BeginChild("WorldSelectionSection", new Vector2(0, 300), true);
            ImGui.PushStyleColor(ImGuiCol.Text, colorAccent);
            ImGui.Text("Select Worlds");
            ImGui.PopStyleColor();
            ImGui.Separator();

            var datacenters = GetWorldsByDatacenter();
            foreach (var dc in datacenters)
            {
                ImGui.BeginGroup();
                ImGui.TextColored(colorAccent, dc.Key);
                foreach (var world in dc.Value)
                {
                    bool selected = selectedWorlds[world];
                    if (ImGui.Checkbox(world, ref selected))
                    {
                        selectedWorlds[world] = selected;
                    }
                }
                ImGui.EndGroup();
                ImGui.SameLine();
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, colorSectionBackground);
            ImGui.BeginChild("DistrictSelectionSection", new Vector2(0, 80), true);
            ImGui.PushStyleColor(ImGuiCol.Text, colorAccent);
            ImGui.Text("Select Districts");
            ImGui.PopStyleColor();
            ImGui.Separator();

            bool first = true;
            foreach (var district in GetDistricts())
            {
                if (!first)
                {
                    ImGui.SameLine();
                }
                first = false;

                bool selected = selectedDistricts[district];
                if (ImGui.Checkbox(district, ref selected))
                {
                    selectedDistricts[district] = selected;
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleColor();

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.ChildBg, colorSectionBackground);
            ImGui.BeginChild("CombinedSettingsSection", new Vector2(0, 380), true);
            ImGui.PushStyleColor(ImGuiCol.Text, colorAccent);
            ImGui.Text("Plot Settings");
            ImGui.PopStyleColor();
            ImGui.Separator();

            ImGui.Checkbox("All Wards", ref allWardsSelected);

            ImGui.Text("Select Ward:");
            ImGui.TextWrapped("Choose the ward number (1-30) to narrow down your search to a specific ward.");
            ImGui.BeginDisabled(allWardsSelected);
            ImGui.SliderInt("Ward Number", ref selectedWard, 1, 30);
            ImGui.EndDisabled();

            ImGui.Separator();

            ImGui.Text("Days:");
            ImGui.TextWrapped("Specify how many days back you want to include in your search.");
            ImGui.InputInt("Number of Days", ref days);

            ImGui.Separator();

            ImGui.Text("House Size:");
            ImGui.TextWrapped("Filter the search results by house size (SMALL, MEDIUM, LARGE).");
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
            ImGui.Checkbox("Owned", ref isOwned);

            ImGui.Separator();

            ImGui.Text("Is in Lotto:");
            ImGui.TextWrapped("Check this box to only include plots that are currently in the lotto phase.");
            ImGui.Checkbox("In Lotto", ref isInLotto);

            ImGui.Separator();

            ImGui.EndChild();
            ImGui.PopStyleColor();

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.Button, colorAccent);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.1f, 0.8f, 1.0f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.0f, 0.5f, 0.7f, 1.0f));
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

                    foreach (var result in results)
                    {
                        chatGui.Print($"Datacenter: {GetDatacenter(result.World)}, World: {result.World}, District: {result.District}, Ward: {(allWardsSelected ? "All" : result.WardNumber.ToString())}, Plot: {result.PlotNumber}, " +
                                      $"Size: {result.HouseSize}, Price: {result.Price}, Owned: {result.IsOwned}, Last Seen: {result.LastSeen:yyyy-MM-dd HH:mm:ss}");
                    }

                    OnStatusUpdated("Ready");
                }
                catch (Exception ex)
                {
                    chatGui.PrintError(ex.Message);
                    OnStatusUpdated($"Error: {ex.Message}");
                }
            }
            ImGui.PopStyleColor(3);

            ImGui.Separator();

            if (showSeeResultsMessage)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.5f, 0.0f, 1.0f), "Query executed. Please check the 'Results' tab for details.");
            }
        }

        private void DrawResultsTab()
        {
            var colorAccent = new Vector4(0.1f, 0.6f, 0.8f, 1.0f);
            ImGui.TextColored(colorAccent, "Query Results:");

            if (queryResults.Any())
            {
                ImGui.Text("Export Results to CSV:");
                ImGui.InputText("Save File Path", ref saveFilePath, 512);

                if (ImGui.Button("Export to CSV"))
                {
                    if (SaveResultsToCsv(saveFilePath))
                    {
                        chatGui.Print($"Results successfully saved to {saveFilePath}.");
                    }
                }

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
                        if (ImGui.Button($"Travel##{result.Id}"))
                        {
                            // Construct the travel command
                            var travelCommand = $"li {result.World}, {result.District}, w{result.WardNumber} p{result.PlotNumber}";

                            // Send the command to the chat
                            chatGui.Print($"/{travelCommand}"); // Send the command to the chat for the user to execute
                        }

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

        private Dictionary<string, List<string>> GetWorldsByDatacenter()
        {
            return new Dictionary<string, List<string>>()
            {
                { "Aether", new List<string> { "Adamantoise", "Cactuar", "Faerie", "Gilgamesh", "Jenova", "Midgardsormr", "Sargatanas", "Siren" }},
                { "Primal", new List<string> { "Behemoth", "Excalibur", "Exodus", "Famfrit", "Hyperion", "Lamia", "Leviathan", "Ultros" }},
                { "Crystal", new List<string> { "Balmung", "Brynhildr", "Coeurl", "Diabolos", "Goblin", "Malboro", "Mateus", "Zalera" }},
                { "Chaos", new List<string> { "Cerberus", "Louisoix", "Moogle", "Omega", "Ragnarok", "Spriggan" }},
                { "Light", new List<string> { "Lich", "Odin", "Phoenix", "Shiva", "Twintania", "Zodiark" }},
                { "Elemental", new List<string> { "Aegis", "Atomos", "Carbuncle", "Garuda", "Gungnir", "Kujata", "Ramuh", "Tonberry", "Typhon", "Unicorn" }},
                { "Gaia", new List<string> { "Alexander", "Bahamut", "Durandal", "Fenrir", "Ifrit", "Ridill", "Tiamat", "Ultima", "Valefor", "Yojimbo", "Zeromus" }},
                { "Mana", new List<string> { "Anima", "Asura", "Chocobo", "Hades", "Ixion", "Mandragora", "Masamune", "Pandaemonium", "Shinryu", "Titan" }},
                { "Materia", new List<string> { "Bismarck", "Ravana", "Sephirot", "Sophia", "Zurvan" }},
            };
        }

        private string GetDatacenter(string world)
        {
            foreach (var dc in GetWorldsByDatacenter())
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
