using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2ViewedCardsStatistics.Data;
using STS2ViewedCardsStatistics.Patches;
using STS2ViewedCardsStatistics.Patching.Core;
using STS2ViewedCardsStatistics.Utils;
using STS2ViewedCardsStatistics.Utils.Persistence.Patches;

namespace STS2ViewedCardsStatistics
{
    [ModInitializer("Initialize")]
    public static class Main
    {
        public static readonly Logger Logger = new(Const.ModId, LogType.Generic);
        private static readonly Dictionary<string, ModPatcher> Patchers = [];
        private static bool _profileServicesInitialized;
        public static I18N I18N { get; private set; } = null!;

        public static bool IsModActive { get; private set; }

        public static void Initialize()
        {
            Logger.Info($"Mod ID: {Const.ModId}");
            Logger.Info($"Version: {Const.Version}");
            Logger.Info("Initializing mod...");

            try
            {
                var frameworkPatcher = GetOrCreatePatcher("framework", "Framework-level patches");
                RegisterFrameworkPatches(frameworkPatcher);

                var mainPatcher = GetOrCreatePatcher("main", "Main patches");
                RegisterMainPatches(mainPatcher);

                var allSuccess = ApplyAllPatchers();
                if (!allSuccess)
                {
                    Logger.Error("Mod initialization failed: Critical patch(es) failed to apply");
                    Logger.Error("Mod is in a failed state and will not be active. Please check the logs for details.");
                    LogPatcherStatus();
                    IsModActive = false;
                    return;
                }

                LogPatcherStatus();

                I18N = new(
                    "ViewedCardsStatistics.I18N",
                    [$"user://mod-configs/{Const.ModId}/localization"],
                    pckFolders: ["STS2-ViewedCardsStatistics/localization"]
                );

                ModDataStore.Instance.InitializeGlobal();

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

        public static void EnsureProfileServicesInitialized()
        {
            if (_profileServicesInitialized) return;

            ModDataStore.Instance.InitializeProfileScoped();

            _profileServicesInitialized = true;
        }

        private static ModPatcher GetOrCreatePatcher(string patcherName, string description)
        {
            var patcherId = $"{Const.ModId}.{patcherName}";

            if (Patchers.TryGetValue(patcherName, out var createPatcher)) return createPatcher;

            Logger.Info($"Creating patcher: {patcherName} - {description}");
            var patcher = new ModPatcher(patcherId, Logger, patcherName);
            Patchers[patcherName] = patcher;
            return patcher;
        }

        private static bool ApplyAllPatchers()
        {
            var allSuccess = true;

            foreach (var (name, patcher) in Patchers)
            {
                Logger.Info($"Applying patcher: {name}");
                var success = patcher.PatchAll();

                if (success) continue;
                Logger.Error($"Patcher '{name}' failed to apply");
                allSuccess = false;

                Logger.Error("Rolling back all patchers due to critical failure...");
                UnpatchAll();
                break;
            }

            return allSuccess;
        }

        private static void UnpatchAll()
        {
            foreach (var (name, patcher) in Patchers)
            {
                Logger.Info($"Unpatching: {name}");
                patcher.UnpatchAll();
            }

            IsModActive = false;
        }

        private static void LogPatcherStatus()
        {
            Logger.Info("=== Patcher Status ===");
            foreach (var (name, patcher) in Patchers)
                Logger.Info(
                    $"  {name}: {patcher.AppliedPatchCount}/{patcher.RegisteredPatchCount} patches applied (Applied: {patcher.IsApplied})");
            Logger.Info("======================");
        }

        private static void RegisterFrameworkPatches(ModPatcher patcher)
        {
            patcher.RegisterPatch<ProfilePathInitializedPatch>();
            patcher.RegisterPatch<ProfileDeletePatch>();
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
