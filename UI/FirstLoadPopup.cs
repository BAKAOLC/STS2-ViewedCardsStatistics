using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using STS2ViewedCardsStatistics.Data;

namespace STS2ViewedCardsStatistics.UI
{
    /// <summary>
    ///     First load popup that asks whether to import run history
    /// </summary>
    public partial class FirstLoadPopup : Control, IScreenContext
    {
        private static readonly string ScenePath = SceneHelper.GetScenePath("ui/vertical_popup");

        private NVerticalPopup? _verticalPopup;

        public Control? DefaultFocusedControl => _verticalPopup?.YesButton;

        public static void ShowPopup()
        {
            try
            {
                var scene = PreloadManager.Cache.GetScene(ScenePath);
                var verticalPopup = scene.Instantiate<NVerticalPopup>();

                var wrapper = new FirstLoadPopup();
                wrapper._verticalPopup = verticalPopup;
                wrapper.AddChild(verticalPopup);

                NModalContainer.Instance?.Add(wrapper);
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Failed to show FirstLoadPopup: {ex.Message}");
                StatisticsManager.CreateEmptyData();
            }
        }

        public override void _Ready()
        {
            if (_verticalPopup == null) return;

            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

            var title = Main.I18N.Get("FIRST_LOAD_TITLE", "Import Run History?");
            var description = Main.I18N.Get("FIRST_LOAD_DESCRIPTION",
                "This is the first time loading Viewed Cards Statistics mod.\n\n" +
                "Would you like to analyze your existing run history to import statistics data?\n\n" +
                "This may take a moment depending on how many runs you have.");
            var yesText = Main.I18N.Get("FIRST_LOAD_YES", "Yes, Import");
            var noText = Main.I18N.Get("FIRST_LOAD_NO", "No, Start Fresh");

            _verticalPopup.SetText(title, description);

            _verticalPopup.YesButton.IsYes = true;
            _verticalPopup.YesButton.SetText(yesText);
            _verticalPopup.YesButton.Released += OnYesPressed;

            _verticalPopup.NoButton.Visible = true;
            _verticalPopup.NoButton.IsYes = false;
            _verticalPopup.NoButton.SetText(noText);
            _verticalPopup.NoButton.Released += OnNoPressed;
        }

        private static void OnYesPressed(NClickableControl _)
        {
            Main.Logger.Info("User chose to import run history");
            StatisticsManager.ImportFromRunHistory();
            NModalContainer.Instance?.Clear();
        }

        private static void OnNoPressed(NClickableControl _)
        {
            Main.Logger.Info("User chose to start fresh");
            StatisticsManager.CreateEmptyData();
            NModalContainer.Instance?.Clear();
        }
    }
}
