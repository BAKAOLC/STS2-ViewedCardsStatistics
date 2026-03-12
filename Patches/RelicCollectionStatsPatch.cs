using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.Patching.Models;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Add viewed statistics to relic collection hover tips
    /// </summary>
    public class RelicCollectionStatsPatch : IPatchMethod
    {
        public static string PatchId => "relic_collection_stats";
        public static string Description => "Add viewed statistics to relic collection hover tips";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NRelicCollectionEntry), "OnFocus", []),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NRelicCollectionEntry __instance)
        {
            try
            {
                if (!StatisticsManager.Instance.IsInitialized) return;
                if (__instance.ModelVisibility != ModelVisibility.Visible) return;

                var relic = __instance.relic;
                if (relic == null) return;

                var statsText = StatisticsManager.Instance.GetRelicDetailedStatsText(relic.Id);
                if (string.IsNullOrEmpty(statsText)) return;

                var viewedLabel = Main.I18N.Get("VIEWED_STATS_LABEL", "Viewed");
                HoverTipHelper.AddTipToOwner(__instance, viewedLabel, statsText);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to add relic stats hover tip: {ex.Message}");
            }
        }
    }
}
