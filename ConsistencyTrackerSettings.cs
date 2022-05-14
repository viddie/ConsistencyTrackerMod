using System.Collections.Generic;

namespace Celeste.Mod.ConsistencyTracker {
    public class ConsistencyTrackerSettings : EverestModuleSettings {
        public bool Enabled { get; set; } = false;

        public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.Disabled;

        public int OverlayOpacity { get; set; } = 8;
        public void CreateOverlayOpacityEntry(TextMenu menu, bool inGame) {
            var slider = new TextMenu.Slider("Overlay Opacity", i => $"{i * 10}%", 1, 10, OverlayOpacity) {
                OnValueChange = i => OverlayOpacity = i,
            };
            menu.Add(slider);
        }

        public bool PauseDeathTracking {
            get => _PauseDeathTracking;
            set {
                _PauseDeathTracking = value;
                ConsistencyTrackerModule.Instance.SaveChapterStats();
            }
        }
        private bool _PauseDeathTracking { get; set; } = false;

        public bool OnlyTrackWithGoldenBerry { get; set; } = false;
        public void CreateOnlyTrackWithGoldenBerryEntry(TextMenu menu, bool inGame) {
            var toggle = new TextMenu.OnOff("Only Track Deaths With Golden Berry", this.OnlyTrackWithGoldenBerry);
            toggle.OnValueChange = v => {
                this.OnlyTrackWithGoldenBerry = v;
            };
            menu.Add(toggle);
        }

        public bool RecordPath { get; set; } = false;
        public void CreateRecordPathEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;

            var pathRecordingToggle = new TextMenu.OnOff("Record Path", ConsistencyTrackerModule.Instance.DoRecordPath);
            pathRecordingToggle.OnValueChange = v => {
                if (v)
                    ConsistencyTrackerModule.Instance.Log($"Recording chapter path...");
                else
                    ConsistencyTrackerModule.Instance.Log($"Stopped recording path. Outputting info...");

                this.RecordPath = v;
                ConsistencyTrackerModule.Instance.DoRecordPath = v;
                ConsistencyTrackerModule.Instance.SaveChapterStats();
            };

            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Path Recording", false);
            subMenu.Add(new TextMenu.SubHeader("!!!Existing paths will be overwritten!!!"));
            subMenu.Add(pathRecordingToggle);

            subMenu.Add(new TextMenu.SubHeader("Editing the path requires a reload of the Overlay"));
            var removeRoomButton = new TextMenu.Button("Remove Current Room From Path") {
                OnPressed = ConsistencyTrackerModule.Instance.RemoveRoomFromChapter
            };
            subMenu.Add(removeRoomButton);

            menu.Add(subMenu);
        }


        public bool WipeChapter { get; set; } = false;
        public void CreateWipeChapterEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;

            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("!!Data Wipe!!", false);
            subMenu.Add(new TextMenu.SubHeader("These actions cannot be reverted!"));


            subMenu.Add(new TextMenu.SubHeader("Current Room"));
            var buttonLastAttempt = new TextMenu.Button("Remove Last Attempt");
            buttonLastAttempt.OnPressed = () => {
                ConsistencyTrackerModule.Instance.RemoveLastAttempt();
            };
            subMenu.Add(buttonLastAttempt);

            var button0 = new TextMenu.Button("Remove Last Death Streak");
            button0.OnPressed = () => {
                ConsistencyTrackerModule.Instance.RemoveLastDeathStreak();
            };
            subMenu.Add(button0);


            subMenu.Add(new TextMenu.SubHeader("Current Chapter"));
            var button1 = new TextMenu.Button("Wipe Room Data");
            button1.OnPressed = () => {
                ConsistencyTrackerModule.Instance.WipeRoomData();
            };
            subMenu.Add(button1);

            var button2 = new TextMenu.Button("Wipe Chapter Data");
            button2.OnPressed = () => {
                ConsistencyTrackerModule.Instance.WipeChapterData();
            };
            subMenu.Add(button2);

            var button3 = new TextMenu.Button("Wipe Chapter Golden Berry Deaths");
            button3.OnPressed = () => {
                ConsistencyTrackerModule.Instance.WipeChapterGoldenBerryDeaths();
            };
            subMenu.Add(button3);

            menu.Add(subMenu);
        }


        private int SelectedAttemptCount { get; set; } = 20;
        public bool CreateSummary { get; set; } = false;
        public void CreateCreateSummaryEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;

            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Tracker Summary", false);
            subMenu.Add(new TextMenu.SubHeader("Outputs some cool data of the current chapter in a readable .txt format"));


            List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                new KeyValuePair<int, string>(5, "5"),
                new KeyValuePair<int, string>(10, "10"),
                new KeyValuePair<int, string>(20, "20"),
                new KeyValuePair<int, string>(100, "100"),
            };
            TextMenuExt.EnumerableSlider<int> attemptSlider = new TextMenuExt.EnumerableSlider<int>("Summary Over X Attempts", AttemptCounts, 20);
            attemptSlider.OnValueChange = (value) => {
                SelectedAttemptCount = value;
            };
            subMenu.Add(attemptSlider);

            subMenu.Add(new TextMenu.SubHeader("When calculating the consistency stats, only the last X attempts will be counted"));

            var button1 = new TextMenu.Button("Create Chapter Summary");
            button1.OnPressed = () => {
                ConsistencyTrackerModule.Instance.CreateChapterSummary(SelectedAttemptCount);
            };
            subMenu.Add(button1);

            menu.Add(subMenu);
        }
    }

    public enum OverlayPosition {
        Disabled,
        Bottom,
        Top,
        Left,
        Right,
    }

    public static class OverlayPositionExtensions {
        public static bool IsHorizontal(this OverlayPosition self) => self == OverlayPosition.Top || self == OverlayPosition.Bottom;
        public static bool IsVertical(this OverlayPosition self) => self == OverlayPosition.Left || self == OverlayPosition.Right;
    }
}
