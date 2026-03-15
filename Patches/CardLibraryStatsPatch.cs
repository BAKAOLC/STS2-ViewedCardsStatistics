using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using STS2RitsuLib.Patching.Models;
using STS2ViewedCardsStatistics.Data;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Add viewed statistics to card library stats display
    /// </summary>
    public class CardLibraryStatsPatch : IPatchMethod
    {
        public static string PatchId => "card_library_stats";
        public static string Description => "Add viewed statistics to card library stats display";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCardLibraryStats), "UpdateStats", [typeof(CardModel)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NCardLibraryStats __instance, CardModel card)
        {
            try
            {
                if (!StatisticsManager.IsInitialized) return;

                var (seenTotal, seenGrandTotal, pickedTotal, pickedGrandTotal) =
                    StatisticsManager.Instance.GetCardTotalCounts(card.Id);

                if (seenTotal <= 0 && pickedTotal <= 0) return;

                var seenLabel = Main.I18N.Get("SEEN_LABEL", "Seen");
                var pickedLabel = Main.I18N.Get("PICKED_LABEL", "Picked");

                var pickedPct = pickedGrandTotal > 0 ? (double)pickedTotal / pickedGrandTotal * 100 : 0;
                var seenPct = seenGrandTotal > 0 ? (double)seenTotal / seenGrandTotal * 100 : 0;

                __instance._label.Text = $"""
                                          {__instance._label.Text}
                                          [color=#aaaaaa]{pickedLabel}:[/color] {pickedTotal}/{pickedGrandTotal} ({pickedPct:F1}%)
                                          [color=#aaaaaa]{seenLabel}:[/color] {seenTotal}/{seenGrandTotal} ({seenPct:F1}%)
                                          """;
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to add card stats: {ex.Message}");
            }
        }
    }
}
