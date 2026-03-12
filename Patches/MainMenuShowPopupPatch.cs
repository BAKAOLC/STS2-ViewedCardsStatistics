using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.Patching.Models;
using STS2ViewedCardsStatistics.UI;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Show first load popup when entering main menu if no existing data
    /// </summary>
    public class MainMenuShowPopupPatch : IPatchMethod
    {
        private static bool _hasShownPopup;

        public static string PatchId => "main_menu_show_popup";
        public static string Description => "Show first load popup when entering main menu";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NMainMenu), "_Ready")];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NMainMenu __instance)
        {
            try
            {
                if (_hasShownPopup) return;
                _hasShownPopup = true;

                if (!StatisticsManager.Instance.IsInitialized)
                    StatisticsManager.Instance.Initialize();

                if (!StatisticsManager.Instance.HasExistingData)
                    __instance.ToSignal(__instance.GetTree(), SceneTree.SignalName.ProcessFrame)
                        .OnCompleted(FirstLoadPopup.ShowPopup);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to show first load popup: {ex.Message}");
            }
        }
    }
}
