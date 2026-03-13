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
        private static bool _popupCheckInProgress;
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

                    ProfileManager.Instance.ProfileChanged += OnProfileChanged;
                    DataReadyLifecycle.DataReady += OnDataReady;
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

        private static void OnDataReady(int profileId, string source)
        {
            if (_currentMainMenu != null && GodotObject.IsInstanceValid(_currentMainMenu))
                TryShowPopupIfNeeded(_currentMainMenu);
        }

        private static void TryShowPopupIfNeeded(NMainMenu mainMenu)
        {
            if (_popupCheckInProgress) return;

            if (!DataReadyLifecycle.IsReady)
            {
                _popupCheckInProgress = true;
                DataReadyLifecycle.RunWhenReady(_ =>
                {
                    _popupCheckInProgress = false;
                    if (GodotObject.IsInstanceValid(mainMenu))
                        TryShowPopupIfNeeded(mainMenu);
                });

                return;
            }

            if (ModDataStore.Instance.HasExistingData(ModDataStore.StatisticsKey)) return;

            mainMenu.ToSignal(mainMenu.GetTree(), SceneTree.SignalName.ProcessFrame)
                .OnCompleted(FirstLoadPopup.ShowPopup);
        }
    }
}
