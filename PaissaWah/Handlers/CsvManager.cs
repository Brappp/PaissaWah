using PaissaWah.Configuration;
using PaissaWah.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace PaissaWah.Handlers
{
    public class CsvManager
    {
        private readonly string csvUrl = "https://paissadb.zhu.codes/csv/dump";
        private readonly string csvDirectoryPath;
        private readonly string csvFilePath;
        private readonly PaissaWah.Configuration.Configuration configuration;  
        public DateTime LastDownloadTime { get; private set; } = DateTime.MinValue;

        public event Action<string>? StatusUpdated;

        public CsvManager(PaissaWah.Configuration.Configuration config, string customCsvDirectoryPath)
        {
            configuration = config;

            // Ensure the directory exists; create it if it doesn't
            if (!Directory.Exists(customCsvDirectoryPath))
            {
                Directory.CreateDirectory(customCsvDirectoryPath);
            }

            csvDirectoryPath = customCsvDirectoryPath;
            csvFilePath = Path.Combine(csvDirectoryPath, "housing.csv");

            // Initialize settings from the configuration
            AutoDownloadIntervalHours = config.DownloadIntervalHours;
        }

        public string GetCsvDirectoryPath()
        {
            return csvDirectoryPath;
        }

        public int AutoDownloadIntervalHours
        {
            get => configuration.DownloadIntervalHours;
            set
            {
                configuration.DownloadIntervalHours = value;
                configuration.Save(); 
            }
        }

        public async Task DownloadLatestCsv(bool forceDownload = false)
        {
            try
            {
                if (forceDownload || DateTime.UtcNow >= LastDownloadTime.AddHours(AutoDownloadIntervalHours))
                {
                    StatusUpdated?.Invoke("Starting CSV download...");

                    if (File.Exists(csvFilePath))
                    {
                        StatusUpdated?.Invoke("Deleting old CSV...");
                        File.Delete(csvFilePath);
                    }

                    using var client = new HttpClient();
                    var response = await client.GetAsync(csvUrl);
                    response.EnsureSuccessStatusCode();

                    await using var fileStream = new FileStream(csvFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fileStream);

                    LastDownloadTime = DateTime.UtcNow;
                    StatusUpdated?.Invoke("CSV download completed.");
                }
                else
                {
                    StatusUpdated?.Invoke($"Next auto-download in {AutoDownloadIntervalHours - (DateTime.UtcNow - LastDownloadTime).TotalHours:F2} hours.");
                }
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Error downloading CSV: {ex.Message}");
            }
        }

        public IEnumerable<HousingData> QueryHousingData(List<string> selectedWorlds, List<string> selectedDistricts, int selectedWard, int days, bool isOwned, string houseSize)
        {
            if (!File.Exists(csvFilePath))
            {
                StatusUpdated?.Invoke("CSV file not found.");
                return Enumerable.Empty<HousingData>();
            }

            try
            {
                using var reader = new StreamReader(csvFilePath);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));
                csv.Context.RegisterClassMap<HousingDataMap>();
                var housingData = csv.GetRecords<HousingData>().ToList();

                if (housingData == null || !housingData.Any())
                {
                    StatusUpdated?.Invoke("No data found in CSV.");
                    return Enumerable.Empty<HousingData>();
                }

                var results = housingData
                    .Where(h =>
                        selectedWorlds.Contains(h.World) &&
                        selectedDistricts.Contains(h.District) &&
                        h.WardNumber <= selectedWard &&
                        h.IsOwned == isOwned &&
                        h.LastSeen >= DateTime.UtcNow.AddDays(-days) &&
                        (houseSize == "Any" || h.HouseSize.Equals(houseSize, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                return results;
            }
            catch (Exception ex)
            {
                StatusUpdated?.Invoke($"Error querying CSV data: {ex.Message}");
                return Enumerable.Empty<HousingData>();
            }
        }
    }
}
