using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.Patching.Models;
using STS2ViewedCardsStatistics.UI;
using STS2ViewedCardsStatistics.Utils.Persistence;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Show first load popup when entering main menu if no existing data
    /// </summary>
    public class MainMenuShowPopupPatch : IPatchMethod
    {
        private static bool _initialized;
        private static NMainMenu? _currentMainMenu;

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
                _currentMainMenu = __instance;

                if (!_initialized)
                {
                    _initialized = true;

                    if (!StatisticsManager.IsInitialized)
                        StatisticsManager.Initialize();

                    ProfileManager.Instance.ProfileChanged += OnProfileChanged;
                }

                TryShowPopupIfNeeded(__instance);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to show first load popup: {ex.Message}");
            }
        }

        private static void OnProfileChanged(int oldProfileId, int newProfileId)
        {
            if (_currentMainMenu != null && GodotObject.IsInstanceValid(_currentMainMenu))
                TryShowPopupIfNeeded(_currentMainMenu);
        }

        private static void TryShowPopupIfNeeded(NMainMenu mainMenu)
        {
            if (StatisticsManager.HasExistingData) return;

            mainMenu.ToSignal(mainMenu.GetTree(), SceneTree.SignalName.ProcessFrame)
                .OnCompleted(FirstLoadPopup.ShowPopup);
        }
    }
}
