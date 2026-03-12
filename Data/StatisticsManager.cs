using System.Text;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using STS2ViewedCardsStatistics.Utils;

namespace STS2ViewedCardsStatistics.Data
{
    /// <summary>
    ///     Statistics data manager
    /// </summary>
    public class StatisticsManager
    {
        private static StatisticsManager? _instance;

        private Setting<ViewedStatisticsData>? _dataSettings;
        private Setting<ModSettings>? _modSettings;

        private StatisticsManager()
        {
        }

        public bool VerboseImportLogging
        {
            get => _modSettings?.Data.VerboseImportLogging ?? false;
            set
            {
                if (_modSettings == null) return;
                _modSettings.Data.VerboseImportLogging = value;
                _modSettings.Save();
            }
        }

        public static StatisticsManager Instance => _instance ??= new();
        public ViewedStatisticsData Data => _dataSettings?.Data ?? new ViewedStatisticsData();

        public bool IsInitialized => _dataSettings != null;
        public bool HasExistingData { get; private set; }

        public void Initialize()
        {
            _modSettings = new(Const.SettingsFilePath, new(), "ModSettings");
            _modSettings.Load();

            HasExistingData = FileOperations.FileExists(Const.DataFilePath);

            _dataSettings = new(Const.DataFilePath, new(), "StatisticsManager");

            if (HasExistingData)
            {
                _dataSettings.Load();
                MigrateDataIfNeeded();
            }

            Main.Logger.Info($"StatisticsManager initialized. HasExistingData: {HasExistingData}");
        }

        public void CreateEmptyData()
        {
            Data.Clear();
            Save();
            HasExistingData = true;
            Main.Logger.Info("Created empty statistics data");
        }

        public void ImportFromRunHistory()
        {
            Main.Logger.Info("Starting import from run history...");

            var historyNames = SaveManager.Instance.GetAllRunHistoryNames();
            Main.Logger.Info($"Found {historyNames.Count} run history files");

            var importedRuns = 0;
            foreach (var fileName in historyNames)
                try
                {
                    var result = SaveManager.Instance.LoadRunHistory(fileName);
                    if (result is not { Success: true, SaveData: not null }) continue;
                    ProcessRunHistory(result.SaveData, fileName);
                    importedRuns++;
                }
                catch (Exception ex)
                {
                    Main.Logger.Warn($"Failed to process run history {fileName}: {ex.Message}");
                }

            Save();
            HasExistingData = true;
            Main.Logger.Info($"Imported data from {importedRuns} run histories");
        }

        public void ClearAndReimportFromRunHistory()
        {
            Main.Logger.Info("Clearing existing data and reimporting from run history...");
            Data.Clear();
            ImportFromRunHistory();
        }

        /// <summary>
        ///     Process run history and save statistics when a run ends
        /// </summary>
        public void ProcessAndSaveRunHistory(RunHistory history)
        {
            ProcessRunHistory(history, "current_run");
            Save();
        }

        private void ProcessRunHistory(RunHistory history, string fileName = "", bool skipDuplicateCheck = false)
        {
            if (history.Players.Count == 0)
            {
                if (VerboseImportLogging)
                    Main.Logger.Info($"[{fileName}] Skipping: No players in history");
                return;
            }

            var localPlayer = history.Players[0];
            var characterId = localPlayer.Character;

            if (characterId == ModelId.none)
            {
                if (VerboseImportLogging)
                    Main.Logger.Info($"[{fileName}] Skipping: No character ID");
                return;
            }

            var runId = $"{history.Seed}_{history.StartTime}_{characterId}";
            if (!skipDuplicateCheck && Data.ProcessedRuns.Contains(runId))
            {
                if (VerboseImportLogging)
                    Main.Logger.Info($"[{fileName}] Skipping: Already processed (RunId: {runId})");
                return;
            }

            var characterName = GetCharacterDisplayName(characterId.ToString());

            if (VerboseImportLogging)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{fileName}] Processing run history:");
                sb.AppendLine($"  Character: {characterName} (ID: {characterId})");
                sb.AppendLine($"  Player ID: {localPlayer.Id}");
                sb.AppendLine($"  Total Players: {history.Players.Count}");
                if (history.Players.Count > 1)
                {
                    sb.AppendLine("  Other players (ignored):");
                    for (var i = 1; i < history.Players.Count; i++)
                    {
                        var otherPlayer = history.Players[i];
                        var otherCharName = GetCharacterDisplayName(otherPlayer.Character.ToString());
                        sb.AppendLine(
                            $"    - {otherCharName} (ID: {otherPlayer.Character}, PlayerID: {otherPlayer.Id})");
                    }
                }

