using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2ViewedCardsStatistics.Data;

namespace STS2ViewedCardsStatistics.Console
{
    public class ViewedStatsConsoleCmd : AbstractConsoleCmd
    {
        private static readonly string[] Subcommands = ["reimport", "verbose", "clear"];

        public override string CmdName => "vcs";
        public override string Args => "<reimport|verbose|clear>";
        public override string Description => "Viewed Cards Statistics: reimport/verbose [on|off]/clear";
        public override bool IsNetworked => false;
        public override bool DebugOnly => false;

        public override CmdResult Process(Player? issuingPlayer, string[] args)
        {
            if (args.Length == 0)
                return new(false, "Usage: vcs <reimport|verbose|clear>");

            var subcommand = args[0].ToLowerInvariant();

            return subcommand switch
            {
                "reimport" => ProcessReimport(),
                "verbose" => ProcessVerbose(args),
                "clear" => ProcessClear(),
                _ => new(false, $"Unknown subcommand: {subcommand}"),
            };
        }

        private static CmdResult ProcessReimport()
        {
            StatisticsManager.Instance.ClearAndReimportFromRunHistory();
            return new(true, "Statistics data reimported from run history");
        }

        private static CmdResult ProcessVerbose(string[] args)
        {
            if (args.Length < 2)
            {
                StatisticsManager.Instance.VerboseImportLogging = !StatisticsManager.Instance.VerboseImportLogging;
                var status = StatisticsManager.Instance.VerboseImportLogging ? "on" : "off";
                return new(true, $"Verbose logging toggled to: {status}");
            }

            var value = args[1].ToLowerInvariant();
            StatisticsManager.Instance.VerboseImportLogging = value is "on" or "true" or "1";
            var newStatus = StatisticsManager.Instance.VerboseImportLogging ? "on" : "off";
            return new(true, $"Verbose logging set to: {newStatus}");
        }

        private static CmdResult ProcessClear()
        {
            StatisticsManager.Instance.CreateEmptyData();
            return new(true, "Statistics data cleared");
        }

        public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
        {
            if (args.Length <= 1)
                return CompleteArgument(Subcommands, [], args.Length > 0 ? args[0] : "");

            if (args[0].ToLowerInvariant() == "verbose")
                return CompleteArgument(["on", "off"], [args[0]], args.Length > 1 ? args[1] : "");

            return base.GetArgumentCompletions(player, args);
        }
    }
}
