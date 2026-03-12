using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.Patching.Models;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Add viewed statistics to potion lab hover tips
    /// </summary>
    public class PotionLabStatsPatch : IPatchMethod
    {
        public static string PatchId => "potion_lab_stats";
        public static string Description => "Add viewed statistics to potion lab hover tips";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NLabPotionHolder), "OnFocus", []),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NLabPotionHolder __instance)
        {
            try
            {
                if (!StatisticsManager.Instance.IsInitialized) return;
                if (__instance._visibility != ModelVisibility.Visible) return;
                if (__instance._model == null) return;

                var statsText = StatisticsManager.Instance.GetPotionDetailedStatsText(__instance._model.Id);
                if (string.IsNullOrEmpty(statsText)) return;

                var viewedLabel = Main.I18N.Get("VIEWED_STATS_LABEL", "Viewed");
                HoverTipHelper.AddTipToOwner(__instance, viewedLabel, statsText);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to add potion stats hover tip: {ex.Message}");
            }
        }
    }
}