                sb.Append($"  Acts: {history.MapPointHistory.Count}");
                Main.Logger.Info(sb.ToString());
            }

            var actIndex = 0;
            foreach (var actHistory in history.MapPointHistory)
            {
                actIndex++;
                if (VerboseImportLogging)
                    Main.Logger.Info($"  [Act {actIndex}] Processing {actHistory.Count} map points");

                var mapPointIndex = 0;
                foreach (var mapPoint in actHistory)
                {
                    mapPointIndex++;
                    ProcessMapPointHistory(mapPoint, localPlayer.Id, characterId, actIndex, mapPointIndex);
                }
            }

            Data.ProcessedRuns.Add(runId);
        }

        private void ProcessMapPointHistory(MapPointHistoryEntry mapPoint, ulong playerId, ModelId characterId,
            int actIndex = 0, int mapPointIndex = 0)
        {
            var playerEntry = mapPoint.PlayerStats.FirstOrDefault(entry => entry.PlayerId == playerId);

            if (playerEntry == null)
            {
                if (VerboseImportLogging)
                {
                    var typeDisplay = mapPoint.MapPointType.ToString();
                    if (mapPoint.MapPointType == MapPointType.Unknown && mapPoint.Rooms.Count > 0)
                        typeDisplay = $"{mapPoint.MapPointType}({mapPoint.Rooms[0].RoomType})";
                    Main.Logger.Info(
                        $"    [Act {actIndex}, Point {mapPointIndex}] {typeDisplay}: No player entry for ID {playerId}");
                }

                return;
            }

            var cardStats = Data.GetOrCreateCardStats(characterId);
            var relicStats = Data.GetOrCreateRelicStats(characterId);
            var potionStats = Data.GetOrCreatePotionStats(characterId);
            var monsterStats = Data.GetOrCreateMonsterStats(characterId);

            var logDetails = new StringBuilder();
            var cardChoiceNames = new List<string>();
            var cardGainedNames = new List<string>();
            var cardTransformedNames = new List<string>();
            var cardEnchantedNames = new List<string>();
            var cardUpgradedNames = new List<string>();
            var cardDowngradedNames = new List<string>();
            var cardRemovedNames = new List<string>();
            var cardBoughtColorlessNames = new List<string>();
            var cardChoiceCounts = new Dictionary<string, int>();
            var potionChoiceCounts = new Dictionary<string, int>();
            var relicChoiceCounts = new Dictionary<string, int>();
            var relicNames = new List<string>();
            var relicBoughtNames = new List<string>();
            var relicRemovedNames = new List<string>();
            var ancientChoiceNames = new List<string>();
            var potionNames = new List<string>();
            var potionBoughtNames = new List<string>();
            var potionUsedNames = new List<string>();
            var potionDiscardedNames = new List<string>();
            var monsterNames = new List<string>();

            var mapPointTypeDisplay = mapPoint is { MapPointType: MapPointType.Unknown, Rooms.Count: > 0 }
                ? $"{mapPoint.MapPointType}({mapPoint.Rooms[0].RoomType})"
                : mapPoint.MapPointType.ToString();

            if (VerboseImportLogging)
                logDetails.AppendLine($"    [Act {actIndex}, Point {mapPointIndex}] {mapPointTypeDisplay}:");

            foreach (var ancientChoice in playerEntry.AncientChoices)
            {
                var relicId = FindRelicIdByTitle(ancientChoice.Title.GetFormattedText());
                if (relicId == ModelId.none) continue;
                relicStats.RecordSeen(relicId);
                if (ancientChoice.WasChosen)
                    relicStats.RecordPicked(relicId);
                if (!VerboseImportLogging) continue;
                var picked = ancientChoice.WasChosen ? "[picked]" : "[skipped]";
                ancientChoiceNames.Add($"{GetRelicDisplayName(relicId)}{picked}");
            }

            foreach (var cardChoice in playerEntry.CardChoices)
            {
                var cardId = cardChoice.Card.Id;
                if (cardId == null || cardId == ModelId.none) continue;

                var key = cardId.ToString();
                cardChoiceCounts[key] = cardChoiceCounts.GetValueOrDefault(key, 0) + 1;

                cardStats.RecordSeen(cardId);
                if (cardChoice.wasPicked)
                    cardStats.RecordPicked(cardId);

                if (!VerboseImportLogging) continue;
                var picked = cardChoice.wasPicked ? "[picked]" : "[skipped]";
                cardChoiceNames.Add($"{GetCardDisplayName(cardId)}{picked}");
            }

            foreach (var card in playerEntry.CardsGained)
            {
                var cardId = card.Id;
                if (cardId == null || cardId == ModelId.none) continue;

                var key = cardId.ToString();
                if (cardChoiceCounts.TryGetValue(key, out var count) && count > 0)
                {
                    cardChoiceCounts[key] = count - 1;
                    continue;
                }

                cardStats.RecordSeen(cardId);
                cardStats.RecordPicked(cardId);

                if (VerboseImportLogging)
                    cardGainedNames.Add(GetCardDisplayName(cardId));
            }

            foreach (var relicChoice in
                     playerEntry.RelicChoices.Where(relicChoice => relicChoice.choice != ModelId.none))
            {
                var key = relicChoice.choice.ToString();
                if (relicChoice.wasPicked)
                    relicChoiceCounts[key] = relicChoiceCounts.GetValueOrDefault(key, 0) + 1;

                relicStats.RecordSeen(relicChoice.choice);
                if (relicChoice.wasPicked)
                    relicStats.RecordPicked(relicChoice.choice);
                if (!VerboseImportLogging) continue;
                var picked = relicChoice.wasPicked ? "[picked]" : "[skipped]";
                relicNames.Add($"{GetRelicDisplayName(relicChoice.choice)}{picked}");
            }

            foreach (var potionChoice in playerEntry.PotionChoices.Where(potionChoice =>
                         potionChoice.choice != ModelId.none))
            {
                var key = potionChoice.choice.ToString();
                if (potionChoice.wasPicked)
                    potionChoiceCounts[key] = potionChoiceCounts.GetValueOrDefault(key, 0) + 1;

                potionStats.RecordSeen(potionChoice.choice);
                if (potionChoice.wasPicked)
                    potionStats.RecordPicked(potionChoice.choice);
                if (!VerboseImportLogging) continue;
                var picked = potionChoice.wasPicked ? "[picked]" : "[skipped]";
                potionNames.Add($"{GetPotionDisplayName(potionChoice.choice)}{picked}");
            }

            foreach (var transform in playerEntry.CardsTransformed)
            {
                var finalCardId = transform.FinalCard.Id;
                if (finalCardId != null && finalCardId != ModelId.none)
                {
                    cardStats.RecordSeen(finalCardId);
                    cardStats.RecordPicked(finalCardId);
                }

                if (!VerboseImportLogging) continue;
                var originalName = transform.OriginalCard.Id != null
                    ? GetCardDisplayName(transform.OriginalCard.Id)
                    : "?";
                var finalName = finalCardId != null ? GetCardDisplayName(finalCardId) : "?";
                cardTransformedNames.Add($"{originalName} -> {finalName}");
            }

            if (VerboseImportLogging)
                cardEnchantedNames.AddRange(from enchant in playerEntry.CardsEnchanted
                    let cardName = enchant.Card.Id != null ? GetCardDisplayName(enchant.Card.Id) : "?"
                    let enchantName = enchant.Enchantment != ModelId.none
                        ? GetEnchantmentDisplayName(enchant.Enchantment)
                        : "?"
                    select $"{cardName}[{enchantName}]");

            if (VerboseImportLogging)
                cardUpgradedNames.AddRange(playerEntry.UpgradedCards.Where(id => id != ModelId.none)
                    .Select(GetCardDisplayName));

            if (VerboseImportLogging)
                cardDowngradedNames.AddRange(playerEntry.DowngradedCards.Where(id => id != ModelId.none)
                    .Select(GetCardDisplayName));

            if (VerboseImportLogging)
                foreach (var card in playerEntry.CardsRemoved)
                    if (card.Id != null && card.Id != ModelId.none)
                        cardRemovedNames.Add(GetCardDisplayName(card.Id));

            foreach (var cardId in playerEntry.BoughtColorless.Where(id => id != ModelId.none))
            {
                cardStats.RecordSeen(cardId);
                cardStats.RecordPicked(cardId);
                if (VerboseImportLogging)
                    cardBoughtColorlessNames.Add(GetCardDisplayName(cardId));
            }

            foreach (var relicId in playerEntry.BoughtRelics.Where(id => id != ModelId.none))
            {
                var key = relicId.ToString();
                if (relicChoiceCounts.TryGetValue(key, out var count) && count > 0)
                {
                    relicChoiceCounts[key] = count - 1;
                    if (VerboseImportLogging)
                        relicBoughtNames.Add(GetRelicDisplayName(relicId));
                    continue;
                }

                relicStats.RecordSeen(relicId);
                relicStats.RecordPicked(relicId);
                if (VerboseImportLogging)
                    relicBoughtNames.Add(GetRelicDisplayName(relicId));
            }

            if (VerboseImportLogging)
                relicRemovedNames.AddRange(playerEntry.RelicsRemoved.Where(id => id != ModelId.none)
                    .Select(GetRelicDisplayName));

            foreach (var potionId in playerEntry.BoughtPotions.Where(id => id != ModelId.none))
            {
                var key = potionId.ToString();
                if (potionChoiceCounts.TryGetValue(key, out var count) && count > 0)
                {
                    potionChoiceCounts[key] = count - 1;
                    if (VerboseImportLogging)
                        potionBoughtNames.Add(GetPotionDisplayName(potionId));
                    continue;
                }

                potionStats.RecordSeen(potionId);
                potionStats.RecordPicked(potionId);
                if (VerboseImportLogging)
                    potionBoughtNames.Add(GetPotionDisplayName(potionId));
            }

            if (VerboseImportLogging)
                potionUsedNames.AddRange(playerEntry.PotionUsed.Where(id => id != ModelId.none)
                    .Select(GetPotionDisplayName));

            if (VerboseImportLogging)
                potionDiscardedNames.AddRange(playerEntry.PotionDiscarded.Where(id => id != ModelId.none)
                    .Select(GetPotionDisplayName));

            foreach (var monsterId in from room in mapPoint.Rooms
                     from monsterId in room.MonsterIds
                     where monsterId != ModelId.none
                     select monsterId)
            {
                monsterStats.RecordSeen(monsterId);
                if (VerboseImportLogging)
                    monsterNames.Add(GetMonsterDisplayName(monsterId));
            }

            if (!VerboseImportLogging) return;

            if (ancientChoiceNames.Count > 0)
                logDetails.AppendLine(
                    $"      Ancient Choices ({ancientChoiceNames.Count}): {string.Join(", ", ancientChoiceNames)}");
            if (cardChoiceNames.Count > 0)
                logDetails.AppendLine(
                    $"      Card Choices ({cardChoiceNames.Count}): {string.Join(", ", cardChoiceNames)}");
            if (cardGainedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Cards Gained ({cardGainedNames.Count}): {string.Join(", ", cardGainedNames)}");
            if (cardTransformedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Cards Transformed ({cardTransformedNames.Count}): {string.Join(", ", cardTransformedNames)}");
            if (cardBoughtColorlessNames.Count > 0)
                logDetails.AppendLine(
                    $"      Bought Colorless ({cardBoughtColorlessNames.Count}): {string.Join(", ", cardBoughtColorlessNames)}");
            if (cardEnchantedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Cards Enchanted ({cardEnchantedNames.Count}): {string.Join(", ", cardEnchantedNames)}");
            if (cardUpgradedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Cards Upgraded ({cardUpgradedNames.Count}): {string.Join(", ", cardUpgradedNames)}");
            if (cardDowngradedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Cards Downgraded ({cardDowngradedNames.Count}): {string.Join(", ", cardDowngradedNames)}");
            if (cardRemovedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Cards Removed ({cardRemovedNames.Count}): {string.Join(", ", cardRemovedNames)}");
            if (relicNames.Count > 0)
                logDetails.AppendLine($"      Relics ({relicNames.Count}): {string.Join(", ", relicNames)}");
            if (relicBoughtNames.Count > 0)
                logDetails.AppendLine(
                    $"      Bought Relics ({relicBoughtNames.Count}): {string.Join(", ", relicBoughtNames)}");
            if (relicRemovedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Relics Removed ({relicRemovedNames.Count}): {string.Join(", ", relicRemovedNames)}");
            if (potionNames.Count > 0)
                logDetails.AppendLine($"      Potions ({potionNames.Count}): {string.Join(", ", potionNames)}");
            if (potionBoughtNames.Count > 0)
                logDetails.AppendLine(
                    $"      Bought Potions ({potionBoughtNames.Count}): {string.Join(", ", potionBoughtNames)}");
            if (potionUsedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Potions Used ({potionUsedNames.Count}): {string.Join(", ", potionUsedNames)}");
            if (potionDiscardedNames.Count > 0)
                logDetails.AppendLine(
                    $"      Potions Discarded ({potionDiscardedNames.Count}): {string.Join(", ", potionDiscardedNames)}");
            if (monsterNames.Count > 0)
                logDetails.AppendLine($"      Monsters ({monsterNames.Count}): {string.Join(", ", monsterNames)}");

            if (ancientChoiceNames.Count > 0 || cardChoiceNames.Count > 0 || cardGainedNames.Count > 0 ||
                cardTransformedNames.Count > 0 || cardBoughtColorlessNames.Count > 0 ||
                cardEnchantedNames.Count > 0 || cardUpgradedNames.Count > 0 || cardDowngradedNames.Count > 0 ||
                cardRemovedNames.Count > 0 || relicNames.Count > 0 || relicBoughtNames.Count > 0 ||
                relicRemovedNames.Count > 0 || potionNames.Count > 0 || potionBoughtNames.Count > 0 ||
                potionUsedNames.Count > 0 || potionDiscardedNames.Count > 0 || monsterNames.Count > 0)
                Main.Logger.Info(logDetails.ToString().TrimEnd());
        }

        public void RecordCardSeen(ModelId characterId, ModelId cardId, bool picked = false)
        {
            if (!IsInitialized || characterId == ModelId.none || cardId == ModelId.none) return;
            var stats = Data.GetOrCreateCardStats(characterId);
            stats.RecordSeen(cardId);
            if (picked) stats.RecordPicked(cardId);
        }

        public void RecordRelicSeen(ModelId characterId, ModelId relicId, bool picked = false)
        {
            if (!IsInitialized || characterId == ModelId.none || relicId == ModelId.none) return;
            var stats = Data.GetOrCreateRelicStats(characterId);
            stats.RecordSeen(relicId);
            if (picked) stats.RecordPicked(relicId);
        }

        public void RecordPotionSeen(ModelId characterId, ModelId potionId, bool picked = false)
        {
            if (!IsInitialized || characterId == ModelId.none || potionId == ModelId.none) return;
            var stats = Data.GetOrCreatePotionStats(characterId);
            stats.RecordSeen(potionId);
            if (picked) stats.RecordPicked(potionId);
        }

        public void RecordMonsterSeen(ModelId characterId, ModelId monsterId)
        {
            if (!IsInitialized || characterId == ModelId.none || monsterId == ModelId.none) return;
            var stats = Data.GetOrCreateMonsterStats(characterId);
            stats.RecordSeen(monsterId);
        }

        public void Save()
        {
            _dataSettings?.Save();
        }

        private void MigrateDataIfNeeded()
        {
            if (Data.DataVersion >= ViewedStatisticsData.CurrentDataVersion) return;
            Main.Logger.Info(
                $"Migrating data from version {Data.DataVersion} to {ViewedStatisticsData.CurrentDataVersion}");
            Data.DataVersion = ViewedStatisticsData.CurrentDataVersion;
            Save();
        }

        public (long seenTotal, long seenGrandTotal, long pickedTotal, long pickedGrandTotal) GetItemTotalCounts(
            ModelId itemId, Func<ViewedStatisticsData, Dictionary<string, CharacterItemStats>> statsSelector)
        {
            if (!IsInitialized) return (0, 0, 0, 0);

            long seenTotal = 0, seenGrandTotal = 0, pickedTotal = 0, pickedGrandTotal = 0;
            var allStats = statsSelector(Data);

            foreach (var (_, charStats) in allStats)
            {
                seenTotal += charStats.GetSeenCount(itemId);
                seenGrandTotal += charStats.TotalSeen;
                pickedTotal += charStats.GetPickedCount(itemId);
                pickedGrandTotal += charStats.TotalPicked;
            }

            return (seenTotal, seenGrandTotal, pickedTotal, pickedGrandTotal);
        }

        public (long seenTotal, long seenGrandTotal, long pickedTotal, long pickedGrandTotal) GetCardTotalCounts(
            ModelId cardId)
        {
            return GetItemTotalCounts(cardId, d => d.CardStats);
        }

        public (long seenTotal, long seenGrandTotal, long pickedTotal, long pickedGrandTotal) GetRelicTotalCounts(
            ModelId relicId)
        {
            return GetItemTotalCounts(relicId, d => d.RelicStats);
        }

        public (long seenTotal, long seenGrandTotal, long pickedTotal, long pickedGrandTotal) GetPotionTotalCounts(
            ModelId potionId)
        {
            return GetItemTotalCounts(potionId, d => d.PotionStats);
        }

        public string GetItemDetailedStatsText(ModelId itemId,
            Func<ViewedStatisticsData, Dictionary<string, CharacterItemStats>> statsSelector)
        {
            if (!IsInitialized) return "";

            var lines = new List<string>();
            var allStats = statsSelector(Data);
            var seenLabel = Main.I18N.Get("SEEN_LABEL", "Seen");
            var pickedLabel = Main.I18N.Get("PICKED_LABEL", "Picked");

            foreach (var (characterKey, charStats) in allStats)
            {
                var seenCount = charStats.GetSeenCount(itemId);
                if (seenCount <= 0) continue;

                var pickedCount = charStats.GetPickedCount(itemId);
                var characterName = GetCharacterDisplayName(characterKey);
                var pickedPct = charStats.TotalPicked > 0 ? (double)pickedCount / charStats.TotalPicked * 100 : 0;
                var seenPct = charStats.TotalSeen > 0 ? (double)seenCount / charStats.TotalSeen * 100 : 0;
                var line = $""" 
                            {characterName}:
                              {pickedLabel}: {pickedCount}/{charStats.TotalPicked} ({pickedPct:F1}%)
                              {seenLabel}: {seenCount}/{charStats.TotalSeen} ({seenPct:F1}%)
                            """;

                lines.Add(line);
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "";
        }

        public string GetCardDetailedStatsText(ModelId cardId)
        {
            return GetItemDetailedStatsText(cardId, d => d.CardStats);
        }

        public string GetRelicDetailedStatsText(ModelId relicId)
        {
            return GetItemDetailedStatsText(relicId, d => d.RelicStats);
        }

        public string GetPotionDetailedStatsText(ModelId potionId)
        {
            return GetItemDetailedStatsText(potionId, d => d.PotionStats);
        }

        private static string GetCardDisplayName(ModelId cardId)
        {
            try
            {
                return ModelDb.GetByIdOrNull<CardModel>(cardId)?.Title ?? cardId.ToString();
            }
            catch
            {
                return cardId.ToString();
            }
        }

        private static string GetRelicDisplayName(ModelId relicId)
        {
            try
            {
                return ModelDb.GetByIdOrNull<RelicModel>(relicId)?.Title.GetFormattedText() ?? relicId.ToString();
            }
            catch
            {
                return relicId.ToString();
            }
        }

        private static string GetPotionDisplayName(ModelId potionId)
        {
            try
            {
                return ModelDb.GetByIdOrNull<PotionModel>(potionId)?.Title.GetFormattedText() ?? potionId.ToString();
            }
            catch
            {
                return potionId.ToString();
            }
        }

        private static string GetMonsterDisplayName(ModelId monsterId)
        {
            try
            {
                return ModelDb.GetByIdOrNull<MonsterModel>(monsterId)?.Title.GetFormattedText() ?? monsterId.ToString();
            }
            catch
            {
                return monsterId.ToString();
            }
        }

        private static string GetEnchantmentDisplayName(ModelId enchantmentId)
        {
            try
            {
                return ModelDb.GetByIdOrNull<EnchantmentModel>(enchantmentId)?.Title.GetFormattedText() ??
                       enchantmentId.ToString();
            }
            catch
            {
                return enchantmentId.ToString();
            }
        }

        private static string GetCharacterDisplayName(string characterKey)
        {
            try
            {
                var modelId = ModelId.Deserialize(characterKey);
                var character = ModelDb.GetByIdOrNull<CharacterModel>(modelId);
                return character?.Title.GetFormattedText() ?? characterKey;
            }
            catch
            {
                return characterKey;
            }
        }

        private static ModelId FindRelicIdByTitle(string title)
        {
            try
            {
                foreach (var relic in ModelDb.AllRelics)
                    if (relic.Title.GetFormattedText() == title)
                        return relic.Id;
                return ModelId.none;
            }
            catch
            {
                return ModelId.none;
            }
        }
    }
}
