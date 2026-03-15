using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Utils.Persistence;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.UI;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Show first load popup when entering main menu if no existing data
    /// </summary>
    public class MainMenuShowPopupPatch : IPatchMethod
    {
        private static bool _initialized;
        private static NMainMenu? _currentMainMenu;
        private static IDisposable? _profileDataReadySubscription;
        private static readonly PopupLifecycleObserver LifecycleObserver = new();

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

                    _profileDataReadySubscription = RitsuLibFramework.SubscribeLifecycle(LifecycleObserver);
                }

                TryShowPopupIfNeeded(__instance);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to show first load popup: {ex.Message}");
            }
        }

        private static void TryShowPopupIfNeeded(NMainMenu mainMenu)
        {
            if (!DataReadyLifecycle.IsReady)
                return;

            if (ModDataStore.HasExistingData(ModDataStore.StatisticsKey)) return;

            mainMenu.ToSignal(mainMenu.GetTree(), SceneTree.SignalName.ProcessFrame)
                .OnCompleted(FirstLoadPopup.ShowPopup);
        }

        private sealed class PopupLifecycleObserver : ILifecycleObserver
        {
            public void OnEvent(IFrameworkLifecycleEvent evt)
            {
                if (evt is not ProfileDataReadyEvent and not ProfileDataChangedEvent)
                    return;

                if (_currentMainMenu != null && GodotObject.IsInstanceValid(_currentMainMenu))
                    TryShowPopupIfNeeded(_currentMainMenu);
            }
        }
    }
}
