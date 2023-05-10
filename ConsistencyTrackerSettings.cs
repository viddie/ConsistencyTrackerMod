using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using Celeste.Mod.UI;
using IL.Monocle;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.ConsistencyTracker.Utility.PacePingManager;

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
        
        public bool VerboseLogging { get; set; } = false;

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
            subMenu.Add(new TextMenu.SubHeader("Path Editing"));
            bool hasPath = Mod.CurrentChapterPath != null;
            bool hasCurrentRoom = Mod.CurrentChapterPath?.CurrentRoom != null;
            
            subMenu.Add(new TextMenu.Button("Open Path Edit Tool In Browser (Coming Soon...)") {
                Disabled = true,
            });
            subMenu.Add(new TextMenu.Button("Remove Current Room From Path") {
                OnPressed = Mod.RemoveRoomFromChapterPath,
                Disabled = !hasCurrentRoom
            });
            subMenu.Add(new TextMenu.Button("Group Current And Previous Rooms") {
                OnPressed = Mod.GroupRoomsOnChapterPath,
                Disabled = !hasCurrentRoom
            });
            subMenu.Add(new TextMenu.Button("Ungroup Current From Previous Room") {
                OnPressed = Mod.UngroupRoomsOnChapterPath,
                Disabled = !hasCurrentRoom
            });
            
            bool? currentRoomIsTransition = Mod.CurrentChapterPath?.CurrentRoom?.IsNonGameplayRoom;
            //add button to toggle transition flag for room
            string buttonText = currentRoomIsTransition == null ? "Toggle Current Room As Transition" : (currentRoomIsTransition.Value ? "Set Current Room As Gameplay Room" : "Set Current Room As Transition Room");
            subMenu.Add(menuItem = new TextMenu.Button(buttonText) {
                OnPressed = () => {
                    if (Mod.CurrentChapterPath == null) return;
                    if (Mod.CurrentChapterPath.CurrentRoom == null) return;
                    Mod.CurrentChapterPath.CurrentRoom.IsNonGameplayRoom = !Mod.CurrentChapterPath.CurrentRoom.IsNonGameplayRoom;
                    Mod.SavePathToFile();
                    Mod.StatsManager.AggregateStatsPassOnce(Mod.CurrentChapterPath);
                    Mod.SaveChapterStats();//Path changed, so force a stat recalculation
                },
                Disabled = !hasCurrentRoom
            });

            //bool hasCustomName = hasCurrentRoom && Mod.CurrentChapterPath.CurrentRoom.CustomRoomName != null;
            //buttonText = hasCustomName ? $"Custom Room Name: {Mod.CurrentChapterPath.CurrentRoom.CustomRoomName}" : $"Set Custom Room Name";
            //subMenu.Add(new TextMenu.Button(buttonText) {
            //    OnPressed = () => {
            //        Mod.Log($"Starting string input scene...");
            //        Audio.Play(SFX.ui_main_savefile_rename_start);
                    
            //        Overworld overworld = menu.SceneAs<Overworld>();
            //        Mod.Log($"overworld == null {overworld == null}", isFollowup: true);
                    
            //        OuiModOptionString modOptionsString = overworld.Goto<OuiModOptionString>();
            //        Mod.Log($"modOptionsString == null {modOptionsString == null}", isFollowup: true);

            //        modOptionsString.Init<OuiModOptions>(
            //            Mod.CurrentChapterPath.CurrentRoom.GetFormattedRoomName(StatManager.RoomNameType),
            //            v => {
            //                Mod.CurrentChapterPath.CurrentRoom.CustomRoomName = v;
            //                Mod.Log($"Set custom room name of room '{Mod.CurrentChapterPath.CurrentRoom.DebugRoomName}' to '{v}'");
            //                //Mod.SavePathToFile();
            //            },
            //            30,
            //            1
            //        );
            //    },
            //    Disabled = !hasCurrentRoom
            //});

            string currentRoomCustomName = Mod.CurrentChapterPath?.CurrentRoom?.CustomRoomName;
            //Add an option to input a custom room name

            subMenu.Add(new TextMenu.SubHeader("Import / Export"));
            subMenu.Add(new TextMenu.Button("Export path to Clipboard") { 
                OnPressed = () => {
                    if (Mod.CurrentChapterPath == null) return;
                    TextInput.SetClipboardText(JsonConvert.SerializeObject(Mod.CurrentChapterPath, Formatting.Indented));
                },
                Disabled = !hasPath
            });
            subMenu.Add(menuItem = new TextMenu.Button("Import path from Clipboard").Pressed(() => {
                string text = TextInput.GetClipboardText();
                Mod.Log($"Importing path from clipboard...");
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
        [JsonIgnore]
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
        [JsonIgnore]
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
                },
                Disabled = Mod.CurrentChapterPath == null,
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

        [JsonIgnore]
        public bool LiveData { get; set; } = false;
        
        [SettingIgnore]
        public bool LiveDataFileOutputEnabled { get; set; } = false;
        public int LiveDataDecimalPlaces { get; set; } = 2;
        public int LiveDataSelectedAttemptCount { get; set; } = 20;

        [SettingIgnore]
        public RoomNameDisplayType LiveDataRoomNameDisplayType { get; set; } = RoomNameDisplayType.AbbreviationAndRoomNumberInCP;

        [SettingIgnore]
        public ListFormat LiveDataListOutputFormat { get; set; } = ListFormat.Json;

        [SettingIgnore]
        public bool LiveDataHideFormatsWithoutPath { get; set; } = false;

        [SettingIgnore]
        public bool LiveDataIgnoreUnplayedRooms { get; set; } = false;

        public void CreateLiveDataEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Live Data Settings", false);
            TextMenu.Item menuItem;

            
            subMenu.Add(new TextMenu.SubHeader($"Format Editing"));
            subMenu.Add(new TextMenu.Button("Open Format Editor In Browser") {
                OnPressed = () => {
                    string relPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "LiveDataEditTool.html");
                    string path = System.IO.Path.GetFullPath(relPath);
                    Mod.LogVerbose($"Opening format editor at '{path}'");
                    Process.Start("explorer", path);
                },
            });
            subMenu.Add(new TextMenu.Button("Open Format Text File").Pressed(() => {
                string relPath = ConsistencyTrackerModule.GetPathToFile(StatManager.BaseFolder, StatManager.FormatFileName);
                string path = System.IO.Path.GetFullPath(relPath);
                Mod.LogVerbose($"Opening format file at '{path}'");
                Process.Start("explorer", path);
            }));


            subMenu.Add(new TextMenu.SubHeader($"Settings"));
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
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.DebugRoomName, "Debug Room Name"),
            };
            if (LiveDataRoomNameDisplayType == RoomNameDisplayType.CustomRoomName) {
                LiveDataRoomNameDisplayType = RoomNameDisplayType.AbbreviationAndRoomNumberInCP;
            }
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
        [JsonIgnore]
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
                string path = System.IO.Path.GetFullPath(ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "CCTOverlay.html"));
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
                    "Custom Style 1",
                    "Custom Style 2",
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


            menu.Add(subMenu);
        }
        #endregion

        #region Ingame Overlay Settings
        [JsonIgnore]
        public bool IngameOverlay { get; set; } = false;

        //Debug Map
        [SettingIgnore]
        public bool ShowCCTRoomNamesOnDebugMap { get; set; } = true;
        [SettingIgnore]
        public bool ShowSuccessRateBordersOnDebugMap { get; set; } = false;

        //Text Overlay
        [SettingIgnore]
        public bool IngameOverlayTextEnabled { get; set; } = false;
        
        [SettingIgnore]
        public bool IngameOverlayOnlyShowInPauseMenu { get; set; } = false;
        

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

        //[SettingIgnore]
        //public int IngameOverlayText1Alpha { get; set; } = 100;

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

        //[SettingIgnore]
        //public int IngameOverlayText2Alpha { get; set; } = 100;

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

        //[SettingIgnore]
        //public int IngameOverlayText3Alpha { get; set; } = 100;

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

        //[SettingIgnore]
        //public int IngameOverlayText4Alpha { get; set; } = 100;

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

            subMenu.Add(new TextMenu.SubHeader("=== Debug Map ==="));
            subMenu.Add(new TextMenu.OnOff("Show Room Names", ShowCCTRoomNamesOnDebugMap) {
                OnValueChange = v => {
                    ShowCCTRoomNamesOnDebugMap = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff("Show Success Rate Borders", ShowSuccessRateBordersOnDebugMap) {
                OnValueChange = v => {
                    ShowSuccessRateBordersOnDebugMap = v;
                }
            });

            subMenu.Add(new TextMenu.SubHeader("=== Text Overlay ==="));
            subMenu.Add(new TextMenu.OnOff("Text Overlay Enabled", IngameOverlayTextEnabled) {
                OnValueChange = v => {
                    IngameOverlayTextEnabled = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff("Only Show Overlay In Menu", IngameOverlayOnlyShowInPauseMenu) {
                OnValueChange = v => {
                    IngameOverlayOnlyShowInPauseMenu = v;
                }
            });

            

            //Get all formats
            List<string> availableFormats = new List<string>(Mod.StatsManager.GetFormatListSorted().Select((f) => f.Name));
            List<string> availableFormatsGolden = new List<string>(availableFormats);
            string noneFormat = "<same>";
            availableFormatsGolden.Insert(0, noneFormat);
            string descAvailableFormats = $"The available formats can be changed by editing 'Celeste/ConsistencyTracker/{StatManager.BaseFolder}/{StatManager.FormatFileName}'";
            string descAvailableFormatsGolden = $"Room transition with golden required to activate. '{noneFormat}' will use same format as above.";
            string descHideWithGolden = $"Turn this on to hide this text while in a golden run";


            bool hasStats = Mod.CurrentChapterStats != null;
            bool holdingGolden = Mod.CurrentChapterStats.ModState.PlayerIsHoldingGolden;

            // ========== Text 1 ==========
            subMenu.Add(new TextMenu.SubHeader("=== Text 1 ==="));
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
                    TextSelectionHelper(hasStats, holdingGolden, 1, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format With Golden", availableFormatsGolden, IngameOverlayText1FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText1FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 1, noneFormat, v);
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
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText1Size) {
                OnValueChange = (value) => {
                    IngameOverlayText1Size = value;
                    Mod.IngameOverlay.SetTextSize(1, value);
                }
            });
            //subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Alpha", PercentageSlider(5, 5, 100), IngameOverlayText1Alpha) {
            //    OnValueChange = (value) => {
            //        IngameOverlayText1Alpha = value;
            //        Mod.IngameOverlay.SetTextAlpha(1, value);
            //    }
            //});
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


            // ========== Text 2 ==========
            subMenu.Add(new TextMenu.SubHeader("=== Text 2 ==="));
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
                    TextSelectionHelper(hasStats, holdingGolden, 2, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format with Golden", availableFormatsGolden, IngameOverlayText2FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText2FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 2, noneFormat, v);
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
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText2Size) {
                OnValueChange = (value) => {
                    IngameOverlayText2Size = value;
                    Mod.IngameOverlay.SetTextSize(2, value);
                }
            });
            //subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Alpha", PercentageSlider(5, 5, 100), IngameOverlayText2Alpha) {
            //    OnValueChange = (value) => {
            //        IngameOverlayText2Alpha = value;
            //        Mod.IngameOverlay.SetTextAlpha(2, value);
            //    }
            //});
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

            // ========== Text 3 ==========
            subMenu.Add(new TextMenu.SubHeader("=== Text 3 ==="));
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
                    TextSelectionHelper(hasStats, holdingGolden, 3, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format with Golden", availableFormatsGolden, IngameOverlayText3FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText3FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 3, noneFormat, v);
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
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText3Size) {
                OnValueChange = (value) => {
                    IngameOverlayText3Size = value;
                    Mod.IngameOverlay.SetTextSize(3, value);
                }
            });
            //subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Alpha", PercentageSlider(5, 5, 100), IngameOverlayText3Alpha) {
            //    OnValueChange = (value) => {
            //        IngameOverlayText3Alpha = value;
            //        Mod.IngameOverlay.SetTextAlpha(3, value);
            //    }
            //});
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


            // ========== Text 4 ==========
            subMenu.Add(new TextMenu.SubHeader("=== Text 4 ==="));
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
                    TextSelectionHelper(hasStats, holdingGolden, 4, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>("Selected Format with Golden", availableFormatsGolden, IngameOverlayText4FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText4FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 4, noneFormat, v);
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
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), IngameOverlayText4Size) {
                OnValueChange = (value) => {
                    IngameOverlayText4Size = value;
                    Mod.IngameOverlay.SetTextSize(4, value);
                }
            });
            //subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>("Alpha", PercentageSlider(5, 5, 100), IngameOverlayText4Alpha) {
            //    OnValueChange = (value) => {
            //        IngameOverlayText4Alpha = value;
            //        Mod.IngameOverlay.SetTextAlpha(4, value);
            //    }
            //});
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

        private void TextSelectionHelper(bool hasStats, bool holdingGolden, int textNum, string noneFormat, string selectedFormat) {
            Mod.LogVerbose($"Changed format selection of text '{textNum}' to format '{selectedFormat}'");

            string goldenFormat = null;
            switch (textNum) {
                case 1:
                    goldenFormat = IngameOverlayText1FormatGolden;
                    break;
                case 2:
                    goldenFormat = IngameOverlayText2FormatGolden;
                    break;
                case 3:
                    goldenFormat = IngameOverlayText3FormatGolden;
                    break;
                case 4:
                    goldenFormat = IngameOverlayText4FormatGolden;
                    break;
            };
            
            if (hasStats && holdingGolden && goldenFormat != noneFormat) {
                Mod.LogVerbose($"In golden run and golden format is not '{noneFormat}', not updating text");
                return;
            }
            string text = Mod.StatsManager.GetLastPassFormatText(selectedFormat);
            if (text != null) {
                Mod.IngameOverlay.SetText(textNum, text);
            }
        }

        private void GoldenTextSelectionHelper(bool hasStats, bool holdingGolden, int textNum, string noneFormat, string selectedFormat) {
            Mod.LogVerbose($"Changed golden format selection of text '{textNum}' to format '{selectedFormat}'");

            string regularFormat = null;
            switch (textNum) {
                case 1:
                    regularFormat = IngameOverlayText1Format;
                    break;
                case 2:
                    regularFormat = IngameOverlayText2Format;
                    break;
                case 3:
                    regularFormat = IngameOverlayText3Format;
                    break;
                case 4:
                    regularFormat = IngameOverlayText4Format;
                    break;
            };

            if (!hasStats || !holdingGolden) {
                Mod.LogVerbose($"Not in golden run, not updating text");
                return;
            }
            string formatName = selectedFormat == noneFormat ? regularFormat : selectedFormat;
            string text = Mod.StatsManager.GetLastPassFormatText(formatName);
            if (text != null) {
                Mod.IngameOverlay.SetText(textNum, text);
            }
        }
        #endregion

        #region Physics Inspector Settings
        [JsonIgnore]
        public bool PhysicsLoggerSettings { get; set; } = false;

        [SettingIgnore]
        public bool LogPhysicsEnabled { get; set; } = false;

        [SettingIgnore]
        public bool LogSegmentOnDeath { get; set; } = true;

        [SettingIgnore]
        public bool LogSegmentOnLoadState { get; set; } = true;

        [SettingIgnore]
        public bool LogPhysicsInputsToTasFile { get; set; } = false;

        [SettingIgnore]
        public bool LogPhysicsFlipY { get; set; } = false;

        public void CreatePhysicsLoggerSettingsEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Physics Inspector Settings", false);
            TextMenu.Item menuItem;

            subMenu.Add(new TextMenu.SubHeader($"Physics Inspector"));
            subMenu.Add(new TextMenu.Button("Open Inspector In Browser") {
                OnPressed = () => {
                    string relPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "PhysicsInspector.html");
                    string path = System.IO.Path.GetFullPath(relPath);
                    Mod.LogVerbose($"Opening physics inspector at '{path}'");
                    Process.Start("explorer", path);
                },
            });
            subMenu.Add(menuItem = new TextMenu.OnOff("Recording Physics Enabled", LogPhysicsEnabled) {
                OnValueChange = v => {
                    LogPhysicsEnabled = v;
                    Mod.Log($"Logging physics {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, "Records various physics properties, to be displayed in the physics inspector");
            subMenu.AddDescription(menu, menuItem, "Enabling this settings starts the recording, disabling it stops the recording");


            subMenu.Add(new TextMenu.SubHeader($"Recording Settings"));
            subMenu.Add(menuItem = new TextMenu.OnOff("Segment Recording On Death", LogSegmentOnDeath) {
                OnValueChange = v => {
                    LogSegmentOnDeath = v;
                    Mod.Log($"Recording segmenting on death {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, "When recording is enabled, segments the recording when the player dies.");
            subMenu.Add(menuItem = new TextMenu.OnOff("Segment Recording On Load State", LogSegmentOnLoadState) {
                OnValueChange = v => {
                    LogSegmentOnLoadState = v;
                    Mod.Log($"Recording segmenting on loading state {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, "When recording is enabled, segments the recording when the player loads a savestate.");
            subMenu.Add(menuItem = new TextMenu.OnOff("Copy TAS File To Clipboard", LogPhysicsInputsToTasFile) {
                OnValueChange = v => {
                    LogPhysicsInputsToTasFile = v;
                    Mod.Log($"Recordings inputs to tas file {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, "Will copy the inputs formatted for TAS Studio to clipboard when recording is stopped");
            subMenu.AddDescription(menu, menuItem, "Multiple buttons for one input don't work properly!");
            subMenu.Add(menuItem = new TextMenu.OnOff("Flip Y-Axis In Recording Data", LogPhysicsFlipY) {
                OnValueChange = v => {
                    LogPhysicsFlipY = v;
                    Mod.Log($"Logging physics flip y-axis {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, "Usually, negative numbers mean up in Celeste.");
            subMenu.AddDescription(menu, menuItem, "This option flips the Y-Axis so that negative numbers mean down in the data.");
            subMenu.AddDescription(menu, menuItem, "Might be useful when you want to look at the data in a different program (e.g. Excel, Google Sheet)");

            menu.Add(subMenu);
        }
        #endregion
        
        #region Pace Ping Settings
        public bool PacePing { get; set; } = false;

        [SettingIgnore]
        public bool PacePingEnabled { get; set; } = false;

        public void CreatePacePingEntry(TextMenu menu, bool inGame) {
            if (!inGame) return;

            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Pace Ping Settings", false);
            TextMenu.Item menuItem;

            bool hasPath = Mod.CurrentChapterPath != null;
            bool hasCurrentRoom = Mod.CurrentChapterPath?.CurrentRoom != null;
            PaceTiming paceTiming = null;
            if (hasCurrentRoom) { 
                paceTiming = Mod.PacePingManager.GetPaceTiming(Mod.CurrentChapterPath.ChapterSID, Mod.CurrentChapterPath.CurrentRoom.DebugRoomName);
            }

            subMenu.Add(new TextMenu.SubHeader("=== General ==="));
            subMenu.Add(new TextMenu.OnOff("Pace Pings Enabled", PacePingEnabled) {
                OnValueChange = v => {
                    PacePingEnabled = v;
                }
            });
            subMenu.Add(new TextMenu.Button("Import Default Ping Message from Clipboard") { 
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing default ping message from clipboard...");
                    try {
                        Mod.PacePingManager.SaveDefaultPingMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import default ping message from clipboard: {ex}");
                    }
                },
            });

            subMenu.Add(menuItem = new TextMenu.Button("Import Webhook URL from Clipboard").Pressed(() => {
                string text = TextInput.GetClipboardText();
                Mod.Log($"Importing webhook url from clipboard...");
                try {
                    Mod.PacePingManager.SaveDiscordWebhook(text);
                } catch (Exception ex) {
                    Mod.Log($"Couldn't import webhook url from clipboard: {ex}");
                }
            }));
            subMenu.AddDescription(menu, menuItem, "DON'T SHOW THE URL ON STREAM");


            subMenu.Add(new TextMenu.SubHeader("=== Current Room ==="));
            string toggleRoomTimingText = paceTiming == null ? "Enable Pace Ping" : "Disable Pace Ping";
            subMenu.Add(new TextMenu.Button(toggleRoomTimingText) {
                OnPressed = Mod.PacePingManager.ToggleCurrentRoomPacePing,
                Disabled = !hasCurrentRoom
            });
            subMenu.Add(new TextMenu.Button("Import Ping Message from Clipboard") {
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing custom ping message from clipboard...");
                    try {
                        Mod.PacePingManager.SaveCustomPingMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import custom ping message from clipboard: {ex}");
                    }
                },
                Disabled = paceTiming == null,
            });
            
            subMenu.Add(new TextMenu.Button("Test Pace Ping For This Room") {
                OnPressed = Mod.PacePingManager.TestPingForCurrentRoom,
                Disabled = paceTiming == null,
            });

            menu.Add(subMenu);
        }
        #endregion

        #region Tool Versions

        [SettingIgnore]
        public string OverlayVersion { get; set; }
        [SettingIgnore]
        public string LiveDataEditorVersion { get; set; }
        [SettingIgnore]
        public string PhysicsInspectorVersion { get; set; }

        #endregion

        #region Hotkeys
        public ButtonBinding ButtonToggleTextOverlayEnabled { get; set; }

        public ButtonBinding ButtonTogglePauseDeathTracking { get; set; }

        public ButtonBinding ButtonAddRoomSuccess { get; set; }

        public ButtonBinding ButtonRemoveRoomLastAttempt { get; set; }

        public ButtonBinding ButtonRemoveRoomDeathStreak { get; set; }

        public ButtonBinding ButtonToggleRecordPhysics { get; set; }

        public ButtonBinding ButtonToggleSummaryHud { get; set; }
        public ButtonBinding ButtonSummaryHudNextTab { get; set; }
        public ButtonBinding ButtonSummaryHudNextStat { get; set; }
        public ButtonBinding ButtonSummaryHudPreviousStat { get; set; }
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
