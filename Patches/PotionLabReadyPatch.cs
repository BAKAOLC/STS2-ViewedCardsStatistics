using Godot;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using STS2RitsuLib.Patching.Models;
using STS2ViewedCardsStatistics.Data;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Add stats label to potion lab holder on ready
    /// </summary>
    public class PotionLabReadyPatch : IPatchMethod
    {
        public static string PatchId => "potion_lab_ready";
        public static string Description => "Add stats label to potion lab holder";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NLabPotionHolder), "_Ready", []),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NLabPotionHolder __instance)
        {
            try
            {
                if (!StatisticsManager.IsInitialized) return;
                if (__instance._visibility != ModelVisibility.Visible) return;
                if (__instance._model == null) return;

                var (seenTotal, _, pickedTotal, _) =
                    StatisticsManager.Instance.GetPotionTotalCounts(__instance._model.Id);
                if (seenTotal <= 0 && pickedTotal <= 0) return;

                var text = $"{pickedTotal}/{seenTotal}";

                var label = new Label
                {
                    Name = "ViewedStatsLabel",
                    Text = text,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                label.AddThemeColorOverride("font_color", new(0.7f, 0.7f, 0.7f));
                label.AddThemeFontSizeOverride("font_size", 12);
                label.Position = new(-10, 55);
                label.Size = new(88, 20);

                __instance.AddChild(label);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to add potion stats label: {ex.Message}");
            }
        }
    }
}
