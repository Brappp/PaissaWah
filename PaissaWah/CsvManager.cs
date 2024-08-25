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

namespace PaissaWah
{
    public class CsvManager
    {
        private readonly string csvUrl = "https://paissadb.zhu.codes/csv/dump";
        private readonly string csvDirectoryPath;
        private readonly string csvFilePath;
        private readonly Configuration configuration; // Reference to the configuration
        public DateTime LastDownloadTime { get; private set; } = DateTime.MinValue;

        public event Action<string>? StatusUpdated;

        public CsvManager(Configuration config, string customCsvDirectoryPath)
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
                configuration.Save(); // Save the updated value to the config
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

        public class HousingData
        {
            public int Id { get; set; }
            public string World { get; set; } = string.Empty;
            public string District { get; set; } = string.Empty;
            public int WardNumber { get; set; }
            public int PlotNumber { get; set; }
            public string HouseSize { get; set; } = string.Empty;
            public string LottoEntries { get; set; } = string.Empty;
            public string Price { get; set; } = string.Empty;

            public long FirstSeenEpoch { get; set; }
            public long LastSeenEpoch { get; set; }
            public long? LottoPhaseUntilEpoch { get; set; }

            public bool IsOwned { get; set; }
            public string LottoPhase { get; set; } = string.Empty;

            public DateTime FirstSeen => DateTimeOffset.FromUnixTimeSeconds(FirstSeenEpoch).DateTime;
            public DateTime LastSeen => DateTimeOffset.FromUnixTimeSeconds(LastSeenEpoch).DateTime;
            public DateTime? LottoPhaseUntil => LottoPhaseUntilEpoch.HasValue ? (DateTime?)DateTimeOffset.FromUnixTimeSeconds(LottoPhaseUntilEpoch.Value).DateTime : null;
        }

        public sealed class HousingDataMap : ClassMap<HousingData>
        {
            public HousingDataMap()
            {
                Map(m => m.Id).Name("id");
                Map(m => m.World).Name("world");
                Map(m => m.District).Name("district");
                Map(m => m.WardNumber).Name("ward_number");
                Map(m => m.PlotNumber).Name("plot_number");
                Map(m => m.HouseSize).Name("house_size");
                Map(m => m.LottoEntries).Name("lotto_entries");
                Map(m => m.Price).Name("price");
                Map(m => m.FirstSeenEpoch).Name("first_seen").TypeConverter<EpochTimeConverter>();
                Map(m => m.LastSeenEpoch).Name("last_seen").TypeConverter<EpochTimeConverter>();
                Map(m => m.LottoPhaseUntilEpoch).Name("lotto_phase_until").TypeConverter<EpochTimeConverter>().TypeConverterOption.NullValues(string.Empty);
                Map(m => m.IsOwned).Name("is_owned");
                Map(m => m.LottoPhase).Name("lotto_phase");
            }
        }
    }

    public class EpochTimeConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            if (text.Contains('.'))
            {
                text = text.Split('.')[0];
            }

            if (long.TryParse(text, out long epochTime))
            {
                return epochTime;
            }

            throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unable to convert '{text}' to {typeof(long)}.");
        }
    }
}
