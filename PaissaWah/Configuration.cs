using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Configuration;

namespace PaissaWah
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // Configuration setting for auto download interval in hours
        public int DownloadIntervalHours { get; set; } = 24;

        [JsonIgnore]
        private static string ConfigFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "pluginConfigs", "PaissaWah", "config.json"
        );

        public Configuration() { }

        // Method to save the configuration to the default path
        public void Save()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(this, jsonOptions);
            File.WriteAllText(ConfigFilePath, jsonString);
        }

        // Method to load the configuration from the default path
        public static Configuration Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                var jsonString = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<Configuration>(jsonString) ?? new Configuration();
            }
            return new Configuration();
        }
    }
}
