using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Util;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker
{
    public class ConsistencyTrackerSettings : EverestModuleSettings {

        [SettingIgnore]
        private ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        #region General Settings
        public bool Enabled {
            get => _Enabled;
            set {
                _Enabled = value;
                Mod.Log($"Mod is now {(value ? "enabled" : "disabled")}.");
                //Other hooks
                if (Mod.IngameOverlay != null) { 
                    Mod.IngameOverlay.Visible = value;
                }
            }
        }

        [SettingIgnore]
        private bool _Enabled { get; set; } = true;

        public bool PauseDeathTracking {
            get => _PauseDeathTracking;
            set {
                _PauseDeathTracking = value;
                Mod.SaveChapterStats();
            }
        }
        private bool _PauseDeathTracking { get; set; } = false;

        public bool OnlyTrackWithGoldenBerry { get; set; } = false;
        public void CreateOnlyTrackWithGoldenBerryEntry(TextMenu menu, bool inGame) {
            menu.Add(new TextMenu.OnOff("Only Track Deaths With Golden Berry", this.OnlyTrackWithGoldenBerry) {
                OnValueChange = v => {
                    this.OnlyTrackWithGoldenBerry = v;
                }
            });
        }
        #endregion

        #region Record Path Settings
        public bool RecordPath { get; set; } = false;
        public void CreateRecordPathEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;

            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Path Recording", false);

            subMenu.Add(new TextMenu.SubHeader("!!! The existing path will be overwritten !!!"));
            subMenu.Add(new TextMenu.OnOff("Record Path", Mod.DoRecordPath) {
                OnValueChange = v => {
                    if (v)
                        Mod.Log($"Recording chapter path...");
                    else
                        Mod.Log($"Stopped recording path. Outputting info...");

                    this.RecordPath = v;
                    Mod.DoRecordPath = v;
                    Mod.SaveChapterStats();
                }
            });

            TextMenu.Item menuItem;
            subMenu.Add(new TextMenu.SubHeader("Editing the path requires a reload of the external overlay"));
            subMenu.Add(new TextMenu.Button("Open Path Edit Tool In Browser (Coming Soon...)") {
                Disabled = true,
            });
            subMenu.Add(new TextMenu.Button("Remove Current Room From Path") {
                OnPressed = Mod.RemoveRoomFromChapter
            });
            subMenu.Add(new TextMenu.Button("Export path to Clipboard").Pressed(() => {
                if (Mod.CurrentChapterPath == null) return;
                TextInput.SetClipboardText(JsonConvert.SerializeObject(Mod.CurrentChapterPath, Formatting.Indented));
            }));
            subMenu.Add(menuItem = new TextMenu.Button("Import path from Clipboard").Pressed(() => {
                string text = TextInput.GetClipboardText();
                try {
                    PathInfo path = JsonConvert.DeserializeObject<PathInfo>(text);
                    Mod.SetCurrentChapterPath(path);
                    Mod.SavePathToFile();
                    Mod.SaveChapterStats();
                } catch (Exception ex) {
                    Mod.Log($"Couldn't import path from clipboard: {ex}");
                }
            }));
            subMenu.AddDescription(menu, menuItem, "!!! The existing path will be overwritten !!!");

            menu.Add(subMenu);
        }
        #endregion

        #region Data Wipe Settings
        public bool WipeChapter { get; set; } = false;
        public void CreateWipeChapterEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;

            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("!!Data Wipe!!", false);
            TextMenu.Item menuItem;
            subMenu.Add(new TextMenu.SubHeader("These actions cannot be reverted!"));


            subMenu.Add(new TextMenu.SubHeader("=== ROOM ==="));
            subMenu.Add(new TextMenu.Button("Remove Last Attempt") {
                OnPressed = () => {
                    Mod.RemoveLastAttempt();
                }
            });

            subMenu.Add(new TextMenu.Button("Remove Last Death Streak") {
                OnPressed = () => {
                    Mod.RemoveLastDeathStreak();
                }
            });

            subMenu.Add(new TextMenu.Button("Remove All Attempts") {
                OnPressed = () => {
                    Mod.WipeRoomData();
                }
            });

            subMenu.Add(new TextMenu.Button("Remove Golden Berry Deaths") {
                OnPressed = () => {
                    Mod.RemoveRoomGoldenBerryDeaths();
                }
            });


            subMenu.Add(new TextMenu.SubHeader("=== CHAPTER ==="));
            subMenu.Add(new TextMenu.Button("Reset All Attempts") {
                OnPressed = () => {
                    Mod.WipeChapterData();
                }
            });

            subMenu.Add(new TextMenu.Button("Reset All Golden Berry Deaths") {
                OnPressed = () => {
                    Mod.WipeChapterGoldenBerryDeaths();
                }
            });

            subMenu.Add(new TextMenu.SubHeader("=== LIVE-DATA ==="));
            subMenu.Add(menuItem = new TextMenu.Button($"Reset '{StatManager.FormatFileName}' file").Pressed(() => {
                Mod.StatsManager.ResetFormats();
            }));
            subMenu.AddDescription(menu, menuItem, "Resets the live-data format file back to the default values");
            subMenu.AddDescription(menu, menuItem, "This will delete all custom formats!");
            subMenu.AddDescription(menu, menuItem, "This will also generate explanations and examples for all new stats, if CCT is updated!");


            menu.Add(subMenu);
        }
        #endregion

        #region Summary Settings
        public bool CreateSummary { get; set; } = false;
        public int SummarySelectedAttemptCount { get; set; } = 20;
        public void CreateCreateSummaryEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;

            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Tracker Summary", false);
            subMenu.Add(new TextMenu.SubHeader("Outputs some cool data of the current chapter in a readable .txt format"));


            subMenu.Add(new TextMenu.SubHeader("When calculating the consistency stats, only the last X attempts will be counted"));
            List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(5, "5"),
                    new KeyValuePair<int, string>(10, "10"),
                    new KeyValuePair<int, string>(20, "20"),
                    new KeyValuePair<int, string>(100, "100"),
                };
            subMenu.Add(new TextMenuExt.EnumerableSlider<int>("Summary Over X Attempts", AttemptCounts, SummarySelectedAttemptCount) {
                OnValueChange = (value) => {
                    SummarySelectedAttemptCount = value;
                }
            });


            subMenu.Add(new TextMenu.Button("Create Chapter Summary") {
                OnPressed = () => {
                    Mod.CreateChapterSummary(SummarySelectedAttemptCount);
                }
            });

            menu.Add(subMenu);
        }
        #endregion

        #region Live Data Settings
        //Live Data Settings:
        //- Percentages digit cutoff (default: 2)
        //- Stats over X Attempts
        //- Reload format file
        //- Toggle name/abbreviation for e.g. PB Display
        public bool LiveData { get; set; } = false;
        
        [SettingIgnore]
        public bool LiveDataFileOutputEnabled { get; set; } = false;
        public int LiveDataDecimalPlaces { get; set; } = 2;
        public int LiveDataSelectedAttemptCount { get; set; } = 20;

        //Types: 1 -> EH-3 | 2 -> Event-Horizon-3
        [SettingIgnore]
        public RoomNameDisplayType LiveDataRoomNameDisplayType { get; set; } = RoomNameDisplayType.AbbreviationAndRoomNumberInCP;

        //Types: 1 -> EH-3 | 2 -> Event-Horizon-3
        [SettingIgnore]
        public ListFormat LiveDataListOutputFormat { get; set; } = ListFormat.Json;

        [SettingIgnore]
        public bool LiveDataHideFormatsWithoutPath { get; set; } = false;

        [SettingIgnore]
        public bool LiveDataIgnoreUnplayedRooms { get; set; } = false;

        public void CreateLiveDataEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Live Data Settings", false);
            TextMenu.Item menuItem;

            
            subMenu.Add(menuItem = new TextMenu.OnOff("Enable Output To Files", LiveDataFileOutputEnabled) {
                OnValueChange = (value) => {
                    LiveDataFileOutputEnabled = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Disabling this might improve performance. Ingame Overlay is unaffected by this.");
            subMenu.AddDescription(menu, menuItem, "DISABLE THIS IF YOU HAVE STUTTERS ON ROOM TRANSITION IN RECORDINGS/STREAMS.");

            List<int> DigitCounts = new List<int>() { 1, 2, 3, 4, 5 };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Max. Decimal Places", DigitCounts, LiveDataDecimalPlaces) {
                OnValueChange = (value) => {
                    LiveDataDecimalPlaces = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Floating point numbers will be rounded to this decimal.");

            List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(5, "5"),
                    new KeyValuePair<int, string>(10, "10"),
                    new KeyValuePair<int, string>(20, "20"),
                    new KeyValuePair<int, string>(100, "100"),
                };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Consider Last X Attempts", AttemptCounts, LiveDataSelectedAttemptCount) {
                OnValueChange = (value) => {
                    LiveDataSelectedAttemptCount = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "When calculating room consistency stats, only the last X attempts in each room will be counted.");

            List<KeyValuePair<int, string>> PBNameTypes = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.AbbreviationAndRoomNumberInCP, "DT-3"),
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.FullNameAndRoomNumberInCP, "Determination-3"),
                };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Room Name Format", PBNameTypes, (int)LiveDataRoomNameDisplayType) {
                OnValueChange = (value) => {
                    LiveDataRoomNameDisplayType = (RoomNameDisplayType)value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Whether you want checkpoint names to be full or abbreviated in the room name.");

            List<KeyValuePair<int, string>> ListTypes = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>((int)ListFormat.Plain, "Plain"),
                    new KeyValuePair<int, string>((int)ListFormat.Json, "JSON"),
                };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("List Output Format", ListTypes, (int)LiveDataListOutputFormat) {
                OnValueChange = (value) => {
                    LiveDataListOutputFormat = (ListFormat)value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Output format for lists. Plain is easily readable, JSON is for programming purposes.");

            subMenu.Add(menuItem = new TextMenu.OnOff("Hide Formats When No Path", LiveDataHideFormatsWithoutPath) {
                OnValueChange = v => {
                    LiveDataHideFormatsWithoutPath = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, "If a format depends on path information and no path is set, the format will be blanked out.");

            subMenu.Add(menuItem = new TextMenu.OnOff("Ignore Unplayed Rooms", LiveDataIgnoreUnplayedRooms) {
                OnValueChange = v => {
                    LiveDataIgnoreUnplayedRooms = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, "For chance calculation unplayed rooms count as 0% success rate. Toggle this on to ignore unplayed rooms.");


            subMenu.Add(new TextMenu.SubHeader($"Reload the format file, after editing 'Celeste/ConsistencyTracker/{StatManager.BaseFolder}/{StatManager.FormatFileName}'!"));
            subMenu.Add(new TextMenu.Button("Open Format Edit Tool In Browser (Coming Soon...)") {
                Disabled = true,
            });
            subMenu.Add(new TextMenu.Button("Open format file in text editor").Pressed(() => {
                string path = System.IO.Path.GetFullPath(ConsistencyTrackerModule.GetPathToFile($"{StatManager.BaseFolder}/{StatManager.FormatFileName}"));
                Process.Start("explorer", path);
            }));
            subMenu.Add(new TextMenu.Button("Reload format file") {
                OnPressed = () => {
                    Mod.StatsManager.LoadFormats();
                    Mod.SaveChapterStats();
                }
            });

            menu.Add(subMenu);
        }
        #endregion

        #region External Overlay Settings
        public bool ExternalOverlay { get; set; } = false;

        [SettingIgnore]
        public int ExternalOverlayRefreshTimeSeconds { get; set; } = 2;
        [SettingIgnore]
        public int ExternalOverlayAttemptsCount { get; set; } = 20;
        [SettingIgnore]
        public int ExternalOverlayTextOutlineSize { get; set; } = 10;
        [SettingIgnore]
        public bool ExternalOverlayColorblindMode { get; set; } = false;
        [SettingIgnore]
        public string ExternalOverlayFontFamily { get; set; } = "Renogare";

        [SettingIgnore]
        public bool ExternalOverlayTextDisplayEnabled { get; set; } = true;
        [SettingIgnore]
        public string ExternalOverlayTextDisplayPreset { get; set; } = "Default";
        [SettingIgnore]
        public bool ExternalOverlayTextDisplayLeftEnabled { get; set; } = true;
        [SettingIgnore]
        public bool ExternalOverlayTextDisplayMiddleEnabled { get; set; } = true;
        [SettingIgnore]
        public bool ExternalOverlayTextDisplayRightEnabled { get; set; } = true;

        [SettingIgnore]
        public bool ExternalOverlayRoomAttemptsDisplayEnabled { get; set; } = true;

        [SettingIgnore]
        public bool ExternalOverlayGoldenShareDisplayEnabled { get; set; } = true;
        [SettingIgnore]
        public bool ExternalOverlayGoldenShareDisplayShowSession { get; set; } = true;

        [SettingIgnore]
        public bool ExternalOverlayGoldenPBDisplayEnabled { get; set; } = true;

        [SettingIgnore]
        public bool ExternalOverlayChapterBarEnabled { get; set; } = true;
        [SettingIgnore]
        public int ExternalOverlayChapterBarLightGreenPercent { get; set; } = 95;
        [SettingIgnore]
        public int ExternalOverlayChapterBarGreenPercent { get; set; } = 80;
        [SettingIgnore]
        public int ExternalOverlayChapterBarYellowPercent { get; set; } = 50;
        [SettingIgnore]
        public int ExternalOverlayChapterBorderWidthMultiplier { get; set; } = 2;

        public void CreateExternalOverlayEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("External Overlay Settings", false);
            TextMenu.Item menuItem;

            subMenu.Add(new TextMenu.Button("Open External Overlay In Browser").Pressed(() => {
                string path = System.IO.Path.GetFullPath(ConsistencyTrackerModule.GetPathToFile($"{ConsistencyTrackerModule.ExternalOverlayFolder}/CCTOverlay.html"));
                Process.Start("explorer", path);
            }));


            subMenu.Add(new TextMenu.SubHeader("REFRESH THE PAGE / BROWSER SOURCE AFTER CHANGING THESE SETTINGS"));
            //General Settings
            subMenu.Add(new TextMenu.SubHeader("General Settings"));
            subMenu.Add(menuItem = new TextMenu.Slider("Stats Refresh Time", (i) => i == 1 ? $"1 second" : $"{i} seconds", 1, 59, ExternalOverlayRefreshTimeSeconds) {
                OnValueChange = (value) => {
                    ExternalOverlayRefreshTimeSeconds = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "The delay between two updates of the overlay.");
            List<int> attemptsList = new List<int>() { 5, 10, 20, 100 };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Consider Last X Attempts", attemptsList, ExternalOverlayAttemptsCount) {
                OnValueChange = (value) => {
                    ExternalOverlayAttemptsCount = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "When calculating room consistency stats, only the last X attempts will be used for calculation");
            subMenu.Add(new TextMenu.Slider("Text Outline Size", (i) => $"{i}px", 0, 60, ExternalOverlayTextOutlineSize) {
                OnValueChange = (value) => {
                    ExternalOverlayTextOutlineSize = value;
                }
            });
            List<string> fontList = new List<string>() {
                    "Renogare",
                    "Helvetica",
                    "Verdana",
                    "Arial",
                    "Times New Roman",
                    "Courier",
                    "Impact",
                    "Comic Sans MS",
                };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Text Font", fontList, ExternalOverlayFontFamily) {
                OnValueChange = v => {
                    ExternalOverlayFontFamily = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, "If a font doesn't show up on the overlay, you might need to install it first (just google font name lol)");
            subMenu.Add(new TextMenu.OnOff("Colorblind Mode", ExternalOverlayColorblindMode) {
                OnValueChange = v => {
                    ExternalOverlayColorblindMode = v;
                }
            });


            //Text Segment Display
            subMenu.Add(new TextMenu.SubHeader("The text stats segment at the top left / top middle / top right"));
            subMenu.Add(new TextMenu.OnOff("Text Stats Display Enabled (All)", ExternalOverlayTextDisplayEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayEnabled = v;
                }
            });
            List<string> availablePresets = new List<string>() {
                    "Default",
                    "Low Death",
                    "Golden Attempts",
                };
            subMenu.Add(new TextMenuExt.EnumerableSlider<string>("Text Stats Preset", availablePresets, ExternalOverlayTextDisplayPreset) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayPreset = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff("Text Stats Left Enabled", ExternalOverlayTextDisplayLeftEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayLeftEnabled = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff("Text Stats Middle Enabled", ExternalOverlayTextDisplayMiddleEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayMiddleEnabled = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff("Text Stats Right Enabled", ExternalOverlayTextDisplayRightEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayRightEnabled = v;
                }
            });

            //Chapter Bar
            subMenu.Add(new TextMenu.SubHeader("The bars representing the rooms and checkpoints in a map"));
            subMenu.Add(new TextMenu.OnOff("Chapter Bar Enabled", ExternalOverlayChapterBarEnabled) {
                OnValueChange = v => {
                    ExternalOverlayChapterBarEnabled = v;
                }
            });

            //subMenu.Add(new TextMenu.SubHeader($"The width of the black bars between rooms on the chapter display"));
            subMenu.Add(new TextMenuExt.IntSlider("Chapter Bar Border Width", 1, 10, ExternalOverlayChapterBorderWidthMultiplier) {
                OnValueChange = (value) => {
                    ExternalOverlayChapterBorderWidthMultiplier = value;
                }
            });

            //subMenu.Add(new TextMenu.SubHeader($"Success rate in a room to get a certain color (default: light green 95%, green 80%, yellow 50%)"));
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Light Green Percentage", PercentageSlider(), ExternalOverlayChapterBarLightGreenPercent) {
                OnValueChange = (value) => {
                    ExternalOverlayChapterBarLightGreenPercent = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Default: 95%");
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Green Percentage", PercentageSlider(), ExternalOverlayChapterBarGreenPercent) {
                OnValueChange = (value) => {
                    ExternalOverlayChapterBarGreenPercent = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Default: 80%");
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Yellow Percentage", PercentageSlider(), ExternalOverlayChapterBarYellowPercent) {
                OnValueChange = (value) => {
                    ExternalOverlayChapterBarYellowPercent = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Default: 50%");


            //Room Attempts Display
            subMenu.Add(new TextMenu.SubHeader("The red/green dots that show the last X attempts in a room"));
            subMenu.Add(new TextMenu.OnOff("Room Attempts Display Enabled", ExternalOverlayRoomAttemptsDisplayEnabled) {
                OnValueChange = v => {
                    ExternalOverlayRoomAttemptsDisplayEnabled = v;
                }
            });


            //Golden Share Display
            subMenu.Add(new TextMenu.SubHeader("The count of golden deaths per checkpoint below the chapter bar"));
            subMenu.Add(new TextMenu.OnOff("Golden Share Display Enabled", ExternalOverlayGoldenShareDisplayEnabled) {
                OnValueChange = v => {
                    ExternalOverlayGoldenShareDisplayEnabled = v;
                }
            });
            subMenu.Add(menuItem = new TextMenu.OnOff("Golden Share Show Session Deaths", ExternalOverlayGoldenShareDisplayShowSession) {
                OnValueChange = v => {
                    ExternalOverlayGoldenShareDisplayShowSession = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Shown in parenthesis after the total checkpoint death count");


            //Golden PB Display
            subMenu.Add(new TextMenu.SubHeader("The count of golden deaths per checkpoint below the chapter bar"));
            subMenu.Add(new TextMenu.OnOff("Golden PB Display Enabled", ExternalOverlayGoldenPBDisplayEnabled) {
                OnValueChange = v => {
                    ExternalOverlayGoldenPBDisplayEnabled = v;
                }
            });

            //Update Overlay/Tools buttons
            subMenu.Add(new TextMenu.SubHeader("===== Updating ====="));
            subMenu.Add(new TextMenu.Button("Update External Overlay").Pressed(() => {
                Mod.CreateExternalOverlayAndTools();
            }));


            menu.Add(subMenu);
        }
        #endregion

        #region Ingame Overlay Settings
        public bool IngameOverlay { get; set; } = false;

        [SettingIgnore]
        public bool IngameOverlayTextEnabled { get; set; } = false;

        // ======== Text 1 ========
        [SettingIgnore]
        public bool IngameOverlayText1Enabled { get; set; } = true;

        [SettingIgnore]
        public StatTextPosition IngameOverlayText1Position { get; set; } = StatTextPosition.TopLeft;

        [SettingIgnore]
        public string IngameOverlayText1Format { get; set; }

        [SettingIgnore]
        public string IngameOverlayText1FormatGolden { get; set; }
        
        [SettingIgnore]
        public bool IngameOverlayText1HideWithGolden { get; set; } = false;

        [SettingIgnore]
        public int IngameOverlayText1Size { get; set; } = 100;

        [SettingIgnore]
        public int IngameOverlayText1OffsetX { get; set; } = 5;

        [SettingIgnore]
        public int IngameOverlayText1OffsetY { get; set; } = 0;

        
        // ======== Text 2 ========
        [SettingIgnore]
        public bool IngameOverlayText2Enabled { get; set; } = true;

        [SettingIgnore]
        public StatTextPosition IngameOverlayText2Position { get; set; } = StatTextPosition.TopRight;

        [SettingIgnore]
        public string IngameOverlayText2Format { get; set; }

        [SettingIgnore]
        public string IngameOverlayText2FormatGolden { get; set; }

        [SettingIgnore]
        public bool IngameOverlayText2HideWithGolden { get; set; } = false;

        [SettingIgnore]
        public int IngameOverlayText2Size { get; set; } = 100;

        [SettingIgnore]
        public int IngameOverlayText2OffsetX { get; set; } = 5;

        [SettingIgnore]
        public int IngameOverlayText2OffsetY { get; set; } = 0;

        
        // ======== Text 3 ========
        [SettingIgnore]
        public bool IngameOverlayText3Enabled { get; set; } = false;

        [SettingIgnore]
        public StatTextPosition IngameOverlayText3Position { get; set; } = StatTextPosition.BottomLeft;

        [SettingIgnore]
        public string IngameOverlayText3Format { get; set; }

        [SettingIgnore]
        public string IngameOverlayText3FormatGolden { get; set; }

        [SettingIgnore]
        public bool IngameOverlayText3HideWithGolden { get; set; } = false;

        [SettingIgnore]
        public int IngameOverlayText3Size { get; set; } = 100;

        [SettingIgnore]
        public int IngameOverlayText3OffsetX { get; set; } = 5;

        [SettingIgnore]
        public int IngameOverlayText3OffsetY { get; set; } = 0;

        
        // ======== Text 4 ========
        [SettingIgnore]
        public bool IngameOverlayText4Enabled { get; set; } = false;

        [SettingIgnore]
        public StatTextPosition IngameOverlayText4Position { get; set; } = StatTextPosition.BottomRight;

        [SettingIgnore]
        public string IngameOverlayText4Format { get; set; }

        [SettingIgnore]
        public string IngameOverlayText4FormatGolden { get; set; }

        [SettingIgnore]
        public bool IngameOverlayText4HideWithGolden { get; set; } = false;

        [SettingIgnore]
        public int IngameOverlayText4Size { get; set; } = 100;

        [SettingIgnore]
        public int IngameOverlayText4OffsetX { get; set; } = 5;

        [SettingIgnore]
        public int IngameOverlayText4OffsetY { get; set; } = 0;

        

        //Debug Settings
        [SettingIgnore]
        public int IngameOverlayTestStyle { get; set; } = 1;

        [SettingIgnore]
        public bool IngameOverlayTextDebugPositionEnabled { get; set; } = false;

        public void CreateIngameOverlayEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;
            
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Ingame Overlay Settings", false);
            TextMenu.Item menuItem;

            subMenu.Add(new TextMenu.SubHeader("Text Overlay"));
            subMenu.Add(new TextMenu.OnOff("Text Overlay Enabled", IngameOverlayTextEnabled) {
                OnValueChange = v => {
                    IngameOverlayTextEnabled = v;
                    Mod.IngameOverlay.SetVisibility(v);
                }
            });

            //Get all formats
            List<string> availableFormats = new List<string>(Mod.StatsManager.Formats.Select((kv) => kv.Key.Name));
            List<string> availableFormatsGolden = new List<string>(availableFormats);
            string noneFormat = "<same>";
            availableFormatsGolden.Insert(0, noneFormat);
            string descAvailableFormats = $"The available formats can be changed by editing 'Celeste/ConsistencyTracker/{StatManager.BaseFolder}/{StatManager.FormatFileName}'";
            string descAvailableFormatsGolden = $"Room transition with golden required to activate. '{noneFormat}' will use same format as above.";
            string descHideWithGolden = $"Turn this on to hide this text while in a golden run";


            bool hasStats = Mod.CurrentChapterStats != null;
            bool holdingGolden = Mod.CurrentChapterStats.ModState.PlayerIsHoldingGolden;

            // ========== Text 1 ==========
            subMenu.Add(new TextMenu.SubHeader("Text 1"));
            subMenu.Add(new TextMenu.OnOff("Text 1 Enabled", IngameOverlayText1Enabled) {
                OnValueChange = v => {
                    IngameOverlayText1Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(1, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>("Position", IngameOverlayText1Position) {
                OnValueChange = v => {
                    IngameOverlayText1Position = v;
                    Mod.IngameOverlay.SetTextPosition(1, v);
                }
            });
            IngameOverlayText1Format = GetFormatOrDefault(IngameOverlayText1Format, availableFormats);
            IngameOverlayText1FormatGolden = GetFormatOrDefault(IngameOverlayText1FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format", availableFormats, IngameOverlayText1Format) {
                OnValueChange = v => {
                    IngameOverlayText1Format = v;
                    if (hasStats && holdingGolden && IngameOverlayText1FormatGolden != noneFormat) {
                        return;
                    }
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == v);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(1, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format With Golden", availableFormatsGolden, IngameOverlayText1FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText1FormatGolden = v;
                    if (!hasStats || !holdingGolden) {
                        return;
                    }
                    string formatName = v == noneFormat ? IngameOverlayText1Format : v;
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == formatName);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(1, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff("Hide In Golden Run", IngameOverlayText1HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText1HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(1, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(new TextMenuExt.IntSlider("Offset X", 0, 2000, IngameOverlayText1OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText1OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(1, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Offset Y", 0, 2000, IngameOverlayText1OffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayText1OffsetY = value;
                    Mod.IngameOverlay.SetTextOffsetY(1, value);
                }
            });
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText1Size) {
                OnValueChange = (value) => {
                    IngameOverlayText1Size = value;
                    Mod.IngameOverlay.SetTextSize(1, value);
                }
            });

            
            // ========== Text 2 ==========
            subMenu.Add(new TextMenu.SubHeader("Text 2"));
            subMenu.Add(new TextMenu.OnOff("Text 2 Enabled", IngameOverlayText2Enabled) {
                OnValueChange = v => {
                    IngameOverlayText2Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(2, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>("Position", IngameOverlayText2Position) {
                OnValueChange = v => {
                    IngameOverlayText2Position = v;
                    Mod.IngameOverlay.SetTextPosition(2, v);
                }
            });
            IngameOverlayText2Format = GetFormatOrDefault(IngameOverlayText2Format, availableFormats);
            IngameOverlayText2FormatGolden = GetFormatOrDefault(IngameOverlayText2FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format", availableFormats, IngameOverlayText2Format) {
                OnValueChange = v => {
                    IngameOverlayText2Format = v;
                    if (hasStats && holdingGolden && IngameOverlayText2FormatGolden != noneFormat) {
                        return;
                    }
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == v);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(2, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format with Golden", availableFormatsGolden, IngameOverlayText2FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText2FormatGolden = v;
                    if (!hasStats || !holdingGolden) {
                        return;
                    }
                    string formatName = v == noneFormat ? IngameOverlayText2Format : v;
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == formatName);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(2, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff("Hide In Golden Run", IngameOverlayText2HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText2HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(2, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(new TextMenuExt.IntSlider("Offset X", 0, 2000, IngameOverlayText2OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText2OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(2, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Offset Y", 0, 2000, IngameOverlayText2OffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayText2OffsetY = value;
                    Mod.IngameOverlay.SetTextOffsetY(2, value);
                }
            });
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText2Size) {
                OnValueChange = (value) => {
                    IngameOverlayText2Size = value;
                    Mod.IngameOverlay.SetTextSize(2, value);
                }
            });
            
            // ========== Text 3 ==========
            subMenu.Add(new TextMenu.SubHeader("Text 3"));
            subMenu.Add(new TextMenu.OnOff("Text 3 Enabled", IngameOverlayText3Enabled) {
                OnValueChange = v => {
                    IngameOverlayText3Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(3, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>("Position", IngameOverlayText3Position) {
                OnValueChange = v => {
                    IngameOverlayText3Position = v;
                    Mod.IngameOverlay.SetTextPosition(3, v);
                }
            });
            IngameOverlayText3Format = GetFormatOrDefault(IngameOverlayText3Format, availableFormats);
            IngameOverlayText3FormatGolden = GetFormatOrDefault(IngameOverlayText3FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format", availableFormats, IngameOverlayText3Format) {
                OnValueChange = v => {
                    IngameOverlayText3Format = v;
                    if (hasStats && holdingGolden && IngameOverlayText3FormatGolden != noneFormat) {
                        return;
                    }
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == v);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(3, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format with Golden", availableFormatsGolden, IngameOverlayText3FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText3FormatGolden = v;
                    if (!hasStats || !holdingGolden) {
                        return;
                    }
                    string formatName = v == noneFormat ? IngameOverlayText3Format : v;
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == formatName);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(3, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff("Hide In Golden Run", IngameOverlayText3HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText3HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(3, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(new TextMenuExt.IntSlider("Offset X", 0, 2000, IngameOverlayText3OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText3OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(3, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Offset Y", 0, 2000, IngameOverlayText3OffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayText3OffsetY = value;
                    Mod.IngameOverlay.SetTextOffsetY(3, value);
                }
            });
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText3Size) {
                OnValueChange = (value) => {
                    IngameOverlayText3Size = value;
                    Mod.IngameOverlay.SetTextSize(3, value);
                }
            });

            
            // ========== Text 4 ==========
            subMenu.Add(new TextMenu.SubHeader("Text 4"));
            subMenu.Add(new TextMenu.OnOff("Text 4 Enabled", IngameOverlayText4Enabled) {
                OnValueChange = v => {
                    IngameOverlayText4Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(4, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>("Position", IngameOverlayText4Position) {
                OnValueChange = v => {
                    IngameOverlayText4Position = v;
                    Mod.IngameOverlay.SetTextPosition(4, v);
                }
            });
            IngameOverlayText4Format = GetFormatOrDefault(IngameOverlayText4Format, availableFormats);
            IngameOverlayText4FormatGolden = GetFormatOrDefault(IngameOverlayText4FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format", availableFormats, IngameOverlayText4Format) {
                OnValueChange = v => {
                    IngameOverlayText4Format = v;
                    if (hasStats && holdingGolden && IngameOverlayText4FormatGolden != noneFormat) {
                        return;
                    }
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == v);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(4, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format with Golden", availableFormatsGolden, IngameOverlayText4FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText4FormatGolden = v;
                    if (!hasStats || !holdingGolden) {
                        return;
                    }
                    string formatName = v == noneFormat ? IngameOverlayText4Format : v;
                    KeyValuePair<StatFormat, string> stat = Mod.StatsManager.LastResults.FirstOrDefault((kv) => kv.Key.Name == formatName);
                    if (stat.Key != null) {
                        Mod.IngameOverlay.SetText(4, stat.Value);
                    }
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff("Hide In Golden Run", IngameOverlayText4HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText4HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(4, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(new TextMenuExt.IntSlider("Offset X", 0, 2000, IngameOverlayText4OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText4OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(4, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Offset Y", 0, 2000, IngameOverlayText4OffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayText4OffsetY = value;
                    Mod.IngameOverlay.SetTextOffsetY(4, value);
                }
            });
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText4Size) {
                OnValueChange = (value) => {
                    IngameOverlayText4Size = value;
                    Mod.IngameOverlay.SetTextSize(4, value);
                }
            });

            

            //subMenu.Add(new TextMenu.SubHeader("[Developement Only] Debug Features"));
            //subMenu.Add(new TextMenu.OnOff("Text Overlay Debug Position", IngameOverlayTextDebugPositionEnabled) {
            //    OnValueChange = v => {
            //        IngameOverlayTextDebugPositionEnabled = v;
            //        Mod.IngameOverlay.GetStatText(1).DebugShowPosition = v;
            //        Mod.IngameOverlay.GetStatText(2).DebugShowPosition = v;
            //        Mod.IngameOverlay.GetStatText(3).DebugShowPosition = v;
            //        Mod.IngameOverlay.GetStatText(4).DebugShowPosition = v;
            //    }
            //});

            menu.Add(subMenu);
        }
        #endregion

        #region Hotkeys
        public ButtonBinding ButtonTogglePauseDeathTracking { get; set; }
        
        public ButtonBinding ButtonToggleTextOverlayEnabled { get; set; }
        #endregion

        #region Helpers
        private List<KeyValuePair<int, string>> PercentageSlider(int stepSize = 5, int min = 0, int max = 100) {
            List<KeyValuePair<int, string>> toRet = new List<KeyValuePair<int, string>>();

            for (int i = min; i <= max; i += stepSize) {
                toRet.Add(new KeyValuePair<int, string>(i, $"{i}%"));
            }

            return toRet;
        }

        private string GetFormatOrDefault(string formatName, List<string> availableFormats) {
            if (formatName == null || !availableFormats.Contains(formatName)) {
                return availableFormats[0];
            }

            return formatName;
        }
        #endregion
    }
}
