using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;
using STS2ViewedCardsStatistics.Data;

namespace STS2ViewedCardsStatistics.Patches
{
    /// <summary>
    ///     Process and save statistics data when game saves run history
    /// </summary>
    public class SaveDataPatch : IPatchMethod
    {
        public static string PatchId => "save_data";
        public static string Description => "Process and save statistics data when game saves run history";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(SaveManager), "SaveRunHistory", [typeof(RunHistory)]),
            ];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(RunHistory history)
        {
            try
            {
                StatisticsManager.ProcessAndSaveRunHistory(history);
            }
            catch (Exception ex)
            {
                Main.Logger.Warn($"Failed to process/save statistics data: {ex.Message}");
            }
        }
    }
}
