﻿using Celeste.Mod.ConsistencyTracker.Entities;
using Celeste.Mod.ConsistencyTracker.Entities.Menu;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil;
using Monocle;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.ConsistencyTracker.Entities.WidgetLayout;
using static Celeste.Mod.ConsistencyTracker.Utility.PacePingManager;

namespace Celeste.Mod.ConsistencyTracker
{
    public class ConsistencyTrackerSettings : EverestModuleSettings {

        [SettingIgnore]
        private ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        #region General Settings
        //public bool Enabled {
        //    get => _Enabled;
        //    set {
        //        _Enabled = value;
        //        Mod.Log($"Mod is now {(value ? "enabled" : "disabled")}.");
        //        //Other hooks
        //        if (Mod.IngameOverlay != null) { 
        //            Mod.IngameOverlay.Visible = value;
        //        }
        //    }
        //}

        //[SettingIgnore]
        //private bool _Enabled { get; set; } = true;

        public bool PauseDeathTracking {
            get => _PauseDeathTracking;
            set {
                _PauseDeathTracking = value;
                Mod.SaveChapterStats();
            }
        }
        private bool _PauseDeathTracking { get; set; } = false;

        [SettingIgnore]
        public string DataRootFolderLocation { get; set; } = null;

        #endregion

        #region Tracking Settings
        [JsonIgnore]
        public bool TrackingSettings { get; set; } = false;

