using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Models;

namespace STS2ViewedCardsStatistics.Data.Models
{
    /// <summary>
    ///     Main data structure for statistics
    /// </summary>
    public class ViewedStatisticsData
    {
        public const int CurrentDataVersion = 1;

        [JsonPropertyName("data_version")] public int DataVersion { get; set; } = CurrentDataVersion;

        [JsonPropertyName("card_stats")] public Dictionary<string, CharacterItemStats> CardStats { get; set; } = [];

        [JsonPropertyName("relic_stats")] public Dictionary<string, CharacterItemStats> RelicStats { get; set; } = [];

        [JsonPropertyName("potion_stats")] public Dictionary<string, CharacterItemStats> PotionStats { get; set; } = [];

        [JsonPropertyName("monster_stats")]
        public Dictionary<string, CharacterItemStats> MonsterStats { get; set; } = [];

        [JsonPropertyName("processed_runs")] public HashSet<string> ProcessedRuns { get; set; } = [];

        public void Clear()
        {
            DataVersion = CurrentDataVersion;
            CardStats.Clear();
            RelicStats.Clear();
            PotionStats.Clear();
            MonsterStats.Clear();
            ProcessedRuns.Clear();
        }

        public CharacterItemStats GetOrCreateCardStats(ModelId characterId)
        {
            var key = characterId.ToString();
            if (CardStats.TryGetValue(key, out var stats)) return stats;
            stats = new() { CharacterId = key };
            CardStats[key] = stats;
            return stats;
        }

        public CharacterItemStats GetOrCreateRelicStats(ModelId characterId)
        {
            var key = characterId.ToString();
            if (RelicStats.TryGetValue(key, out var stats)) return stats;
            stats = new() { CharacterId = key };
            RelicStats[key] = stats;
            return stats;
        }

        public CharacterItemStats GetOrCreatePotionStats(ModelId characterId)
        {
            var key = characterId.ToString();
            if (PotionStats.TryGetValue(key, out var stats)) return stats;
            stats = new() { CharacterId = key };
            PotionStats[key] = stats;
            return stats;
        }

        public CharacterItemStats GetOrCreateMonsterStats(ModelId characterId)
        {
            var key = characterId.ToString();
            if (MonsterStats.TryGetValue(key, out var stats)) return stats;
            stats = new() { CharacterId = key };
            MonsterStats[key] = stats;
            return stats;
        }
    }

    /// <summary>
    ///     Statistics for items seen/picked by a character
    /// </summary>
    public class CharacterItemStats
    {
        [JsonPropertyName("character_id")] public string CharacterId { get; set; } = "";

        [JsonPropertyName("seen")] public Dictionary<string, long> SeenItems { get; set; } = [];

        [JsonPropertyName("picked")] public Dictionary<string, long> PickedItems { get; set; } = [];

        [JsonPropertyName("total_seen")] public long TotalSeen { get; set; }

        [JsonPropertyName("total_picked")] public long TotalPicked { get; set; }

        public void RecordSeen(ModelId itemId, long count = 1)
        {
            var key = itemId.ToString();
            SeenItems[key] = SeenItems.GetValueOrDefault(key, 0) + count;
            TotalSeen += count;
        }

        public void RecordPicked(ModelId itemId, long count = 1)
        {
            var key = itemId.ToString();
            PickedItems[key] = PickedItems.GetValueOrDefault(key, 0) + count;
            TotalPicked += count;
        }

        public long GetSeenCount(ModelId itemId)
        {
            return SeenItems.GetValueOrDefault(itemId.ToString(), 0);
        }

        public long GetPickedCount(ModelId itemId)
        {
            return PickedItems.GetValueOrDefault(itemId.ToString(), 0);
        }
    }
}
