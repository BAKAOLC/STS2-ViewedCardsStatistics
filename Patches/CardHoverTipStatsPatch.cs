using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.Patching.Models;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Add viewed statistics to card hover tips in card library (only when ShowStats is enabled)
    /// </summary>
    public class CardHoverTipStatsPatch : IPatchMethod
    {
        public static string PatchId => "card_hover_tip_stats";
        public static string Description => "Add viewed statistics to card hover tips in card library";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCardHolder), "CreateHoverTips", []),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NCardHolder __instance)
        {
            try
            {
                if (!StatisticsManager.Instance.IsInitialized) return;

                var libraryGrid = FindParentOfType<NCardLibraryGrid>(__instance);
                if (libraryGrid is not { ShowStats: true }) return;

                var cardNode = __instance.CardNode;
                var cardModel = cardNode?.Model;
                if (cardModel == null) return;
                if (cardNode != null && cardNode.Visibility != ModelVisibility.Visible) return;

                var statsText = StatisticsManager.Instance.GetCardDetailedStatsText(cardModel.Id);
                if (string.IsNullOrEmpty(statsText)) return;

                var viewedLabel = Main.I18N.Get("VIEWED_STATS_LABEL", "Viewed");
                HoverTipHelper.AddTipToOwner(__instance, viewedLabel, statsText);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to add card hover tip stats: {ex.Message}");
            }
        }

        private static T? FindParentOfType<T>(Node node) where T : Node
        {
            var parent = node.GetParent();
            while (parent != null)
            {
                if (parent is T result) return result;
                parent = parent.GetParent();
            }

            return null;
        }
    }
}