        [SettingIgnore]
        public bool TrackingOnlyWithGoldenBerry { get; set; } = false;
        [SettingIgnore]
        public bool TrackingAlwaysGoldenDeaths { get; set; } = true;
        [SettingIgnore]
        public bool TrackingSaveStateCountsForGoldenDeath { get; set; } = true;
        [SettingIgnore]
        public bool TrackingRestartChapterCountsForGoldenDeath { get; set; } = true;
        [SettingIgnore]
        public bool TrackNegativeStreaks { get; set; } = true;
        [SettingIgnore]
        public bool VerboseLogging { get; set; } = false;
        public void CreateTrackingSettingsEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_TITLE"), false);
            TextMenu.Item menuItem;

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_ONLY_TRACK_DEATHS_WITH_GOLDEN_BERRY"), TrackingOnlyWithGoldenBerry) {
                OnValueChange = v => {
                    TrackingOnlyWithGoldenBerry = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_ONLY_TRACK_DEATHS_WITH_GOLDEN_BERRY_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_ONLY_TRACK_DEATHS_WITH_GOLDEN_BERRY_HINT_2"));

            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_ALWAYS_TRACK_GOLDEN_DEATHS"), TrackingAlwaysGoldenDeaths) {
                OnValueChange = v => {
                    TrackingAlwaysGoldenDeaths = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_ALWAYS_TRACK_GOLDEN_DEATHS_HINT"));

            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_COUNT_GOLDEN_DEATH_WHEN_LOADING_SAVESTATE"), TrackingSaveStateCountsForGoldenDeath) {
                OnValueChange = v => {
                    TrackingSaveStateCountsForGoldenDeath = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_COUNT_GOLDEN_DEATH_WHEN_LOADING_SAVESTATE_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_COUNT_GOLDEN_DEATH_WHEN_LOADING_SAVESTATE_HINT_2"));

            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_COUNT_GOLDEN_DEATH_WHEN_RESTARTING_CHAPTER"), TrackingRestartChapterCountsForGoldenDeath) {
                OnValueChange = v => {
                    TrackingRestartChapterCountsForGoldenDeath = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_GENERAL_COUNT_GOLDEN_DEATH_WHEN_RESTARTING_CHAPTER_HINT"));

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_STATS_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_STATS_TRACK_NEGATIVE_STREAKS"), TrackNegativeStreaks) {
                OnValueChange = v => {
                    TrackNegativeStreaks = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_STATS_HINT"));

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OTHER_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OTHER_VERBOSE_LOGGING"), VerboseLogging) {
                OnValueChange = v => {
                    VerboseLogging = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OTHER_HINT"));

            menu.Add(subMenu);
        }
        #endregion

        #region Record Path Settings
        public bool RecordPath { get; set; } = false;
        [SettingIgnore]
        public bool CustomRoomNameAllSegments { get; set; } = true;
        
        public void CreateRecordPathEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_TITLE"), false);
            TextMenu.Item menuItem;

            if (!inGame) {
                subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_NOT_IN_GAME_HINT"), false));
                menu.Add(subMenu);
                return;
            }

            
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_TITLE")} ==="));
            bool hasPathList = Mod.CurrentChapterPathSegmentList != null;
            int segmentCount = hasPathList ? Mod.CurrentChapterPathSegmentList.Segments.Count : 0;
            List<KeyValuePair<int, string>> SegmentList = new List<KeyValuePair<int, string>>() { 
                new KeyValuePair<int, string>(0, "Default"),
            };
            if (hasPathList) {
                SegmentList.Clear();
                for (int i = 0; i < Mod.CurrentChapterPathSegmentList.Segments.Count; i++) {
                    PathSegment segment = Mod.CurrentChapterPathSegmentList.Segments[i];
                    SegmentList.Add(new KeyValuePair<int, string>(i, segment.Name));
                }
            }
            TextMenuExt.EnumerableSlider<int> sliderCurrentSegment = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_CURRENT_SEGMENT"), SegmentList, Mod.SelectedPathSegmentIndex) {
                OnValueChange = (newValue) => {
                    Mod.SetCurrentChapterPathSegment(newValue);
                },
                Disabled = !hasPathList
            };
            subMenu.Add(sliderCurrentSegment);
            subMenu.AddDescription(menu, sliderCurrentSegment, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_CURRENT_SEGMENT_HINT"));
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_ADD_SEGMENT")) {
                OnPressed = () => {
                    PathSegment segment = Mod.AddCurrentChapterPathSegment();
                    if (segment != null) {
                        sliderCurrentSegment.Values.Add(Tuple.Create(segment.Name, Mod.CurrentChapterPathSegmentList.Segments.Count - 1));
                        sliderCurrentSegment.SelectWiggler.Start();
                    }
                },
                Disabled = !hasPathList
            });
            
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_IMPORT_SEGMENT")) { 
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing segment name from clipboard...");
                    try {
                        bool renamed = Mod.SetCurrentChapterPathSegmentName(text);
                        if (renamed) {
                            sliderCurrentSegment.Values[Mod.SelectedPathSegmentIndex] = Tuple.Create(text, Mod.SelectedPathSegmentIndex);
                            sliderCurrentSegment.SelectWiggler.Start();
                        }
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import segment name from clipboard: {ex}");
                    }
                },
            });

            
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_TITLE")} ==="));
            ColoredButton startPathRecordingButton = new ColoredButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_START")) {
                HighlightColor = Color.Yellow,
                Disabled = Mod.DoRecordPath,
            };
            string recorderStateTitle = Mod.DoRecordPath ? $"{Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_ON")}\n-----\n{Mod.PathRec.GetRecorderStatus()}" : Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OFF");
            TextMenu.SubHeader recorderStateHeader = new TextMenu.SubHeader($"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {recorderStateTitle}", topPadding:false);
            DoubleConfirmButton savePathRecordingButton = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_SAVE")) {
                HighlightColor = Color.Yellow,
                Disabled = !Mod.DoRecordPath,
            };
            DoubleConfirmButton abortPathRecordingButton = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_ABORT")) {
                HighlightColor = Color.Red,
                Disabled = !Mod.DoRecordPath,
            };

            startPathRecordingButton.OnPressed = () => {
                Mod.Log($"Started path recorder...");
                Mod.DoRecordPath = true;
                Mod.SaveChapterStats();
                
                startPathRecordingButton.Disabled = true;
                savePathRecordingButton.Disabled = false;
                abortPathRecordingButton.Disabled = false;

                recorderStateHeader.Title = $"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_ON")}";
            };
            savePathRecordingButton.OnDoubleConfirmation = () => {
                Mod.Log($"Saving path...");
                Mod.DoRecordPath = false;

                startPathRecordingButton.Disabled = false;
                savePathRecordingButton.Disabled = true;
                abortPathRecordingButton.Disabled = true;

                recorderStateHeader.Title = $"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OFF")}";
            };
            abortPathRecordingButton.OnDoubleConfirmation = () => {
                Mod.Log($"Aborting path recording...");
                Mod.AbortPathRecording = true;
                Mod.DoRecordPath = false;

                startPathRecordingButton.Disabled = false;
                savePathRecordingButton.Disabled = true;
                abortPathRecordingButton.Disabled = true;

                recorderStateHeader.Title = $"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OFF")}";
            };

            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_TITLE_HINT_1"), false));
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_TITLE_HINT_2"), false));

            subMenu.Add(startPathRecordingButton);
            subMenu.Add(savePathRecordingButton);
            subMenu.AddDescription(menu, savePathRecordingButton, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_SAVE_HINT"));
            subMenu.Add(abortPathRecordingButton);
            subMenu.AddDescription(menu, abortPathRecordingButton, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_ABORT_HINT"));
            subMenu.Add(recorderStateHeader);



            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_TITLE")} ==="));
            bool hasPath = Mod.CurrentChapterPath != null;
            bool hasCurrentRoom = Mod.CurrentChapterPath?.CurrentRoom != null;
            
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_BROWSER")) {
                Disabled = true,
            });
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_REMOVE_CURRENT_ROOM")) {
                OnPressed = Mod.RemoveRoomFromChapterPath,
                Disabled = !hasCurrentRoom
            });
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_GROUP")) {
                OnPressed = Mod.GroupRoomsOnChapterPath,
                Disabled = !hasCurrentRoom
            });
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_UNGROUP")) {
                OnPressed = Mod.UngroupRoomsOnChapterPath,
                Disabled = !hasCurrentRoom
            });
            
            bool? currentRoomIsTransition = Mod.CurrentChapterPath?.CurrentRoom?.IsNonGameplayRoom;
            List<KeyValuePair<bool, string>> RoomType = new List<KeyValuePair<bool, string>>() {
                    new KeyValuePair<bool, string>(false, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_ROOM_TYPE_GAMEPLAY")),
                    new KeyValuePair<bool, string>(true, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_ROOM_TYPE_TRANSITION")),
            };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<bool>(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_ROOM_TYPE_TITLE"), RoomType, currentRoomIsTransition ?? false) {
                OnValueChange = (newValue) => {
                    if (Mod.CurrentChapterPath == null) return;
                    if (Mod.CurrentChapterPath.CurrentRoom == null) return;
                    Mod.CurrentChapterPath.CurrentRoom.IsNonGameplayRoom = newValue;
                    Mod.SavePathToFile();
                    Mod.StatsManager.AggregateStatsPassOnce(Mod.CurrentChapterPath);
                    Mod.SaveChapterStats();//Path changed, so force a stat recalculation
                },
                Disabled = !hasCurrentRoom
            });

            string currentRoomCustomName = Mod.CurrentChapterPath?.CurrentRoom?.CustomRoomName;
            
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_IMPORT_CLIPBOARD")) { 
                OnPressed = () => {
                    string text = TextInput.GetClipboardText().Trim();
                    Mod.Log($"Importing custom room name from clipboard...");
                    try {
                        if (Mod.CurrentChapterPath == null) return;
                        if (Mod.CurrentChapterPath.CurrentRoom == null) return;

                        if (string.IsNullOrEmpty(text)) text = null;

                        string currentRoomName = Mod.CurrentChapterPath.CurrentRoom.DebugRoomName;

                        int count = 0;
                        if (CustomRoomNameAllSegments) { //Find all other rooms with same name in other segments 
                            foreach (PathSegment segment in Mod.CurrentChapterPathSegmentList.Segments) {
                                foreach (CheckpointInfo cpInfo in segment.Path.Checkpoints) {
                                    foreach (RoomInfo rInfo in cpInfo.Rooms) {
                                        if (rInfo.DebugRoomName == currentRoomName) {
                                            rInfo.CustomRoomName = text;
                                            count++;
                                        }
                                    }
                                }
                            }
                        } else {
                            Mod.CurrentChapterPath.CurrentRoom.CustomRoomName = text;
                            count++;
                        }
                        
                        Mod.Log($"Set custom room name of room '{Mod.CurrentChapterPath.CurrentRoom.DebugRoomName}' to '{text}' (Count: {count})");
                        Mod.SavePathToFile();
                        Mod.SaveChapterStats();//Recalc stats
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import custom room name from clipboard: {ex}");
                    }
                },
                Disabled = !hasCurrentRoom
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_IMPORT_CLIPBOARD_HINT"));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_CUSTOM_NAME"), CustomRoomNameAllSegments) {
                OnValueChange = (value) => {
                    CustomRoomNameAllSegments = value;
                },
                Disabled = !hasCurrentRoom
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_CUSTOM_NAME_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_CUSTOM_NAME_HINT_2"));


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_IO_TITLE")} ==="));
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_IO_EXPORT_CLIPBOARD")) { 
                OnPressed = () => {
                    if (Mod.CurrentChapterPath == null) return;
                    TextInput.SetClipboardText(JsonConvert.SerializeObject(Mod.CurrentChapterPath, Formatting.Indented));
                },
                Disabled = !hasPath
            });
            subMenu.Add(menuItem = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_IO_IMPORT_CLIPBOARD")) { 
                OnDoubleConfirmation = () => {
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
                },
                HighlightColor = Color.Yellow,
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_IO_IMPORT_CLIPBOARD_HINT"));

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_DANGER_ZONE_TITLE")} ==="));
            DoubleConfirmButton deleteButton = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_DANGER_ZONE_DELETE")) {
                Disabled = !hasPathList || segmentCount <= 1,
                HighlightColor = Color.Red,
            };
            deleteButton.OnDoubleConfirmation = () => {
                int index = Mod.SelectedPathSegmentIndex;
                bool didDelete = Mod.DeleteCurrentChapterPathSegment();

                if (didDelete) {
                    sliderCurrentSegment.Values.RemoveAt(index);
                    sliderCurrentSegment.Index = Mod.SelectedPathSegmentIndex;
                    sliderCurrentSegment.SelectWiggler.Start();
                }

                deleteButton.Disabled = Mod.CurrentChapterPathSegmentList.Segments.Count <= 1;
            };
            subMenu.Add(deleteButton);
            subMenu.AddDescription(menu, deleteButton, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_DANGER_ZONE_DELETE_HINT_1"));
            subMenu.AddDescription(menu, deleteButton, Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_DANGER_ZONE_DELETE_HINT_2"));

            menu.Add(subMenu);
        }
        #endregion

        #region Data Wipe Settings
        [JsonIgnore]
        public bool WipeChapter { get; set; } = false;
        public void CreateWipeChapterEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_TITLE"), false);
            TextMenu.Item menuItem;

            if (!inGame) {
                subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_NOT_IN_GAME_HINT"), false));
                menu.Add(subMenu);
                return;
            }

            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_HINT")));

            bool hasPath = Mod.CurrentChapterPath != null;
            bool hasCurrentRoom = Mod.CurrentChapterPath?.CurrentRoom != null;


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_TITLE")} ==="));
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_LAST_ATTEMPT")) {
                OnPressed = () => {
                    Mod.RemoveLastAttempt();
                },
                Disabled = !hasCurrentRoom
            });

            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_LAST_DEATH_STREAK")) {
                OnPressed = () => {
                    Mod.RemoveLastDeathStreak();
                },
                Disabled = !hasCurrentRoom
            });

            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_REMOVE_ALL_ATTEMPTS")) {
                OnPressed = () => {
                    Mod.WipeRoomData();
                },
                Disabled = !hasCurrentRoom
            });

            subMenu.Add(new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_REMOVE_GOLDEN_BERRY_DEATHS")) {
                OnDoubleConfirmation = () => {
                    Mod.RemoveRoomGoldenBerryDeaths();
                },
                Disabled = !hasCurrentRoom,
                HighlightColor = Color.Red,
            });


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_DATA_WIPE_CHAPTER_TITLE")} ==="));
            subMenu.Add(new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_CHAPTER_RESET_ALL_ATTEMPTS")) {
                OnDoubleConfirmation = () => {
                    Mod.WipeChapterData();
                },
                Disabled = !hasPath,
                HighlightColor = Color.Red,
            });

            subMenu.Add(new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_CHAPTER_RESET_ALL_GOLDEN_BERRY_DEATHS")) {
                OnDoubleConfirmation = () => {
                    Mod.WipeChapterGoldenBerryDeaths();
                },
                Disabled = !hasPath,
                HighlightColor = Color.Red,
            });
            
            subMenu.Add(new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_CHAPTER_RESET_GOLDEN_BERRY_COLLECTION")) {
                OnDoubleConfirmation = () => {
                    Mod.WipeChapterGoldenBerryCollects();
                },
                Disabled = !hasPath,
                HighlightColor = Color.Red,
            });

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_DATA_WIPE_VANILLA_PATHS_TITLE")} ==="));
            subMenu.Add(menuItem = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_VANILLA_PATHS_RESET_ALL_VANILLA_PATHS")) {
                OnDoubleConfirmation = () => {
                    Mod.CheckPrepackagedPaths(reset:true);
                },
                HighlightColor = Color.Red,
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_DATA_WIPE_VANILLA_PATHS_RESET_ALL_VANILLA_PATHS_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_DATA_WIPE_VANILLA_PATHS_RESET_ALL_VANILLA_PATHS_HINT_2"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_DATA_WIPE_VANILLA_PATHS_RESET_ALL_VANILLA_PATHS_HINT_3"));

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_DATA_WIPE_LIVE_DATA_TITLE")} ==="));
            subMenu.Add(menuItem = new DoubleConfirmButton($"{Dialog.Clean("MODOPTION_CCT_DATA_WIPE_LIVE_DATA_RESET")} '{StatManager.FormatFileName}' {Dialog.Clean("MODOPTION_CCT_DATA_WIPE_LIVE_DATA_FILE")}") { 
                OnDoubleConfirmation = () => {
                    Mod.StatsManager.ResetFormats();
                },
                HighlightColor = Color.Red,
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_DATA_WIPE_LIVE_DATA_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_DATA_WIPE_LIVE_DATA_HINT_2"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_DATA_WIPE_LIVE_DATA_HINT_3"));


            menu.Add(subMenu);
        }
        #endregion

        #region Summary Settings
        [JsonIgnore]
        public bool CreateSummary { get; set; } = false;
        [SettingIgnore]
        public bool IngameSummaryEnabled { get; set; } = true;
        [SettingIgnore]
        public int SummarySelectedAttemptCount { get; set; } = 20;
        public void CreateCreateSummaryEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_SUMMARY_TITLE"), false);
            TextMenu.Item menuItem;
            
            if (!inGame) {
                subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_SUMMARY_NOT_IN_GAME_HINT"), false));
                menu.Add(subMenu);
                return;
            }


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_SUMMARY_IN_GAME_SUMMARY_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_SUMMARY_IN_GAME_SUMMARY_ENABLED"), IngameSummaryEnabled) {
                OnValueChange = v => {
                    IngameSummaryEnabled = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_SUMMARY_IN_GAME_SUMMARY_BIND_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_SUMMARY_IN_GAME_SUMMARY_BIND_HINT_2"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_SUMMARY_IN_GAME_SUMMARY_BIND_HINT_3"));

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_SUMMARY_EXPORT_TITLE")} ==="));
            List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(5, "5"),
                    new KeyValuePair<int, string>(10, "10"),
                    new KeyValuePair<int, string>(20, "20"),
                    new KeyValuePair<int, string>(100, "100"),
                };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_SUMMARY_EXPORT_SUMMARY_OVER_X_ATTEMPTS"), AttemptCounts, SummarySelectedAttemptCount) {
                OnValueChange = (value) => {
                    SummarySelectedAttemptCount = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_SUMMARY_EXPORT_SUMMARY_OVER_X_ATTEMPTS_HINT"));


            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_SUMMARY_EXPORT_EXPORT_TRACKER_SUMMARY")) {
                OnPressed = () => {
                    Mod.CreateChapterSummary(SummarySelectedAttemptCount);
                },
                Disabled = Mod.CurrentChapterPath == null,
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_SUMMARY_EXPORT_EXPORT_TRACKER_SUMMARY_HINT"));

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
        [SettingIgnore]
        public int LiveDataDecimalPlaces { get; set; } = 2;
        [SettingIgnore]
        public int LiveDataSelectedAttemptCount { get; set; } = 20;

        [SettingIgnore]
        public RoomNameDisplayType LiveDataRoomNameDisplayType { get; set; } = RoomNameDisplayType.AbbreviationAndRoomNumberInCP;
        [SettingIgnore]
        public CustomNameBehavior LiveDataCustomNameBehavior { get; set; } = CustomNameBehavior.Override;
        [SettingIgnore]
        public ListFormat LiveDataListOutputFormat { get; set; } = ListFormat.Json;
        [SettingIgnore]
        public bool LiveDataHideFormatsWithoutPath { get; set; } = false;
        [SettingIgnore]
        public bool LiveDataIgnoreUnplayedRooms { get; set; } = false;


        [SettingIgnore]
        public int LiveDataChapterBarLightGreenPercent { get; set; } = 95;
        [SettingIgnore]
        public int LiveDataChapterBarGreenPercent { get; set; } = 80;
        [SettingIgnore]
        public int LiveDataChapterBarYellowPercent { get; set; } = 50;

        [SettingIgnore]
        public LowDeathBehavior LiveDataStatLowDeathBehavior { get; set; } = LowDeathBehavior.AlwaysCheckpoints;

        public void CreateLiveDataEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_TITLE"), false);
            TextMenu.Item menuItem;
            
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_TITLE")} ==="));
            List<KeyValuePair<int, string>> PBNameTypes = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.AbbreviationAndRoomNumberInCP, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_ROOM_NAME_FORMAT_ABBREVIATION_AND_ROOM_NUMBER_IN_CP")),
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.FullNameAndRoomNumberInCP, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_ROOM_NAME_FORMAT_FULL_NAME_AND_ROOM_NUMBER_IN_CP")),
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.DebugRoomName, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_ROOM_NAME_FORMAT_DEBUG_ROOM_NAME")),
            };
            if (LiveDataRoomNameDisplayType == RoomNameDisplayType.CustomRoomName) {
                LiveDataRoomNameDisplayType = RoomNameDisplayType.AbbreviationAndRoomNumberInCP;
            }
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_ROOM_NAME_FORMAT"), PBNameTypes, (int)LiveDataRoomNameDisplayType) {
                OnValueChange = (value) => {
                    LiveDataRoomNameDisplayType = (RoomNameDisplayType)value;
                    Mod.SaveChapterStats();
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_ROOM_NAME_FORMAT_HINT"));

            subMenu.Add(menuItem = new TextMenuExt.EnumSlider<CustomNameBehavior>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CUSTOM_ROOM_NAME_BEHAVIOR"), LiveDataCustomNameBehavior) { 
                OnValueChange = (value) => {
                    LiveDataCustomNameBehavior = value;
                    Mod.SaveChapterStats();
                }
            });

            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_HIDE_FORMATS_WHEN_NO_PATH"), LiveDataHideFormatsWithoutPath) {
                OnValueChange = v => {
                    LiveDataHideFormatsWithoutPath = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_HIDE_FORMATS_WHEN_NO_PATH_HINT"));

            List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(5, "5"),
                    new KeyValuePair<int, string>(10, "10"),
                    new KeyValuePair<int, string>(20, "20"),
                    new KeyValuePair<int, string>(100, "100"),
                };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CONSIDER_LAST_X_ATTEMPTS"), AttemptCounts, LiveDataSelectedAttemptCount) {
                OnValueChange = (value) => {
                    LiveDataSelectedAttemptCount = value;
                    Mod.SaveChapterStats();
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CONSIDER_LAST_X_ATTEMPTS_HINT"));

            List<int> DigitCounts = new List<int>() { 1, 2, 3, 4, 5 };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_MAX_DECIMAL_PLACES"), DigitCounts, LiveDataDecimalPlaces) {
                OnValueChange = (value) => {
                    LiveDataDecimalPlaces = value;
                    Mod.SaveChapterStats();
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_MAX_DECIMAL_PLACES_HINT"));
            
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_IGNORE_UNPLAYED_ROOMS"), LiveDataIgnoreUnplayedRooms) {
                OnValueChange = v => {
                    LiveDataIgnoreUnplayedRooms = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_IGNORE_UNPLAYED_ROOMS_HINT"));


            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_SUCCESS_RATE_COLORS"))); 
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_SUCCESS_RATE_COLORS_LIGHT_GREEN_PERCENTAGE"), PercentageSlider(), LiveDataChapterBarLightGreenPercent) {
                OnValueChange = (value) => {
                    LiveDataChapterBarLightGreenPercent = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_SUCCESS_RATE_COLORS_LIGHT_GREEN_PERCENTAGE_DEFAULT"));
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_SUCCESS_RATE_COLORS_GREEN_PERCENTAGE"), PercentageSlider(), LiveDataChapterBarGreenPercent) {
                OnValueChange = (value) => {
                    LiveDataChapterBarGreenPercent = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_SUCCESS_RATE_COLORS_GREEN_PERCENTAGE_DEFAULT"));
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_SUCCESS_RATE_COLORS_YELLOW_PERCENTAGE"), PercentageSlider(), LiveDataChapterBarYellowPercent) {
                OnValueChange = (value) => {
                    LiveDataChapterBarYellowPercent = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_SUCCESS_RATE_COLORS_YELLOW_PERCENTAGE_DEFAULT"));


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_TITLE")} ==="));
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_HINT"), false));
            subMenu.Add(menuItem = new TextMenuExt.EnumSlider<LowDeathBehavior>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_LOW_DEATH_DISPLAY_BEHAVIOR"), LiveDataStatLowDeathBehavior) {
                OnValueChange = (value) => {
                    LiveDataStatLowDeathBehavior = value;
                    Mod.SaveChapterStats();
                }
            });
            subMenu.AddDescription(menu, menuItem, $"{Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_LOW_DEATH_DISPLAY_BEHAVIOR_HINT_1")} '{ListCheckpointDeathsStat.ListCheckpointDeaths}'");
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_LOW_DEATH_DISPLAY_BEHAVIOR_HINT_2"));


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_ENABLE"), LiveDataFileOutputEnabled) {
                OnValueChange = (value) => {
                    LiveDataFileOutputEnabled = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_ENABLE_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_ENABLE_HINT_2"));

            List<KeyValuePair<int, string>> ListTypes = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>((int)ListFormat.Plain, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_FORMAT_PLAIN")),
                    new KeyValuePair<int, string>((int)ListFormat.Json, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_FORMAT_JSON")),
                };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_FORMAT"), ListTypes, (int)LiveDataListOutputFormat) {
                OnValueChange = (value) => {
                    LiveDataListOutputFormat = (ListFormat)value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FILE_OUTPUT_FORMAT_HINT"));

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FORMAT_EDITING_TITLE")} ==="));
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FORMAT_EDITING_OPEN_EDITOR_IN_BROWSER")) {
                OnPressed = () => {
                    string relPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "LiveDataEditTool.html");
                    string path = System.IO.Path.GetFullPath(relPath);
                    Mod.LogVerbose($"Opening format editor at '{path}'");
                    Process.Start("explorer", path);
                },
            });
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FORMAT_EDITING_OPEN_FORMAT_TEXT_FILE")).Pressed(() => {
                string relPath = ConsistencyTrackerModule.GetPathToFile(StatManager.BaseFolder, StatManager.FormatFileName);
                string path = System.IO.Path.GetFullPath(relPath);
                Mod.LogVerbose($"Opening format file at '{path}'");
                Process.Start("explorer", path);
            }));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FORMAT_EDITING_OPEN_FORMAT_TEXT_FILE_HINT"));
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FORMAT_EDITING_RELOAD_FORMAT_FILE")) {
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
        public int ExternalOverlayChapterBorderWidthMultiplier { get; set; } = 2;

        public void CreateExternalOverlayEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_TITLE"), false);
            TextMenu.Item menuItem;

            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_OPEN_OVERLAY_IN_BROWSER")).Pressed(() => {
                string path = System.IO.Path.GetFullPath(ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "CCTOverlay.html"));
                Process.Start("explorer", path);
            }));


            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_REFRESH_HINT")));
            //General Settings
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.Slider(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_REFRESH_TIME"), (i) => i == 1 ? $"1 {Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_REFRESH_TIME_SECOND")}" : $"{i} {Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_REFRESH_TIME_SECONDS")}", 1, 59, ExternalOverlayRefreshTimeSeconds) {
                OnValueChange = (value) => {
                    ExternalOverlayRefreshTimeSeconds = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_REFRESH_TIME_HINT"));
            List<int> attemptsList = new List<int>() { 5, 10, 20, 100 };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_CONSIDER_LAST_X_ATTEMPTS"), attemptsList, ExternalOverlayAttemptsCount) {
                OnValueChange = (value) => {
                    ExternalOverlayAttemptsCount = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_CONSIDER_LAST_X_ATTEMPTS_HINT"));
            subMenu.Add(new TextMenu.Slider(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_TEXT_OUTLINE_SIZE"), (i) => $"{i}px", 0, 60, ExternalOverlayTextOutlineSize) {
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
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_TEXT_FONT"), fontList, ExternalOverlayFontFamily) {
                OnValueChange = v => {
                    ExternalOverlayFontFamily = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_TEXT_FONT_HINT"));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_GENERAL_COLORBLIND_MODE"), ExternalOverlayColorblindMode) {
                OnValueChange = v => {
                    ExternalOverlayColorblindMode = v;
                }
            });

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TITLE")} ==="));

            //Text Segment Display
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_HINT")));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_ENABLE_ALL"), ExternalOverlayTextDisplayEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayEnabled = v;
                }
            });
            List<string> availablePresets = new List<string>() {
                    Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_PRESET_1"),
                    Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_PRESET_2"),
                    Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_PRESET_3"),
                    Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_PRESET_4"),
                    Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_PRESET_5"),
                };
            subMenu.Add(new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_PRESET"), availablePresets, ExternalOverlayTextDisplayPreset) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayPreset = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_ENABLE_LEFT"), ExternalOverlayTextDisplayLeftEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayLeftEnabled = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_ENABLE_MIDDLE"), ExternalOverlayTextDisplayMiddleEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayMiddleEnabled = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_TEXT_STATS_ENABLE_RIGHT"), ExternalOverlayTextDisplayRightEnabled) {
                OnValueChange = v => {
                    ExternalOverlayTextDisplayRightEnabled = v;
                }
            });

            //Chapter Bar
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_CHAPTER_BAR_HINT")));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_CHAPTER_BAR_ENABLE"), ExternalOverlayChapterBarEnabled) {
                OnValueChange = v => {
                    ExternalOverlayChapterBarEnabled = v;
                }
            });

            //subMenu.Add(new TextMenu.SubHeader($"The width of the black bars between rooms on the chapter display"));
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_CHAPTER_BAR_BORDER_WIDTH"), 1, 10, ExternalOverlayChapterBorderWidthMultiplier) {
                OnValueChange = (value) => {
                    ExternalOverlayChapterBorderWidthMultiplier = value;
                }
            });

            //subMenu.Add(new TextMenu.SubHeader($"Success rate in a room to get a certain color (default: light green 95%, green 80%, yellow 50%)"));
            


            //Room Attempts Display
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_ROOM_ATTEMPTS_HINT")));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_ROOM_ATTEMPTS_ENABLE"), ExternalOverlayRoomAttemptsDisplayEnabled) {
                OnValueChange = v => {
                    ExternalOverlayRoomAttemptsDisplayEnabled = v;
                }
            });


            //Golden Share Display
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_GOLDEN_SHARE_HINT")));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_GOLDEN_SHARE_ENABLE"), ExternalOverlayGoldenShareDisplayEnabled) {
                OnValueChange = v => {
                    ExternalOverlayGoldenShareDisplayEnabled = v;
                }
            });
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_GOLDEN_SHARE_SHOW_SESSION_DEATHS"), ExternalOverlayGoldenShareDisplayShowSession) {
                OnValueChange = v => {
                    ExternalOverlayGoldenShareDisplayShowSession = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_GOLDEN_SHARE_SHOW_SESSION_DEATHS_HINT"));


            //Golden PB Display
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_GOLDEN_PB_HINT")));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_EXTERNAL_OVERLAY_SETTINGS_COMPONENT_GOLDEN_PB_ENABLE"), ExternalOverlayGoldenPBDisplayEnabled) {
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
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TITLE"), false);
            TextMenu.Item menuItem;
            
            if (!inGame) {
                subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_NOT_IN_GAME_HINT"), false));
                menu.Add(subMenu);
                return;
            }

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_DEBUG_MAP_TITLE")} ==="));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_DEBUG_MAP_SHOW_ROOM_NAMES"), ShowCCTRoomNamesOnDebugMap) {
                OnValueChange = v => {
                    ShowCCTRoomNamesOnDebugMap = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_DEBUG_MAP_SHOW_SUCCESS_RATE_BORDERS"), ShowSuccessRateBordersOnDebugMap) {
                OnValueChange = v => {
                    ShowSuccessRateBordersOnDebugMap = v;
                }
            });

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TITLE")} ==="));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_ENABLE"), IngameOverlayTextEnabled) {
                OnValueChange = v => {
                    IngameOverlayTextEnabled = v;
                }
            });
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_ONLY_SHOW_IN_MENU"), IngameOverlayOnlyShowInPauseMenu) {
                OnValueChange = v => {
                    IngameOverlayOnlyShowInPauseMenu = v;
                }
            });

            

            //Get all formats
            List<string> availableFormats = new List<string>(Mod.StatsManager.GetFormatListSorted().Select((f) => f.Name));
            List<string> availableFormatsGolden = new List<string>(availableFormats);
            string noneFormat = Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_FORMAT_NO_FORMAT");
            availableFormatsGolden.Insert(0, noneFormat);
            string descAvailableFormats = $"{Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_FORMAT_EDIT_HINT")} 'Celeste/ConsistencyTracker/{StatManager.BaseFolder}/{StatManager.FormatFileName}'";
            string descAvailableFormatsGolden = $"{Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_FORMAT_GOLDEN_HINT_1")} '{noneFormat}' {Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_FORMAT_GOLDEN_HINT_2")}";
            string descHideWithGolden = Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_FORMAT_GOLDEN_HINT_0");


            bool hasStats = Mod.CurrentChapterStats != null;
            bool holdingGolden = Mod.CurrentChapterStats.ModState.PlayerIsHoldingGolden;

            // ========== Text 1 ==========
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_1_TITLE")} ==="));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_1_ENABLE"), IngameOverlayText1Enabled) {
                OnValueChange = v => {
                    IngameOverlayText1Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(1, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), IngameOverlayText1Position) {
                OnValueChange = v => {
                    IngameOverlayText1Position = v;
                    Mod.IngameOverlay.SetTextPosition(1, v);
                }
            });
            IngameOverlayText1Format = GetFormatOrDefault(IngameOverlayText1Format, availableFormats);
            IngameOverlayText1FormatGolden = GetFormatOrDefault(IngameOverlayText1FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT"), availableFormats, IngameOverlayText1Format) {
                OnValueChange = v => {
                    IngameOverlayText1Format = v;
                    TextSelectionHelper(hasStats, holdingGolden, 1, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT_GOLDEN"), availableFormatsGolden, IngameOverlayText1FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText1FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 1, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_HIDE_IN_GOLDEN"), IngameOverlayText1HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText1HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(1, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SIZE"), PercentageSlider(5, 5, 500), IngameOverlayText1Size) {
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
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_X"), 0, 2000, IngameOverlayText1OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText1OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(1, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_Y"), 0, 2000, IngameOverlayText1OffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayText1OffsetY = value;
                    Mod.IngameOverlay.SetTextOffsetY(1, value);
                }
            });


            // ========== Text 2 ==========
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_2_TITLE")} ==="));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_2_ENABLE"), IngameOverlayText2Enabled) {
                OnValueChange = v => {
                    IngameOverlayText2Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(2, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), IngameOverlayText2Position) {
                OnValueChange = v => {
                    IngameOverlayText2Position = v;
                    Mod.IngameOverlay.SetTextPosition(2, v);
                }
            });
            IngameOverlayText2Format = GetFormatOrDefault(IngameOverlayText2Format, availableFormats);
            IngameOverlayText2FormatGolden = GetFormatOrDefault(IngameOverlayText2FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT"), availableFormats, IngameOverlayText2Format) {
                OnValueChange = v => {
                    IngameOverlayText2Format = v;
                    TextSelectionHelper(hasStats, holdingGolden, 2, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT_GOLDEN"), availableFormatsGolden, IngameOverlayText2FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText2FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 2, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_HIDE_IN_GOLDEN"), IngameOverlayText2HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText2HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(2, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SIZE"), PercentageSlider(5, 5, 500), IngameOverlayText2Size) {
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
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_X"), 0, 2000, IngameOverlayText2OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText2OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(2, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_Y"), 0, 2000, IngameOverlayText2OffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayText2OffsetY = value;
                    Mod.IngameOverlay.SetTextOffsetY(2, value);
                }
            });

            // ========== Text 3 ==========
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_3_TITLE")} ==="));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_3_ENABLE"), IngameOverlayText3Enabled) {
                OnValueChange = v => {
                    IngameOverlayText3Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(3, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), IngameOverlayText3Position) {
                OnValueChange = v => {
                    IngameOverlayText3Position = v;
                    Mod.IngameOverlay.SetTextPosition(3, v);
                }
            });
            IngameOverlayText3Format = GetFormatOrDefault(IngameOverlayText3Format, availableFormats);
            IngameOverlayText3FormatGolden = GetFormatOrDefault(IngameOverlayText3FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT"), availableFormats, IngameOverlayText3Format) {
                OnValueChange = v => {
                    IngameOverlayText3Format = v;
                    TextSelectionHelper(hasStats, holdingGolden, 3, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT_GOLDEN"), availableFormatsGolden, IngameOverlayText3FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText3FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 3, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_HIDE_IN_GOLDEN"), IngameOverlayText3HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText3HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(3, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SIZE"), PercentageSlider(5, 5, 500), IngameOverlayText3Size) {
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
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_X"), 0, 2000, IngameOverlayText3OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText3OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(3, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_Y"), 0, 2000, IngameOverlayText3OffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayText3OffsetY = value;
                    Mod.IngameOverlay.SetTextOffsetY(3, value);
                }
            });


            // ========== Text 4 ==========
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_4_TITLE")} ==="));
            subMenu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_4_ENABLE"), IngameOverlayText4Enabled) {
                OnValueChange = v => {
                    IngameOverlayText4Enabled = v;
                    Mod.IngameOverlay.SetTextVisible(4, v);
                }
            });
            subMenu.Add(new TextMenuExt.EnumSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), IngameOverlayText4Position) {
                OnValueChange = v => {
                    IngameOverlayText4Position = v;
                    Mod.IngameOverlay.SetTextPosition(4, v);
                }
            });
            IngameOverlayText4Format = GetFormatOrDefault(IngameOverlayText4Format, availableFormats);
            IngameOverlayText4FormatGolden = GetFormatOrDefault(IngameOverlayText4FormatGolden, availableFormatsGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT"), availableFormats, IngameOverlayText4Format) {
                OnValueChange = v => {
                    IngameOverlayText4Format = v;
                    TextSelectionHelper(hasStats, holdingGolden, 4, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormats);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<string>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SELECTED_FORMAT_GOLDEN"), availableFormatsGolden, IngameOverlayText4FormatGolden) {
                OnValueChange = v => {
                    IngameOverlayText4FormatGolden = v;
                    GoldenTextSelectionHelper(hasStats, holdingGolden, 4, noneFormat, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descAvailableFormatsGolden);
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_HIDE_IN_GOLDEN"), IngameOverlayText4HideWithGolden) {
                OnValueChange = v => {
                    IngameOverlayText4HideWithGolden = v;
                    Mod.IngameOverlay.SetTextHideInGolden(4, v);
                }
            });
            subMenu.AddDescription(menu, menuItem, descHideWithGolden);
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_SIZE"), PercentageSlider(5, 5, 500), IngameOverlayText4Size) {
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
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_X"), 0, 2000, IngameOverlayText4OffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayText4OffsetX = value;
                    Mod.IngameOverlay.SetTextOffsetX(4, value);
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_OFFSET_Y"), 0, 2000, IngameOverlayText4OffsetY) {
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
        public bool LogFlipY { get; set; } = false;

        [SettingIgnore]
        public bool LogFlagDashes { get; set; } = false;
        [SettingIgnore]
        public bool LogFlagMaxDashes { get; set; } = false;
        [SettingIgnore]
        public bool LogFlagDashDir { get; set; } = false;
        [SettingIgnore]
        public bool LogFlagFacing { get; set; } = false;

        public void CreatePhysicsLoggerSettingsEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_TITLE"), false);
            TextMenu.Item menuItem;

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_GENERAL_TITLE")} ==="));
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_GENERAL_OPEN_IN_BROWSER")) {
                OnPressed = () => {
                    string relPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "PhysicsInspector.html");
                    string path = System.IO.Path.GetFullPath(relPath);
                    Mod.LogVerbose($"Opening physics inspector at '{path}'");
                    Process.Start("explorer", path);
                },
            });
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_GENERAL_RECORD_ENABLE"), LogPhysicsEnabled) {
                OnValueChange = v => {
                    LogPhysicsEnabled = v;
                    Mod.Log($"Logging physics {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_GENERAL_RECORD_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_GENERAL_RECORD_HINT_2"));


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_SEGMENT_RECORDING_ON_DEATH"), LogSegmentOnDeath) {
                OnValueChange = v => {
                    LogSegmentOnDeath = v;
                    Mod.Log($"Recording segmenting on death {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_SEGMENT_RECORDING_ON_DEATH_HINT"));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_SEGMENT_RECORDING_ON_LOAD_STATE"), LogSegmentOnLoadState) {
                OnValueChange = v => {
                    LogSegmentOnLoadState = v;
                    Mod.Log($"Recording segmenting on loading state {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_SEGMENT_RECORDING_ON_LOAD_STATE_HINT"));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_COPY_TAS_FILE_TO_CLIPBOARD"), LogPhysicsInputsToTasFile) {
                OnValueChange = v => {
                    LogPhysicsInputsToTasFile = v;
                    Mod.Log($"Recordings inputs to tas file {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_COPY_TAS_FILE_TO_CLIPBOARD_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_COPY_TAS_FILE_TO_CLIPBOARD_HINT_2"));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_FLIP_Y"), LogFlipY) {
                OnValueChange = v => {
                    LogFlipY = v;
                    Mod.Log($"Logging physics flip y-axis {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_FLIP_Y_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_FLIP_Y_HINT_2"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_FLIP_Y_HINT_3"));

            
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_DASH_COUNT"), LogFlagDashes) {
                OnValueChange = v => {
                    LogFlagDashes = v;
                    Mod.Log($"Optional flag '{nameof(LogFlagDashes)}' {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_DASH_COUNT_HINT"));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_MAX_DASH_COUNT"), LogFlagMaxDashes) {
                OnValueChange = v => {
                    LogFlagMaxDashes = v;
                    Mod.Log($"Optional flag '{nameof(LogFlagMaxDashes)}' {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_MAX_DASH_COUNT_HINT"));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_DASH_DIRECTION"), LogFlagDashDir) {
                OnValueChange = v => {
                    LogFlagDashDir = v;
                    Mod.Log($"Optional flag '{nameof(LogFlagDashDir)}' {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_DASH_DIRECTION_HINT"));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_FACING"), LogFlagFacing) {
                OnValueChange = v => {
                    LogFlagFacing = v;
                    Mod.Log($"Optional flag '{nameof(LogFlagFacing)}' {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_OPTIONAL_FLAGS_FACING_HINT"));

            menu.Add(subMenu);
        }
        #endregion
        
        #region Pace Ping Settings
        [JsonIgnore]
        public bool PacePing { get; set; } = false;

        [SettingIgnore]
        public bool PacePingEnabled { get; set; } = false;
        
        [SettingIgnore]
        public PbPingType PacePingPbPingType { get; set; } = PbPingType.NoPing;

        [SettingIgnore]
        public bool PacePingAllDeathsEnabled { get; set; } = false;

        public void CreatePacePingEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_TITLE"), false);
            TextMenu.Item menuItem;
            
            if (!inGame) {
                subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_NOT_IN_GAME_HINT"), false));
                menu.Add(subMenu);
                return;
            }
            

            bool hasPath = Mod.CurrentChapterPath != null;
            bool hasCurrentRoom = Mod.CurrentChapterPath?.CurrentRoom != null;
            PaceTiming paceTiming = null;
            if (hasCurrentRoom) { 
                paceTiming = Mod.PacePingManager.GetPaceTiming(Mod.CurrentChapterPath.ChapterSID, Mod.CurrentChapterPath.CurrentRoom.DebugRoomName);
            }

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_ENABLE"), PacePingEnabled) {
                OnValueChange = v => {
                    PacePingEnabled = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_2"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_3"));

            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_MSG_IMPORT")) { 
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

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_WEBHOOK_IMPORT")).Pressed(() => {
                string text = TextInput.GetClipboardText();
                Mod.Log($"Importing WebHook url from clipboard...");
                try {
                    Mod.PacePingManager.SaveDiscordWebhook(text);
                } catch (Exception ex) {
                    Mod.Log($"Couldn't import WebHook url from clipboard: {ex}");
                }
            }));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_URL_HINT"));

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_RELOAD_STATE_FILE")) {
                OnPressed = Mod.PacePingManager.ReloadStateFile,
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_RELOAD_STATE_FILE_HINT"));

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_TITLE")} ==="));
            List<KeyValuePair<PbPingType, string>> pbPingTypes = new List<KeyValuePair<PbPingType, string>>() {
                new KeyValuePair<PbPingType, string>(PbPingType.NoPing, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_NO")),
                new KeyValuePair<PbPingType, string>(PbPingType.PingOnPbEntry, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_ENTRY")),
                new KeyValuePair<PbPingType, string>(PbPingType.PingOnPbPassed, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_PASSED")),
            };
            subMenu.Add(new TextMenuExt.EnumerableSlider<PbPingType>(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_ENABLE"), pbPingTypes, PacePingPbPingType) {
                OnValueChange = (newValue) => {
                    PacePingPbPingType = newValue;
                }
            });

            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_MSG_IMPORT")) {
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing pb ping message from clipboard...");
                    try {
                        Mod.PacePingManager.SavePBPingMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import pb ping message from clipboard: {ex}");
                    }
                },
            });


            string roomAddition = hasCurrentRoom ? $" ({Mod.CurrentChapterPath.CurrentRoom.GetFormattedRoomName(StatManager.RoomNameType)})" : "";
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_TITLE")}{roomAddition} ==="));
            TextMenu.Button importMessageButton = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_MSG_IMPORT")) {
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
            };
            TextMenu.Button testButton = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_TEST_PING")) {
                OnPressed = Mod.PacePingManager.TestPingForCurrentRoom,
                Disabled = paceTiming == null,
            };
            TextMenu.OnOff toggleEmbedsEnabledButton = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_ENABLE_EMBEDS"), paceTiming == null ? true : paceTiming.EmbedsEnabled) {
                OnValueChange = (isEnabled) => {
                    Mod.PacePingManager.SavePaceTimingEmbedsEnabled(isEnabled);
                },
                Disabled = paceTiming == null
            };
            TextMenu.OnOff togglePacePingButton = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_PING_THIS_ROOM"), paceTiming != null) {
                OnValueChange = (isEnabled) => {
                    bool isNowEnabled = Mod.PacePingManager.SetCurrentRoomPacePingEnabled(isEnabled);
                    importMessageButton.Disabled = !isNowEnabled;
                    testButton.Disabled = !isNowEnabled;
                    toggleEmbedsEnabledButton.Disabled = !isNowEnabled;
                },
                Disabled = !hasCurrentRoom
            };

            subMenu.Add(togglePacePingButton);
            subMenu.AddDescription(menu, togglePacePingButton, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_PING_THIS_ROOM_HINT"));
            subMenu.Add(importMessageButton);
            subMenu.Add(toggleEmbedsEnabledButton);
            subMenu.Add(testButton);


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_MSG_ON_EVERY_GOLDEN_DEATH"), PacePingAllDeathsEnabled) {
                OnValueChange = v => {
                    PacePingAllDeathsEnabled = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_MSG_ON_EVERY_GOLDEN_DEATH_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_MSG_ON_EVERY_GOLDEN_DEATH_HINT_2"));

            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_MSG_IMPORT")) {
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing all deaths message from clipboard...");
                    try {
                        Mod.PacePingManager.SaveAllDeathsMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import all deaths message from clipboard: {ex}");
                    }
                },
            });

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_WEBHOOK_IMPORT")).Pressed(() => {
                string text = TextInput.GetClipboardText();
                Mod.Log($"Importing WebHook url from clipboard...");
                try {
                    Mod.PacePingManager.SaveDiscordWebhookAllDeaths(text);
                } catch (Exception ex) {
                    Mod.Log($"Couldn't import WebHook url from clipboard: {ex}");
                }
            }));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_WEBHOOK_IMPORT_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_WEBHOOK_IMPORT_HINT_2"));

            menu.Add(subMenu);
        }
        #endregion

        #region FAQ
        [JsonIgnore]
        public bool FAQ { get; set; } = false;

        //[SettingIgnore]
        //public bool PacePingEnabled { get; set; } = false;

        public void CreateFAQEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("FAQ", false);

            List<FAQEntry.FAQSectionModel> faq = new List<FAQEntry.FAQSectionModel>() {
                new FAQEntry.FAQSectionModel(){
                    Title = "Path Management",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "What is a Path?",
                            Answer = "A path is a configuration of Checkpoints and Rooms, in the order that you would beat them in during a run of whatever challenge you want to do. All vanilla maps come with pre-installed paths. 1A-8A and Farewell also have a separate pre-installed Path for full clear runs!" +
                            "\nEvery Checkpoint has a name and an abbreviation" +
                            "\nEvery Room can have a custom room name, and if not, it will get a name from the Checkpoint it is in + the room number in the Checkpoint. Additionally, Rooms can be marked as 'transition' rooms, making them not show up in many stat calculations",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "What is a Path Segment?",
                            Answer = "A later addition to the Path system added Path Segments." +
                            "\nEach map can have multiple Path Segments. A Path Segment is a Path with a given name, for example 'Normal', 'Full Clear', 'Silver 1' or whatever else you can think of." +
                            " Every Path Segment has it's own separate stats!" +
                            "\nUsing this you can track multiple different Paths for a single map (e.g. if you do segments of bigger maps, such as 9D), and switch between them easily in the 'Path Recording' menu.",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How do I record a Path?",
                            Answer = "1. Go into the first room of the Path Segment you want to record." +
                            "\n2. Go to the menu 'Path Recording' and hit the button 'Start Path Recording'" +
                            "\n3. Play through the map as normal (you can do this with invincibility and stuff if you want)" +
                            "\n4. In the last room of the segment, hit the button 'Save Path'. Alternatively, complete the map and the path will be saved automatically." +
                            "\n\nYou have now recorded a Path and can start tracking!"
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How do I rename a Checkpoint in CCT?",
                            Answer = "Sadly this cannot be done in the game currently." +
                            "\nYou can however manually edit the file, see the FAQ entry below." +
                            "\nIn the future, I hope to add an actual Path editing tool."
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How can I manually edit a Path file?",
                            Answer = "The easiest way to manually edit a path is:" +
                            "\n1. Go into the map you want to edit the path for" +
                            "\n2. Export the Path to your clipboard with 'Path Recording -> Export Path To Clipboard'" +
                            "\n3. Paste the Path into a text editor of your choice" +
                            "\n4. Edit the Path to your liking. You can rename checkpoints, rooms, and change the order of rooms and checkpoints. You can add custom room names and set rooms as transition rooms. But beware: The path needs to follow the JSON file format" +
                            "\n5. Copy the edited Path back to your clipboard (CTRL + A to select all text, CTRL + C to copy)" +
                            "\n6. Import the Path from your clipboard with 'Path Recording' -> 'Import Path from Clipboard'" +
                            "\n\nIf done correctly, the changes should be visible immediately. If there was any error with the file format, the import will be stopped and your original path will NOT be overwritten."
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "I accidentally screwed up a vanilla Path file...",
                            Answer = "You can always reset all vanilla Path files back to the original by going to '!!Data Wipe!! -> Reset All Vanilla Paths'"
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "I don't see the 'Full Clear' segments for 1A-8A/Farewell...",
                            Answer = "The 'Full Clear' segments were added in a recent update. If you had CCT installed before the update, the vanilla path files aren't automatically updated." +
                            "\nTo get all default segments added, hit the button '!!Data Wipe!! -> Reset All Vanilla Paths'. This will reset all vanilla paths to the up-to-date original."
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = "Stats Management",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "What stats are there?",
                            Answer = "There are 3 types of stats being tracked:" +
                            "\n\n1. Dying to a room with the golden" +
                            "\n2. Dying in / Completing a room (independently of the golden)" +
                            "\n3. Collecting the golden" +
                            "\n\nMany stats are calculated off of the golden deaths, such as:" +
                            "\n- Best Runs" +
                            "\n- Current Run #" +
                            "\n- Current Run Top x%" +
                            "\n- Room Choke Rate" +
                            "\n- Room Golden Success Rate" +
                            "\n- ..." +
                            "\n\nBut there are also a bunch of stats that are calculated off of normal room completions/deaths, such as:" +
                            "\n- Room/Checkpoint/Chapter Success Rate" +
                            "\n- Room Streak / Max Streak" +
                            "\n- Golden Chance from Start / Golden Chance to End for any room" +
                            "\n- ..." +
                            "\n\nFor the normal room completions/deaths stats, you can configure how many of the last attempts should be counted for the calculation (default: 20)" +
                            "\n\n\nTo get a list of all possible calculated stats take a look at the Live-Data Editor!",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "Where are the stats saved?",
                            Answer = "All stats are saved separate from the path. This means that you can change the path at will, and the stats stay the same." +
                            "\n\nThe raw stats file is saved in the location:" +
                            "\n<CelesteFolder>\\ConsistencyTracker\\stats\\<MapName>.json" +
                            "\nWhere the <MapName> is the SID of the map (usually the campaign's or mapper's name) + the side name (Normal, BSide, CSide)"
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "I accidentally collected a golden berry while practicing...",
                            Answer = "There is currently no ingame option to remove accidental collects of goldens, but here is how you can remove it by editing the stats file:" +
                            "\n\n1. Don't be in the map you want to edit the stats for" +
                            "\n2. Find the stats file in '<CelesteFolder>\\ConsistencyTracker\\stats\\<MapName>.json'" +
                            "\n3. Find the line that says \"goldenCollectedCount\"" +
                            "\n4. Set the value back down to 0" +
                            "\n5. Save the file" +
                            "\n\nOnce you enter the map now the golden will be gone from CCT's best runs." +
                            "\n\nIt will still show up in the Ingame Summary's session history, but fixing this is much harder to do (impossible?) so I won't go into detail here."
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){ 
                    Title = "Live-Data",
                    Entries = new List<FAQEntry.FAQEntryModel>(){ 
                        new FAQEntry.FAQEntryModel(){ 
                            Question = "What is Live-Data?",
                            Answer = "In the beginning of CCT, only raw stats were output to text files. External applications had to calculate any meaningful stat on their own to make use of the data." +
                            "\n\nLive-Data added mod-side calculation of stats, and a way of letting the user decide how they are formatted. As of writing this FAQ there are about 80 different stats and pieces of information from the current game state that can be output through Live-Data.",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "What is a Live-Data format?",
                            Answer = "A 'format' instructs Live-Data how to output information. Each format has a name and an arbitrary text containing certain placeholders." +
                            "\nPlaceholders look like this: {category:name}. Live-Data will insert the desired stat in place of the placeholder." +
                            "\n\nExample: The format" +
                            "\n'Current: {room:name} | Room: ({room:roomNumberInChapter}/{chapter:roomCount})'" +
                            "\ncould produce the output" +
                            "\n'Current: DT-7 | Room: (59/93)'" +
                            "\nwhen playing Farewell.",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "I don't see format 'xy' in my list of formats, where is it?",
                            Answer = "When new default formats are added in an update, they will not be added to your list of formats automatically," +
                            " since that could potentially overwrite changes you made manually." +
                            " To get the most up-to-date default formats, you can reset the 'format.txt' file back to default using the button:" +
                            "\nMod Options -> !!Data Wipe!! -> Reset 'format.txt' file" +
                            "\nbut beware that this will reset any custom formats you have created, so back those up first!",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How can I make my own format?",
                            Answer = "There are 2 ways to make your own format:" +
                            "\n\nFormat Editor" +
                            "\n1. Start the Format Editor through 'Mod Options -> Live-Data Settings -> Start Format Editor In Browser'" +
                            "\n2. In the Format Editor, click the button 'New Format' in the bottom left corner" +
                            "\n3. Give your format a name." +
                            "\n4. Type whatever you want in to the format text section. You can see all available placeholders in the panel on the right side. Hover over a placeholder to view an explanation of what it is for and click on a placeholder to insert it into the format text. If you are in a map, you will be able to see a preview of how the format looks in the box at the bottom." +
                            "\n5. Hit the button 'Create new Format' at the bottom to save your format" +
                            "\n\nManual File Editing" +
                            "\n1. Open the 'format.txt' file through 'Mod Options -> Live-Data Settings -> Open Format Text File' in your text editor" +
                            "\n2. Add a new line for your custom format at the bottom of the file" +
                            "\n3. Write your format in the pattern: 'name;format'. To see all available placeholder with live previews I would still recommend the Format Editor Tool." +
                            "\n4. Save the file" +
                            "\n5. Reload the format file through 'Mod Options -> Live-Data Settings -> Reload Format File'" +
                            "\n\nThe newly added format should now be selectable in the In-Game overlay!",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "I would like to see a certain stat added...",
                            Answer = "If you have an idea for a new stat, feel free to message me on Discord (viddie#4751) or open an issue on GitHub (link on the GameBanana page of CCT). Please include an explanation of how to calculate the stat!",
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = "In-Game Overlay",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "Why does it say <path> everywhere in the In-Game Overlay?",
                            Answer = "CCT doesn't know how you want to go through the map from start to finish." +
                            " To tell CCT how you want to do that, you need a path. By default, all vanilla" +
                            " maps come with a path pre-installed." +
                            "\nIf you want to track modded maps, you need to record a path yourself. See the Path Recording section in the FAQ for more details."
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "The In-Game Overlay is annoying in maps where I don't want to track anything.",
                            Answer = "There is an setting to automatically hide the overlay in maps where you don't have a path recorded:" +
                            "\n'Live-Data Settings' -> 'Hide Formats When No Path'"
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How can I customize the available formats for the In-Game Overlay?",
                            Answer = "The available formats are taken from the Live-Data feature. Any changes to it will be available in the In-Game Overlay." +
                            "\nCheck out the Live-Data section in the FAQ for more details."
                        },
                    },
                },
                new FAQEntry.FAQSectionModel(){
                    Title = "In-Game Summary",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "What is the In-Game Summary?",
                            Answer = "The In-Game Summary is a way to display a bunch of data tracked by CCT within the game. It currently displays:" +
                            "\n\n- Session History: You can look through all of your previous sessions and see how you improved!" +
                            "\n- Overall Stats: How your stats look overall in a map and how it developed over the sessions." +
                            "\n- Charts: Choke Rate / Room Entries chart for your overall stats and each session." +
                            "\n- Graphs: A funky looking graphs of all your golden deaths throughout the map." +
                            "\n\nAll of the displayed data is hand programmed, including the charts/graphs/tables. As such, issues are almost guaranteed." +
                            "\nThere will likely be fixes/changes to this overlay in the future. If you have any suggestions, feel free to message me!"
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How do I use the In-Game Summary?",
                            Answer = "In the Mod Options of CCT you can bind a button to open the In-Game Summary. Whenever you are in a map, you can press that button to bring up the Summary overlay." +
                            "\n\nNavigating the In-Game Summary by default uses these buttons:" +
                            "\n- <Grab> to switch through the tabs" +
                            "\n- <Up>/<Down> to navigate within a tab" +
                            "\n\nThese button bindings can be changed in Mod Options."
                        },
                    },
                },
                new FAQEntry.FAQSectionModel(){
                    Title = "External Tools",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "Tool XYZ shows 'Unable to fetch (...), is CCT running?' when CCT is running!",
                            Answer = "This happens when an external tool is unable to reach the CCT API." +
                            "\nThe most likely cause for this is, that you have the 'Debug Mode' in the Everest settings set to 'Off'. Setting it to either 'Everest' or 'Always' should make the API reachable for the external tools." +
                            "\n\nHint: You can test if the API is reachable by going to 'http://localhost:32270/' in your browser. If the page doesn't load, the API is not available.",
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = "Physics Inspector",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "What is the Physics Inspector?",
                            Answer = "The Physics Inspector is a tool to record yourself playing a map and then looking at the recorded data in your browser, frame by frame." +
                            "\nIt records:" +
                            "\n- Player data such as: Position, Speed/Velocity, Acceleration, Inputs, Stamina, Liftboost, etc." +
                            "\n- Map data such as: Room boundaries, Solid tiles, Solid/Trigger entities" +
                            "\n- The map data is static tho. It is only recorded once on each room entry and does not change, so movement of entities isn't visible!" +
                            "\n\nIf you ever wondered \"How did I just die???\", this tool can help you figure that out.",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How do I use it?",
                            Answer = "To make a recording, enable the setting 'Mod Options -> Physics Inspector Settings -> Recording Physics Enabled'." +
                            "\n\n- Whenever you die or load a savestate, a recording is saved." +
                            "\n- You can view a recording by going to 'Mod Options -> Physics Inspector Settings -> Open Inspector In Browser'\n(I could also recommend bookmarking that page. Reload the tab to get the newest recordings!)" +
                            "\n- Now you can pan/zoom around the map and hover over frames to see the exact physics data" +
                            "\n- The frames are color coded: White (No special state), Red (Dashing), Green (Holding the Jump button), Yellow (Feather), Black (Dead)" +
                            "\n\nOnly the 10 most recent recordings are stored and available to inspect." +
                            " You can save a recent recording to make it permanently available to inspect, by hitting the 'Save' button in the top left corner and giving the recording a name",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "Why is this in CCT and not it's own mod?",
                            Answer = "Physics Inspector requires many parts of the infrastructure that is already available in CCT." +
                            " In the future it would be good to make this it's own mod, but, for the time being, it is easier to develop it as part of CCT.",
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = "Pace Ping",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "What is a Pace Ping?",
                            Answer = "A Pace Ping is a notification of a good run to a group of people interested in that." +
                            " In the context of CCT it means sending a Discord message to ping people, when you are far into a golden run.",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "Who is this feature for?",
                            Answer = "I developed this feature for streamers that want to notify their (lurking) viewers when they are on a good run, but anyone that wants to notify some group of people can use this ofcourse." +
                            "\nThe only requirement for sending a Pace Ping is that you have a Discord WebHook URL, for which you most likely need to own a Discord server."
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How do I setup Pace Pings for my Discord server?",
                            Answer = "If you own a Discord server that you want to receive Pace Pings on, follow these steps:" +
                            "\n\n1. Go to your Discord server settings and select the 'Integrations' tab" +
                            "\n2. Click on 'Create WebHook'" +
                            "\n3. Select the channel you want to receive the Pace Pings on" +
                            "\n4. Copy the WebHook URL" +
                            "\n5. Import the WebHook URL into CCT through 'Mod Options -> Pace Ping Settings -> Import WebHook URL from Clipboard'" +
                            "\n\nYou can then configure the default Pace Ping message, enable pinging when on PB pace or pick certain rooms in the map that should generate a Pace Ping when you enter them with the Golden Berry."
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "Can I have multiple different pings at different points in a map?",
                            Answer = "Yes!" +
                            "\n\nYou can enable Pace Ping on room entry for any room you want. Each room can have a different message (with different roles pinged) and settings.",
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How do I ping roles in a Pace Ping message?",
                            Answer = "To add roles in a ping message, you need to find the ID of the role you want to ping. To do this:" +
                            "\n1. Go to your Discord server" +
                            "\n2. Go into any chat" +
                            "\n3. Type '\\@<role name>'" +
                            "\n4. Hit enter (will ping said role)" +
                            "\n\nThe resulting message that you send should look like this: '<@&123456789012345678>'" +
                            "\nThis string is the roles ID. You can use this in the ping message and it will generate a ping to that role."
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "How do I use emotes in a Pace Ping message?",
                            Answer = "Same concept as pinging roles in a Pace Ping message, except you need to get the ID of the emote you want to use, in the same way that you would get the ID of a role!"
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = "Other",
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = "My issue isn't listed...",
                            Answer = "If your issue is not covered by this FAQ, feel free to message me on Discord (viddie#4751), or create an issue on GitHub (link can be found on the GameBanana page of CCT)" +
                            "\n\nIn order to help me understand the situation, please provide the following details in your message:" +
                            "\n- What version of CCT are you using?" +
                            "\n- What happened exactly? (Including what lead up to the situation you are in, e.g. what map you were playing, etc.)" +
                            "\n- What should have happened? (If the issue isn't self explanatory)" +
                            "\n- Any additional information you think might be helpful (e.g. screenshots, videos)"
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "I love this mod :D",
                            Answer = "Thanks! It took a long time to develop (roughly 300 hours at the time of writing this FAQ), but the amount of people using and liking this mod means a lot to me <3"
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = "I hate this mod >:(",
                            Answer = ":("
                        },
                    }
                },
            };

            foreach (FAQEntry.FAQSectionModel section in faq) {
                subMenu.Add(new TextMenu.SubHeader($"=== {section.Title} ==="));
                foreach (FAQEntry.FAQEntryModel entry in section.Entries) { 
                    subMenu.Add(new FAQEntry(entry.Question, entry.Answer));
                }
            }

            
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
        public ButtonBinding ButtonToggleTextOverlayText1 { get; set; }
        public ButtonBinding ButtonToggleTextOverlayText2 { get; set; }
        public ButtonBinding ButtonToggleTextOverlayText3 { get; set; }
        public ButtonBinding ButtonToggleTextOverlayText4 { get; set; }

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

        #region Test
        public int TestCount { get; set; } = 1;
        [SettingIgnore]
        public int TestSelectedLayout { get; set; } = 0;
        [SettingIgnore]
        public List<WidgetLayout> TestLayoutList { get; set; } = new List<WidgetLayout>() {
            new WidgetLayout(),
        };
        public void CreateTestCountEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Test Overlay Settings", false);

            //Remove all entries in TestLayoutList that are over TestCount
            if (TestLayoutList.Count > TestCount) {
                TestLayoutList.RemoveRange(TestCount, TestLayoutList.Count - TestCount);
            }
            if (TestSelectedLayout < 0) TestSelectedLayout = 0;
            if (TestSelectedLayout > TestLayoutList.Count - 1) TestSelectedLayout = TestLayoutList.Count - 1;

            // ========= General Settings =========
            TextMenu.Slider textCountSlider = new TextMenu.Slider("Text Count", (v) => v == 1 ? $"1 Text" : $"{v} Texts", 1, 100, TestCount);
            TextMenu.Slider selectedTextSlider = new TextMenu.Slider("Selected Text", (v) => $"Text {v+1}", 0, TestCount - 1, TestCount);

            // ========== Text 1 ==========
            TextMenu.OnOff onOffEnabled = new TextMenu.OnOff("Text 1 Enabled", IngameOverlayText1Enabled) {
                OnValueChange = v => {
                    if (TestSelectedLayout < 0 || TestSelectedLayout > TestLayoutList.Count - 1) return;
                    TestLayoutList[TestSelectedLayout].Enabled = v;
                }
            };
            TextMenuExt.EnumSlider<LayoutAnchor> sliderPosition = new TextMenuExt.EnumSlider<LayoutAnchor>("Position", TestLayoutList[TestSelectedLayout].Anchor) {
                OnValueChange = v => {
                    if (TestSelectedLayout < 0 || TestSelectedLayout > TestLayoutList.Count - 1) return;
                    TestLayoutList[TestSelectedLayout].Anchor = v;
                }
            };
            TextMenu.OnOff onOffHideInGolden = new TextMenu.OnOff("Hide In Golden Run", TestLayoutList[TestSelectedLayout].HideWithGolden) {
                OnValueChange = v => {
                    if (TestSelectedLayout < 0 || TestSelectedLayout > TestLayoutList.Count - 1) return;
                    TestLayoutList[TestSelectedLayout].HideWithGolden = v;
                }
            };
            TextMenuExt.EnumerableSlider<int> sliderSize = new TextMenuExt.EnumerableSlider<int>("Size", PercentageSlider(5, 5, 500), TestLayoutList[TestSelectedLayout].Size) {
                OnValueChange = (v) => {
                    if (TestSelectedLayout < 0 || TestSelectedLayout > TestLayoutList.Count - 1) return;
                    TestLayoutList[TestSelectedLayout].Size = v;
                }
            };
            TextMenuExt.IntSlider sliderOffsetX = new TextMenuExt.IntSlider("Offset X", 0, 2000, TestLayoutList[TestSelectedLayout].OffsetX) {
                OnValueChange = (v) => {
                    if (TestSelectedLayout < 0 || TestSelectedLayout > TestLayoutList.Count - 1) return;
                    TestLayoutList[TestSelectedLayout].OffsetX = v;
                }
            };
            TextMenuExt.IntSlider sliderOffsetY = new TextMenuExt.IntSlider("Offset Y", 0, 2000, TestLayoutList[TestSelectedLayout].OffsetY) {
                OnValueChange = (v) => {
                    if (TestSelectedLayout < 0 || TestSelectedLayout > TestLayoutList.Count - 1) return;
                    TestLayoutList[TestSelectedLayout].OffsetY = v;
                }
            };

            Action<WidgetLayout> displayLayout = (layout) => {
                onOffEnabled.PreviousIndex = onOffEnabled.Index = GetIndexOfOptionValueList(onOffEnabled.Values, layout.Enabled);
                sliderPosition.PreviousIndex = sliderPosition.Index = GetIndexOfOptionValueList(sliderPosition.Values, layout.Anchor);
                onOffHideInGolden.PreviousIndex = onOffHideInGolden.Index = GetIndexOfOptionValueList(onOffHideInGolden.Values, layout.HideWithGolden);
                sliderSize.PreviousIndex = sliderSize.Index = GetIndexOfOptionValueList(sliderSize.Values, layout.Size);
                sliderOffsetX.PreviousIndex = sliderOffsetX.Index = layout.OffsetX;
                sliderOffsetY.PreviousIndex = sliderOffsetY.Index = layout.OffsetY;

                onOffEnabled.SelectWiggler.Start();
                sliderPosition.SelectWiggler.Start();
                onOffHideInGolden.SelectWiggler.Start();
                sliderSize.SelectWiggler.Start();
                sliderOffsetX.SelectWiggler.Start();
                sliderOffsetY.SelectWiggler.Start();
            };


            textCountSlider.OnValueChange = v => {
                TestCount = v;
                for (int i = 0; i < selectedTextSlider.Values.Count; i++) {
                    Tuple<string, int> value = selectedTextSlider.Values[i];
                    if (value.Item2 + 1 > v) {
                        if (selectedTextSlider.Index == i) {
                            int index = Math.Max(0, i - 1);
                            selectedTextSlider.Index = index;
                            selectedTextSlider.PreviousIndex = index;

                            TestSelectedLayout = index;
                            displayLayout(TestLayoutList[TestSelectedLayout]);
                        }
                        selectedTextSlider.Values.Remove(value);
                    }
                }

                while (v > TestLayoutList.Count) {
                    TestLayoutList.Add(new WidgetLayout());
                }
                while (v > selectedTextSlider.Values.Count) {
                    Tuple<string, int> last = selectedTextSlider.Values[selectedTextSlider.Values.Count - 1];
                    selectedTextSlider.Values.Add(Tuple.Create($"Text {last.Item2 + 2}", last.Item2 + 1));
                }
            };

            selectedTextSlider.OnValueChange = v => {
                TestSelectedLayout = v;
                WidgetLayout layout = TestLayoutList[TestSelectedLayout];
                displayLayout(layout);
            };


            subMenu.Add(new TextMenu.SubHeader("=== General Settings ==="));
            subMenu.Add(textCountSlider);
            subMenu.Add(selectedTextSlider);

            subMenu.Add(new TextMenu.SubHeader("=== Text Settings ==="));
            subMenu.Add(onOffEnabled);
            subMenu.Add(sliderPosition);
            subMenu.Add(onOffHideInGolden);
            subMenu.Add(sliderSize);
            subMenu.Add(sliderOffsetX);
            subMenu.Add(sliderOffsetY);

            //menu.Add(subMenu);
        }

        /// <summary>
        /// Returns the index of the searched value in the list, or 0 if the search value wasn't found
        /// </summary>
        public int GetIndexOfOptionValueList<T>(List<Tuple<string, T>> values, T search) {
            for (int i = 0; i < values.Count; i++) {
                Tuple<string, T> value = values[i];
                if (value.Item2.Equals(search)) return i;
            }

            return 0;
        }
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
