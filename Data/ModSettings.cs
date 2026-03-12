using System.Text.Json.Serialization;

namespace STS2ViewedCardsStatistics.Data
{
    public class ModSettings
    {
        [JsonPropertyName("verbose_import_logging")]
        public bool VerboseImportLogging { get; set; } = true;
    }
}
