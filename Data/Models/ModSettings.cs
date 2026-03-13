using System.Text.Json.Serialization;

namespace STS2ViewedCardsStatistics.Data.Models
{
    public class ModSettings
    {
        public const int CurrentDataVersion = 1;

        [JsonPropertyName("data_version")] public int DataVersion { get; set; } = CurrentDataVersion;

        [JsonPropertyName("verbose_import_logging")]
        public bool VerboseImportLogging { get; set; } = true;
    }
}
