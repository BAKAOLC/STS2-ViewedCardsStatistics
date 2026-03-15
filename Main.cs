using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Utils;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.Patches;

namespace STS2ViewedCardsStatistics
{
    [ModInitializer(nameof(Initialize))]
    public static class Main
    {
        public static readonly Logger Logger = RitsuLibFramework.CreateLogger(Const.ModId);
        public static I18N I18N { get; private set; } = null!;

        public static bool IsModActive { get; private set; }

        public static void Initialize()
        {
            Logger.Info($"Mod ID: {Const.ModId}");
            Logger.Info($"Version: {Const.Version}");
            Logger.Info("Initializing mod...");

            try
            {
                var patcher = RitsuLibFramework.CreatePatcher(Const.ModId, "main");
                RegisterMainPatches(patcher);

                if (!RitsuLibFramework.ApplyRequiredPatcher(patcher, () => IsModActive = false))
                {
                    Logger.Error("Mod initialization failed: Critical patch(es) failed to apply");
                    return;
                }

                I18N = RitsuLibFramework.CreateLocalization(
                    "ViewedCardsStatistics.I18N",
                    resourceFolders: ["STS2ViewedCardsStatistics.localization"]
                );

                ModDataStore.Initialize();
                IsModActive = true;
                Logger.Info("Mod initialization complete - Mod is now ACTIVE");
            }
            catch (Exception ex)
            {
                Logger.Error($"Mod initialization failed with exception: {ex.Message}");
                Logger.Error($"Stack trace: {ex.StackTrace}");
                IsModActive = false;
            }
        }

        private static void RegisterMainPatches(ModPatcher patcher)
        {
            patcher.RegisterPatch<CardLibraryStatsPatch>();
            patcher.RegisterPatch<CardHoverTipStatsPatch>();
            patcher.RegisterPatch<RelicCollectionStatsPatch>();
            patcher.RegisterPatch<RelicCollectionReadyPatch>();
            patcher.RegisterPatch<PotionLabStatsPatch>();
            patcher.RegisterPatch<PotionLabReadyPatch>();
            patcher.RegisterPatch<SaveDataPatch>();
            patcher.RegisterPatch<MainMenuShowPopupPatch>();
        }
    }
}
