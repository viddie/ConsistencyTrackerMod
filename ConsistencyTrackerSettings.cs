using Celeste.Mod.ConsistencyTracker.Entities;
using Celeste.Mod.ConsistencyTracker.Entities.Menu;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Celeste.Mod.ConsistencyTracker.Entities.WidgetLayout;
using static Celeste.Mod.ConsistencyTracker.Utility.PacePingManager;

namespace Celeste.Mod.ConsistencyTracker
{
    [SettingName("MODOPTION_CCT_NAME")]
    public class ConsistencyTrackerSettings : EverestModuleSettings {

        [SettingIgnore]
        private ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        #region General Settings

        [SettingIgnore]
        public bool IsInRun { get; set; }
        [SettingIgnore]
        public bool PauseDeathTracking { get; set; }
        public void CreatePauseDeathTrackingEntry(TextMenu menu, bool inGame) {
            menu.Add(new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PAUSE_DEATH_TRACKING"), PauseDeathTracking) {
                OnValueChange = v => {
                    PauseDeathTracking = v;
                    Mod.SaveChapterStats();
                }
            });
        }

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
        [SettingIgnore]
        public bool RoomDifficultyAllSegments { get; set; } = true;
        [SettingIgnore]
        public int SelectedFgr { get; set; } = 0;
        [SettingIgnore]
        public bool FgrContinuousSessionTimer { get; set; } = true;
        
        public void CreateRecordPathEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu fgrSubMenu = new TextMenuExt.SubMenu("FGR Path Settings", false);
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_TITLE"), false);
            menu.Add(fgrSubMenu);
            menu.Add(subMenu);
            TextMenu.Item menuItem;

            if (!inGame) {
                fgrSubMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_NOT_IN_GAME_HINT"), false));
                subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_NOT_IN_GAME_HINT"), false));
                return;
            }
            
            // Path recorder button definitions need to be up here so FGR slider can control them
            ColoredButton startPathRecordingButton = new ColoredButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_START")) {
                HighlightColor = Color.Yellow,
                Disabled = Mod.DoRecordPath || Mod.DebugMapUtil.IsRecording || SelectedFgr != 0,
            }; 
            ColoredButton altStartPathRecordingButton = new ColoredButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_ALT_RECORDING_START")) {
                HighlightColor = Color.Yellow,
                Disabled = Mod.DoRecordPath || Mod.DebugMapUtil.IsRecording || SelectedFgr != 0,
            };
            
            // FGR settings
            int highestFgr = ConsistencyTrackerModule.GetHighestFgrWithPath();
            TextMenu.OnOff isInRunItem = new TextMenu.OnOff("Is In Run", IsInRun) {
                OnValueChange = v => {
                    if (v) {
                        Mod.StartNewRun();
                    } else {
                        Mod.EndRun();
                    }

                    Mod.SaveChapterStats();
                },
                Disabled = !Mod.IsInFgrMode
            };

            TextMenu.Slider fgrSelector = new TextMenu.Slider("Selected Full Game Run Path", i => {
                return i == 0 ? "None" : $"FGR {i}";
            }, 0, Math.Max(0, highestFgr), SelectedFgr) {
                OnValueChange = value => {
                    SelectedFgr = value;
                    Mod.ChangedSelectedFgr();
                    isInRunItem.Disabled = !Mod.IsInFgrMode;
                    isInRunItem.Index = Mod.IsInGoldenRun ? 1 : 0;
                    startPathRecordingButton.Disabled = value != 0;
                    altStartPathRecordingButton.Disabled = value != 0;
                },
                Disabled = Mod.DoRecordPath || Mod.DebugMapUtil.IsRecording,
            };
            fgrSubMenu.Add(fgrSelector);
            fgrSubMenu.AddDescription(menu, fgrSelector, "When a full game run (FGR) is selected, the path recorder will be unavailable." +
                                                   "\nCreate an FGR path through the console command 'cct-fgr'");
            fgrSubMenu.Add(isInRunItem);
            fgrSubMenu.Add(menuItem = new TextMenu.OnOff("Continuous Session Timer", FgrContinuousSessionTimer) {
                OnValueChange = value => {
                    FgrContinuousSessionTimer = value;
                }
            });
            fgrSubMenu.AddDescription(menu, menuItem, "When enabled and in a run, the session timer will keep running between chapters.");
            
            // Path management section
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_TITLE")} ==="));
            bool hasPathList = Mod.CurrentChapterPathSegmentList != null;
            int segmentCount = hasPathList ? Mod.CurrentChapterPathSegmentList.Segments.Count : 0;
            var segmentList = new List<KeyValuePair<int, string>>() { 
                new KeyValuePair<int, string>(0, "Default"),
            };
            if (hasPathList) {
                segmentList.Clear();
                for (int i = 0; i < Mod.CurrentChapterPathSegmentList.Segments.Count; i++) {
                    PathSegment segment = Mod.CurrentChapterPathSegmentList.Segments[i];
                    segmentList.Add(new KeyValuePair<int, string>(i, segment.Name));
                }
            }
            var sliderCurrentSegment = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_CURRENT_SEGMENT"), segmentList, Mod.SelectedPathSegmentIndex) {
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
                    if (segment == null) return;
                    sliderCurrentSegment.Values.Add(Tuple.Create(segment.Name, Mod.CurrentChapterPathSegmentList.Segments.Count - 1));
                    sliderCurrentSegment.SelectWiggler.Start();
                },
                Disabled = !hasPathList
            });
            
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_GENERAL_IMPORT_SEGMENT")) { 
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log("Importing segment name from clipboard...");
                    try {
                        bool renamed = Mod.SetCurrentChapterPathSegmentName(text);
                        if (!renamed) return;
                        sliderCurrentSegment.Values[Mod.SelectedPathSegmentIndex] = Tuple.Create(text, Mod.SelectedPathSegmentIndex);
                        sliderCurrentSegment.SelectWiggler.Start();
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import segment name from clipboard: {ex}");
                    }
                },
            });

            
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_TITLE")} ==="));

            string recorderStateTitle = Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OFF");
            if (Mod.DoRecordPath) {
                recorderStateTitle = $"{Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_ON")}\n-----\n{Mod.PathRec.GetRecorderStatus()}";
            } else if (Mod.DebugMapUtil.IsRecording) {
                recorderStateTitle = $"{Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_ON")}\n-----\n{Mod.DebugMapUtil.PathRec.GetRecorderStatus()}";
            }

            TextMenu.SubHeader recorderStateHeader = new TextMenu.SubHeader($"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {recorderStateTitle}", topPadding:false);
            DoubleConfirmButton savePathRecordingButton = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_SAVE")) {
                HighlightColor = Color.Yellow,
                Disabled = !Mod.DoRecordPath && !Mod.DebugMapUtil.IsRecording,
            };
            DoubleConfirmButton abortPathRecordingButton = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_ABORT")) {
                HighlightColor = Color.Red,
                Disabled = !Mod.DoRecordPath && !Mod.DebugMapUtil.IsRecording,
            };

            startPathRecordingButton.OnPressed = () => {
                Mod.Log($"Started path recorder...");
                Mod.DoRecordPath = true;
                Mod.SaveChapterStats();

                startPathRecordingButton.Disabled = true;
                altStartPathRecordingButton.Disabled = true;
                savePathRecordingButton.Disabled = false;
                abortPathRecordingButton.Disabled = false;
                fgrSelector.Disabled = true;

                recorderStateHeader.Title = $"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_ON")}";
            };
            altStartPathRecordingButton.OnPressed = () => {
                Mod.Log($"Started Debug Map path recorder...");
                Mod.DebugMapUtil.StartRecording();
                Mod.SaveChapterStats();

                startPathRecordingButton.Disabled = true;
                altStartPathRecordingButton.Disabled = true;
                savePathRecordingButton.Disabled = false;
                abortPathRecordingButton.Disabled = false;
                fgrSelector.Disabled = true;

                recorderStateHeader.Title = $"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_ON")}";
            };
            savePathRecordingButton.OnDoubleConfirmation = () => {
                Mod.Log($"Saving path...");
                if (Mod.DoRecordPath) {
                    Mod.DoRecordPath = false;
                } else {
                    Mod.DebugMapUtil.StopRecording();
                }

                startPathRecordingButton.Disabled = false;
                altStartPathRecordingButton.Disabled = false;
                savePathRecordingButton.Disabled = true;
                abortPathRecordingButton.Disabled = true;
                fgrSelector.Disabled = false;

                recorderStateHeader.Title = $"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OFF")}";
            };
            abortPathRecordingButton.OnDoubleConfirmation = () => {
                Mod.Log($"Aborting path recording...");
                if (Mod.DoRecordPath) {
                    Mod.AbortPathRecording = true;
                    Mod.DoRecordPath = false;
                } else {
                    Mod.DebugMapUtil.AbortRecording();
                }

                startPathRecordingButton.Disabled = false;
                altStartPathRecordingButton.Disabled = false;
                savePathRecordingButton.Disabled = true;
                abortPathRecordingButton.Disabled = true;
                fgrSelector.Disabled = false;

                recorderStateHeader.Title = $"{Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_STATE")}: {Dialog.Clean("MODOPTION_CCT_TRACKING_SETTINGS_OFF")}";
            };

            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_TITLE_HINT_1"), false));
            subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_RECORDING_TITLE_HINT_2"), false));

            subMenu.Add(startPathRecordingButton);
            subMenu.Add(altStartPathRecordingButton);
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
                    Mod.SaveActivePath();
                    Mod.StatsManager.AggregateStatsPassOnce(Mod.CurrentChapterPath);
                    Mod.SaveChapterStats();//Path changed, so force a stat recalculation
                },
                Disabled = !hasCurrentRoom
            });
            
            CreateRoomDifficultySection(subMenu);
            
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_EDITING_IMPORT_CLIPBOARD")) { 
                OnPressed = () => {
                    string text = TextInput.GetClipboardText().Trim();
                    Mod.Log($"Importing custom room name from clipboard...");
                    try {
                        Mod.SetCustomRoomName(text);
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
                        Mod.SaveActivePath();
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
        }


        private string FormatDifficultyHelpLabel() {
            if (Mod.CurrentChapterPath == null || Mod.CurrentChapterPath.CurrentRoom == null) {
                return "";
            }

            int roomWeight = Mod.CurrentChapterPath.CurrentRoom.DifficultyWeight;
            int cpWeight = Mod.CurrentChapterPath.CurrentRoom.Checkpoint.Stats.DifficultyWeight;
            int chapterWeight = Mod.CurrentChapterPath.Stats.DifficultyWeight;
            double roomInCpRatio = (double)roomWeight / cpWeight;
            double roomInChapterRatio = (double)roomWeight / chapterWeight;
            double cpInChapterRatio = (double)cpWeight / chapterWeight;

            return $"Total CP difficulty: {cpWeight} (This Room: {roomInCpRatio:P})\n" +
                   $"Total chapter difficulty: {chapterWeight} (This Room: {roomInChapterRatio:P}, This CP: {cpInChapterRatio:P})\n" +
                   $"Check out the console commands 'cct-diff' and 'cct-diff-all'\n" +
                   $"for more efficient ways to set the difficulty weight.";
        }
        private void CreateRoomDifficultySection(TextMenuExt.SubMenu subMenu) {
            int currentRoomDifficultyWeight = Mod.CurrentChapterPath?.CurrentRoom?.DifficultyWeight ?? 0;
            TextMenu.SubHeader diffLabel = new TextMenu.SubHeader(FormatDifficultyHelpLabel(), topPadding: false);
            subMenu.Add(new TextMenu.Slider("Room Difficulty Weight", i => i.ToString(), -1, 1000, currentRoomDifficultyWeight) {
                OnValueChange = (value) => {
                    Mod.ChangeRoomDifficultyWeight(value);
                    diffLabel.Title = FormatDifficultyHelpLabel();
                },
                Disabled = Mod.CurrentChapterPath?.CurrentRoom == null
            });
            subMenu.Add(diffLabel);
            subMenu.Add(new TextMenu.OnOff("Apply Difficulty Changes To All Segments", RoomDifficultyAllSegments) {
                OnValueChange = (value) => {
                    RoomDifficultyAllSegments = value;
                },
                Disabled = Mod.CurrentChapterPath?.CurrentRoom == null
            });
        }
        #endregion

        #region Path Golden Tier Settings
        public bool PathGoldenTierSettings { get; set; } = false;
        
        public void CreatePathGoldenTierSettingsEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Path Golden Tier Settings", false);
            menu.Add(subMenu);
            TextMenu.Item menuItem;

            if (!inGame) {
                subMenu.Add(new TextMenu.SubHeader(Dialog.Clean("MODOPTION_CCT_PATH_MANAGEMENT_NOT_IN_GAME_HINT"), false));
                return;
            }
            
            CreateTierSection(subMenu);
        }

        private string FormatGpHelpString(GoldenTier selectedTier, int gpValue) {
            if (gpValue <= 0) {
                if (selectedTier.Sort == -1) {
                    return $"Undetermined doesn't give any points.";
                } else if (selectedTier.Sort == 0) {
                    return $"Untiered doesn't give any points.";
                }
                return $"{selectedTier.GetTierString()} = {Math.Round(selectedTier.GetGp(), 2)} gp";
            }
            GoldenTier calculatedTier = GoldenTier.GetTierByGp(gpValue);
            return $"{gpValue} gp = {calculatedTier.GetTierString(true)}";
        }
        private void CreateTierSection(TextMenuExt.SubMenu subMenu) {
            bool hasPath = Mod.CurrentChapterPath != null;
            GoldenTier chapterTier = hasPath ? Mod.CurrentChapterPath.Tier : new GoldenTier(-1);
            int gpValue = hasPath ? Mod.CurrentChapterPath.GoldenPoints : -1;
            int enduranceFactor = hasPath ? Mod.CurrentChapterPath.EnduranceFactor : 3;
            int endurancePower = hasPath ? Mod.CurrentChapterPath.EndurancePower : 15;
            double calculatedMaxGP = hasPath ? (gpValue == -1 ? chapterTier.GetGp() : gpValue) : -1;
            EnduranceGraph enduranceGraph = new EnduranceGraph(1 + enduranceFactor / 10f, endurancePower / 10f, calculatedMaxGP);
            TextMenu.SubHeader gpSubHeader = new TextMenu.SubHeader(FormatGpHelpString(chapterTier, gpValue), topPadding: false);
            
            subMenu.Add(new TextMenuExt.EnumerableSlider<GoldenTier>("Golden Tier", GoldenTier.GetTiers(), chapterTier) {
                OnValueChange = (newValue) => {
                    Mod.CurrentChapterPath.Tier = newValue;
                    Mod.SaveActivePath();
                    Mod.SaveChapterStats();//Path changed, so force a stat recalculation
                    gpSubHeader.Title = FormatGpHelpString(newValue, gpValue);
                    double calcMaxGP = hasPath ? (gpValue == -1 ? newValue.GetGp() : gpValue) : -1;
                    enduranceGraph.MaxGP = calcMaxGP;
                },
                Disabled = !hasPath
            });
            subMenu.Add(new TextMenuExt.IntSlider("GP Value", -1, 99999, gpValue) {
                OnValueChange = (value) => {
                    Mod.CurrentChapterPath.GoldenPoints = value;
                    Mod.SaveActivePath();
                    Mod.SaveChapterStats();//Path changed, so force a stat recalculation
                    gpSubHeader.Title = FormatGpHelpString(chapterTier, value);
                    double calcMaxGP = hasPath ? (value == -1 ? chapterTier.GetGp() : value) : -1;
                    enduranceGraph.MaxGP = calcMaxGP;
                },
                Disabled = !hasPath
            });
            subMenu.Add(gpSubHeader);
            TextMenu.Slider enduranceSlider = new TextMenu.Slider("Endurance Slope", i => Math.Round(i / 10f + 1, 1).ToString(), 0, 100, enduranceFactor) {
                OnValueChange = (value) => {
                    Mod.CurrentChapterPath.EnduranceFactor = value;
                    Mod.SaveActivePath();
                    Mod.SaveChapterStats();//Path changed, so force a stat recalculation
                    enduranceGraph.TargetSlope = 1 + value / 10f;
                },
                Disabled = !hasPath
            };
            subMenu.Add(enduranceSlider);
            subMenu.AddDescription(subMenu.Container, enduranceSlider,
                                   "This value defines the slope of the endurance curve.\n" +
                                   "Lower value = Starting/Ending rooms are worth more. Middle rooms are worth less.\n" +
                                   "Higher value = Starting/Ending rooms are worth less. Middle rooms are worth more.");
            TextMenu.Slider endurancePowerSlider = new TextMenu.Slider("Endurance Power", i => Math.Round(i / 10f, 2).ToString(), 1, 100, endurancePower) {
                OnValueChange = (value) => {
                    Mod.CurrentChapterPath.EndurancePower = value;
                    Mod.SaveActivePath();
                    Mod.SaveChapterStats();//Path changed, so force a stat recalculation
                    enduranceGraph.TargetPower = value / 10f;
                },
                Disabled = !hasPath
            };
            subMenu.Add(endurancePowerSlider);
            subMenu.AddDescription(subMenu.Container, endurancePowerSlider, "This value defines the power applied to the endurance curve.\n" +
                                                                       "Lower value = Starting rooms are worth more. Middle/Ending rooms are worth less.\n" +
                                                                       "Higher value = Starting rooms are worth less. Middle/Ending rooms are worth more.");
            subMenu.Add(enduranceGraph);
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

            string attemptNote = "Removes practice clears/deaths, NOT golden runs.";
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_TITLE")} ==="));
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_LAST_ATTEMPT")) {
                OnPressed = () => {
                    Mod.RemoveLastAttempt();
                },
                Disabled = !hasCurrentRoom
            });
            subMenu.AddDescription(menu, menuItem, attemptNote);

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_LAST_DEATH_STREAK")) {
                OnPressed = () => {
                    Mod.RemoveLastDeathStreak();
                },
                Disabled = !hasCurrentRoom
            });
            subMenu.AddDescription(menu, menuItem, attemptNote);

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_REMOVE_ALL_ATTEMPTS")) {
                OnPressed = () => {
                    Mod.WipeRoomData();
                },
                Disabled = !hasCurrentRoom
            });
            subMenu.AddDescription(menu, menuItem, attemptNote);

            subMenu.Add(menuItem = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_REMOVE_GOLDEN_BERRY_DEATH")) {
                OnDoubleConfirmation = () => {
                    Mod.RemoveRoomGoldenBerryDeaths(true);
                },
                Disabled = !hasCurrentRoom,
                HighlightColor = Color.Red,
            });
            subMenu.AddDescription(menu, menuItem, $"This will not update the session history.");
            
            subMenu.Add(menuItem = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_DATA_WIPE_ROOM_REMOVE_REMOVE_GOLDEN_BERRY_DEATHS")) {
                OnDoubleConfirmation = () => {
                    Mod.RemoveRoomGoldenBerryDeaths();
                },
                Disabled = !hasCurrentRoom,
                HighlightColor = Color.Red,
            });
            subMenu.AddDescription(menu, menuItem, $"This will not update the session history.");


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
                    ResourceUnpacker.CheckPrepackagedPaths(reset:true);
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

            List<KeyValuePair<CustomNameBehavior, string>> CustomNameBehaviors = new List<KeyValuePair<CustomNameBehavior, string>>() {
                new KeyValuePair<CustomNameBehavior, string>(CustomNameBehavior.Append, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CUSTOM_ROOM_NAME_BEHAVIOR_APPEND")),
                new KeyValuePair<CustomNameBehavior, string>(CustomNameBehavior.Ignore, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CUSTOM_ROOM_NAME_BEHAVIOR_IGNORE")),
                new KeyValuePair<CustomNameBehavior, string>(CustomNameBehavior.Override, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CUSTOM_ROOM_NAME_BEHAVIOR_OVERRIDE")),
                new KeyValuePair<CustomNameBehavior, string>(CustomNameBehavior.Prepend, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CUSTOM_ROOM_NAME_BEHAVIOR_PREPEND")),
            };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<CustomNameBehavior>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_SETTINGS_CUSTOM_ROOM_NAME_BEHAVIOR"), CustomNameBehaviors, LiveDataCustomNameBehavior) { 
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

            List<KeyValuePair<LowDeathBehavior, string>> LowDeathBehaviors = new List<KeyValuePair<LowDeathBehavior, string>>() {
                new KeyValuePair<LowDeathBehavior, string>(LowDeathBehavior.AlwaysCheckpoints, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_LOW_DEATH_DISPLAY_BEHAVIOR_ALWAYS_CHECKPOINTS")),
                new KeyValuePair<LowDeathBehavior, string>(LowDeathBehavior.AlwaysRooms, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_LOW_DEATH_DISPLAY_BEHAVIOR_ALWAYS_ROOMS")),
                new KeyValuePair<LowDeathBehavior, string>(LowDeathBehavior.Adaptive, Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_LOW_DEATH_DISPLAY_BEHAVIOR_ADAPTIVE")),
            };
            subMenu.Add(menuItem = new TextMenuExt.EnumerableSlider<LowDeathBehavior>(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_STATS_SETTINGS_LOW_DEATH_DISPLAY_BEHAVIOR"), LowDeathBehaviors, LiveDataStatLowDeathBehavior) {
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
                    string relPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "LiveDataEditor", "LiveDataEditTool.html");
                    string path = System.IO.Path.GetFullPath(relPath);
                    Mod.LogVerbose($"Opening format editor at '{path}'");
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                },
            });
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_LIVE_DATA_FORMAT_EDITING_OPEN_FORMAT_TEXT_FILE")).Pressed(() => {
                string relPath = ConsistencyTrackerModule.GetPathToFile(StatManager.BaseFolder, StatManager.FormatFileName);
                string path = System.IO.Path.GetFullPath(relPath);
                Mod.LogVerbose($"Opening format file at '{path}'");
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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
                string path = System.IO.Path.GetFullPath(ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "ExternalOverlay", "CCTOverlay.html"));
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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
        [SettingIgnore]
        public bool ShowRoomDifficultiesOnDebugMap { get; set; } = true;
        
        #region Graph Overlay
        //Graph Overlay
        [SettingIgnore]
        public bool IngameOverlayGraphEnabled { get; set; } = false;
        
        [SettingIgnore]
        public StatTextPosition IngameOverlayGraphPosition { get; set; } = StatTextPosition.BottomCenter;
        
        [SettingIgnore]
        public int IngameOverlayGraphWidth { get; set; } = 300;
        
        [SettingIgnore]
        public int IngameOverlayGraphHeight { get; set; } = 35;
        
        [SettingIgnore]
        public int IngameOverlayGraphOffsetX { get; set; } = 5;
        
        [SettingIgnore]
        public int IngameOverlayGraphOffsetY { get; set; } = 1;
        
        [SettingIgnore]
        public int IngameOverlayGraphBarSpacing { get; set; } = 1;
        
        [SettingIgnore]
        public int IngameOverlayGraphRoomsPadding { get; set; } = 50;
        
        [SettingIgnore]
        public bool IngameOverlayGraphShowCheckpointIndicator { get; set; } = true;
        
        [SettingIgnore]
        public bool IngameOverlayGraphShowGoldenPbBar { get; set; } = true;
        
        [SettingIgnore]
        public bool IngameOverlayGraphCurrentRoomExplicit { get; set; } = true;
        
        [SettingIgnore]
        public int IngameOverlayGraphHiddenRoomsIndicatorWidth { get; set; } = 2;
        
        [SettingIgnore]
        public int IngameOverlayGraphBackgroundDim { get; set; } = 3;
        #endregion
        

        #region Text Overlay Settings
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
        #endregion


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
            subMenu.Add(new TextMenu.OnOff("Show Room Difficulties On Debug Map", ShowRoomDifficultiesOnDebugMap) {
                OnValueChange = v => {
                    ShowRoomDifficultiesOnDebugMap = v;
                }
            });
            
            
            List<KeyValuePair<StatTextPosition, string>> statTextPositions = new List<KeyValuePair<StatTextPosition, string>>() {
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.TopLeft, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_TOP_LEFT")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.TopCenter, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_TOP_CENTER")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.TopRight, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_TOP_RIGHT")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.MiddleLeft, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_MIDDLE_LEFT")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.MiddleCenter, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_MIDDLE_CENTER")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.MiddleRight, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_MIDDLE_RIGHT")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.BottomLeft, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_BOTTOM_LEFT")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.BottomCenter, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_BOTTOM_CENTER")),
                new KeyValuePair<StatTextPosition, string>(StatTextPosition.BottomRight, Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION_BOTTOM_RIGHT")),
            };

            
            // ========== Graph ==========
            subMenu.Add(new TextMenu.SubHeader($"=== Chapter Graph ==="));
            subMenu.Add(new TextMenu.OnOff("Chapter Graph Enabled", IngameOverlayGraphEnabled) {
                OnValueChange = v => {
                    IngameOverlayGraphEnabled = v;
                }
            });
            subMenu.Add(new TextMenuExt.EnumerableSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), statTextPositions, IngameOverlayGraphPosition) {
                OnValueChange = v => {
                    IngameOverlayGraphPosition = v;
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Graph Width", 0, 2000, IngameOverlayGraphWidth) {
                OnValueChange = (value) => {
                    IngameOverlayGraphWidth = value;
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Graph Height", 0, 2000, IngameOverlayGraphHeight) {
                OnValueChange = (value) => {
                    IngameOverlayGraphHeight = value;
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Graph Offset X", 0, 2000, IngameOverlayGraphOffsetX) {
                OnValueChange = (value) => {
                    IngameOverlayGraphOffsetX = value;
                }
            });
            subMenu.Add(new TextMenuExt.IntSlider("Graph Offset Y", 0, 2000, IngameOverlayGraphOffsetY) {
                OnValueChange = (value) => {
                    IngameOverlayGraphOffsetY = value;
                }
            });
            subMenu.Add(new TextMenu.Slider("Graph Bar Spacing", (i) => $"{i} Pixel(s)", 0, 100, IngameOverlayGraphBarSpacing) {
                OnValueChange = (value) => {
                    IngameOverlayGraphBarSpacing = value;
                }
            });
            subMenu.Add(menuItem = new TextMenu.Slider("Graph Room Padding", (i) => $"{i} Room(s)", 0, 1000, IngameOverlayGraphRoomsPadding) {
                OnValueChange = (value) => {
                    IngameOverlayGraphRoomsPadding = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "The amount of rooms to show on the graph AROUND the current room." +
                                                   "\nExample: If set to 10 it will show the current room + 20 rooms (10 before and 10 after)." +
                                                   "\nIf you are less than 10 rooms away from the start or end of the chapter, it will still show 21 rooms.");
            
            subMenu.Add(menuItem = new TextMenu.OnOff("Graph Show Checkpoint Indicator", IngameOverlayGraphShowCheckpointIndicator) {
                OnValueChange = v => {
                    IngameOverlayGraphShowCheckpointIndicator = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Shows a small indicator between checkpoints in the graph." +
                                                   "\nIt will be either 1 or 2 pixels in width (to make it nice and even)" +
                                                   "\nand be placed inbetween the two rooms that it connects." +
                                                   "\nIf the Bar Spacing is set to 0, it will not show up.");
            
            subMenu.Add(menuItem = new TextMenu.OnOff("Graph Show Golden PB Bar", IngameOverlayGraphShowGoldenPbBar) {
                OnValueChange = v => {
                    IngameOverlayGraphShowGoldenPbBar = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, "Shows your PB room as a golden bar at the bottom of the graph.");
            
            subMenu.Add(menuItem = new TextMenu.OnOff("Graph Current Room Indicator Explicit", IngameOverlayGraphCurrentRoomExplicit) {
                OnValueChange = v => {
                    IngameOverlayGraphCurrentRoomExplicit = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, "If enabled, no longer colors the current room red," +
                                                   "\nbut instead shows a small indicator on the bottom of the graph.");
            
            subMenu.Add(menuItem = new TextMenu.Slider("Graph Hidden Rooms Indicator Width", (i) => $"{i} Pixel(s)", 0, 20, IngameOverlayGraphHiddenRoomsIndicatorWidth) {
                OnValueChange = (value) => {
                    IngameOverlayGraphHiddenRoomsIndicatorWidth = value;
                }
            });
            subMenu.AddDescription(menu, menuItem, "If greater than 0 and if there are any rooms that aren't already visible on the graph," +
                                                   "\nan indicator will be shown on the sides of the graph where more rooms are available." +
                                                   "\nThe height of the bar is proportional to how many rooms are hidden.");
            
            subMenu.Add(new TextMenu.Slider("Graph Background Dim", (i) => $"{i*10} %", 0, 10, IngameOverlayGraphBackgroundDim) {
                OnValueChange = (value) => {
                    IngameOverlayGraphBackgroundDim = value;
                }
            });
            
            
            
            // ========== Text Overlay ==========
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

            #region Overlay Texts
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
            subMenu.Add(new TextMenuExt.EnumerableSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), statTextPositions, IngameOverlayText1Position) {
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
            subMenu.Add(new TextMenuExt.EnumerableSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), statTextPositions, IngameOverlayText2Position) {
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
            subMenu.Add(new TextMenuExt.EnumerableSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), statTextPositions, IngameOverlayText3Position) {
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
            subMenu.Add(new TextMenuExt.EnumerableSlider<StatTextPosition>(Dialog.Clean("MODOPTION_CCT_IN_GAME_OVERLAY_SETTINGS_TEXT_OVERLAY_TEXT_POSITION"), statTextPositions, IngameOverlayText4Position) {
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
            #endregion

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
        public bool LogMovableEntities { get; set; } = false;
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

        [SettingIgnore]
        public int LogMaxRecentRecordings { get; set; } = 10;

        public void CreatePhysicsLoggerSettingsEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_TITLE"), false);
            TextMenu.Item menuItem;

            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_GENERAL_TITLE")} ==="));
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_GENERAL_OPEN_IN_BROWSER")) {
                OnPressed = () => {
                    string relPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.ExternalToolsFolder, "PhysicsInspector", "PhysicsInspector.html");
                    string path = System.IO.Path.GetFullPath(relPath);
                    Mod.LogVerbose($"Opening physics inspector at '{path}'");
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
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
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_RELOAD_CUSTOM_ENTITY_NAMES")){
                OnPressed = () => {
                    Mod.PhysicsLog.LoadCustomEntityNames();
                }
            });
            subMenu.Add(new TextMenu.Slider(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_MAX_RECENT_RECORDINGS"), i => i.ToString(), 1, 1000, LogMaxRecentRecordings) {
                OnValueChange = v => {
                    LogMaxRecentRecordings = v;
                    Mod.Log($"Max recent recordings set to {v}");
                }
            });
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_LOG_MOVABLE_ENTITIES"), LogMovableEntities) {
                OnValueChange = v => {
                    LogMovableEntities = v;
                    Mod.Log($"Logging movable entities {(v ? "enabled" : "disabled")}");
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PHYSICS_INSPECTOR_SETTINGS_SETTINGS_LOG_MOVABLE_ENTITIES_HINT"));
            
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
        public bool PacePingAllDeathsEnabled { get; set; } = false;

        private void PacePingLiveUpdate(TextMenuExt.EnumerableSlider<int> sliderCurrentPing = null,
                                        TextMenu.OnOff togglePacePingButton = null,
                                        TextMenu.Button testButton = null,
                                        TextMenu.Button importMessageButton = null,
                                        TextMenu.OnOff mapSpecificPing = null,
                                        TextMenuExt.EnumerableSlider<PbPingType> pbPingSelector = null) {
            bool hasPath = Mod.CurrentChapterPath != null;
            bool hasCurrentRoom = Mod.CurrentChapterPath?.CurrentRoom != null;
            PacePingManager manager = Mod.MultiPacePingManager.GetSelectedPing();
            if (sliderCurrentPing != null) {
                sliderCurrentPing.Values[Mod.MultiPacePingManager.CurrSelected] = Tuple.Create(manager.State.PingName, Mod.MultiPacePingManager.CurrSelected);
                sliderCurrentPing.Index = Mod.MultiPacePingManager.CurrSelected;
            }

            PaceTiming timing = null;
            if (hasCurrentRoom)
            {
                timing = Mod.MultiPacePingManager.GetSelectedPing().GetPaceTiming(Mod.CurrentChapterPath.ChapterSID, Mod.CurrentChapterPath.CurrentRoom.DebugRoomName);
            }
            
            if (togglePacePingButton != null) {
                togglePacePingButton.Index = (timing != null) ? 1 : 0;
                togglePacePingButton.SelectWiggler.Start();
            }

            if (testButton != null) {
                testButton.Disabled = timing == null;
            }
            if (importMessageButton != null) {
                importMessageButton.Disabled = timing == null;
            }

            MapSettings mapSpecificSettings = manager.CurrentMapSettings;

            if (mapSpecificPing != null) {
                mapSpecificPing.Index = mapSpecificSettings.PingsEnabled ? 1 : 0;
                mapSpecificPing.SelectWiggler.Start();
            }

            if (pbPingSelector != null) {
                pbPingSelector.Index = (int)mapSpecificSettings.PbPingType;
            }
        }

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
                paceTiming = Mod.MultiPacePingManager.GetSelectedPing().GetPaceTiming(Mod.CurrentChapterPath.ChapterSID, Mod.CurrentChapterPath.CurrentRoom.DebugRoomName);
            }


            TextMenu.OnOff mapSpecificPing;
            MapSettings mapSpecificSettings = Mod.MultiPacePingManager.GetSelectedPing().CurrentMapSettings;
            mapSpecificPing = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_MAP_SPECIFIC_PING_ENABLED"), mapSpecificSettings.PingsEnabled)
            {
                OnValueChange = v =>
                {
                    MapSettings currMapSettings = Mod.MultiPacePingManager.GetSelectedPing().CurrentMapSettings;
                    currMapSettings.PingsEnabled = v;
                    Mod.MultiPacePingManager.GetSelectedPing().CurrentMapSettings = currMapSettings;
                },
                Disabled = !hasPath
            };

            TextMenuExt.EnumerableSlider<PbPingType> pbPingSelector;
            List<KeyValuePair<PbPingType, string>> pbPingTypes = new List<KeyValuePair<PbPingType, string>>() {
                new KeyValuePair<PbPingType, string>(PbPingType.NoPing, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_NO")),
                new KeyValuePair<PbPingType, string>(PbPingType.PingOnPbEntry, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_ENTRY")),
                new KeyValuePair<PbPingType, string>(PbPingType.PingOnPbPassed, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_PASSED")),
            };
            pbPingSelector = new TextMenuExt.EnumerableSlider<PbPingType>(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_MAP_SPECIFIC_PB_PING"), pbPingTypes, mapSpecificSettings.PbPingType)
            {
                OnValueChange = (newValue) =>
                {
                    mapSpecificSettings.PbPingType = newValue;
                    Mod.MultiPacePingManager.GetSelectedPing().CurrentMapSettings = mapSpecificSettings;
                },
                Disabled = !hasPath
            };


            int pingCount = Mod.MultiPacePingManager.PacePingManagers.Count;
            List<KeyValuePair<int, string>> PingList = new List<KeyValuePair<int, string>>() { 
                new KeyValuePair<int, string>(0, "Default"),
            };
            
            // Probably unnecessary, but including in case I missed an edge case 
            if (pingCount > 0) {
                PingList.Clear();
                for (int i = 0; i < Mod.MultiPacePingManager.PacePingManagers.Count; i++) {
                    PacePingManager manager = Mod.MultiPacePingManager.PacePingManagers[i];
                    PingList.Add(new KeyValuePair<int, string>(i, manager.State.PingName));
                }
            }

            TextMenu.Button importMessageButton = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_MSG_IMPORT")) {
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing custom ping message from clipboard...");
                    try {
                        Mod.MultiPacePingManager.GetSelectedPing().SaveCustomPingMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import custom ping message from clipboard: {ex}");
                    }
                },
                Disabled = paceTiming == null,
            };
            TextMenu.Button testButton = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_TEST_PING")) {
                OnPressed = () => {
                    Mod.MultiPacePingManager.GetSelectedPing().TestPingForCurrentRoom();
                },
                Disabled = paceTiming == null
            };

            TextMenu.OnOff togglePacePingButton = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_PING_THIS_ROOM"), paceTiming != null) {
                OnValueChange = (isEnabled) => {
                    bool isNowEnabled = Mod.MultiPacePingManager.GetSelectedPing().SetCurrentRoomPacePingEnabled(isEnabled);
                    importMessageButton.Disabled = !isNowEnabled;
                    testButton.Disabled = !isNowEnabled;
                    PacePingLiveUpdate(importMessageButton: importMessageButton,
                                        testButton: testButton);
                },
                Disabled = !hasCurrentRoom
            };

            TextMenuExt.EnumerableSlider<int> sliderCurrentPing = new TextMenuExt.EnumerableSlider<int>(Dialog.Clean("MODOPTION_CCT_PACE_PING_GENERAL_CURRENT_PING"), PingList, Mod.MultiPacePingManager.CurrSelected) {
                OnValueChange = (newValue) => {
                    Mod.MultiPacePingManager.SetSelectedPing(newValue);
                    PacePingLiveUpdate(null,
                                        togglePacePingButton,
                                        testButton,
                                        importMessageButton,
                                        mapSpecificPing,
                                        pbPingSelector
                                        );
                },
                Disabled = pingCount == 0
            };
            subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_ENABLE"), PacePingEnabled) {
                OnValueChange = v => {
                    PacePingEnabled = v;
                }
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_2"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_3"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_4"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_HINT_5"));

            subMenu.Add(sliderCurrentPing);
            subMenu.AddDescription(menu, sliderCurrentPing, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_PING_HINT"));

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_GENERAL_ADD_PING")) {
                OnPressed = () => {
                    PacePingManager newPing = Mod.MultiPacePingManager.AddNewPing();
                    if (newPing != null) {
                        sliderCurrentPing.Values.Add(Tuple.Create(newPing.State.PingName, Mod.MultiPacePingManager.PacePingManagers.Count - 1));
                        Mod.MultiPacePingManager.SetSelectedPing(Mod.MultiPacePingManager.PacePingManagers.Count-1);
                    }
                    PacePingLiveUpdate(sliderCurrentPing,
                                        togglePacePingButton,
                                        testButton,
                                        importMessageButton,
                                        mapSpecificPing,
                                        pbPingSelector
                                        );
                }
            });

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_GENERAL_IMPORT_NAME")) { 
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing ping name from clipboard...");
                    try {
                        Mod.MultiPacePingManager.GetSelectedPing().SavePingName(text);
                        PacePingLiveUpdate( sliderCurrentPing: sliderCurrentPing);
                                            
                        }
                    catch (Exception ex) {
                        Mod.Log($"Couldn't import ping name from clipboard: {ex}");
                    }
                },
            });
            


            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_TITLE")} ==="));

            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_MSG_IMPORT")) { 
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing default ping message from clipboard...");
                    try {
                        Mod.MultiPacePingManager.GetSelectedPing().SaveDefaultPingMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import default ping message from clipboard: {ex}");
                    }
                },
            });

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_WEBHOOK_IMPORT")).Pressed(() => {
                string text = TextInput.GetClipboardText();
                Mod.Log($"Importing WebHook url from clipboard...");
                try {
                    Mod.MultiPacePingManager.GetSelectedPing().SaveDiscordWebhook(text);
                } catch (Exception ex) {
                    Mod.Log($"Couldn't import WebHook url from clipboard: {ex}");
                }
            }));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_URL_HINT"));
            
            subMenu.Add(new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_PB_PING_MSG_IMPORT")) {
                OnPressed = () => {
                    string text = TextInput.GetClipboardText();
                    Mod.Log($"Importing pb ping message from clipboard...");
                    try {
                        Mod.MultiPacePingManager.GetSelectedPing().SavePBPingMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import pb ping message from clipboard: {ex}");
                    }
                },
            });
            
            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_RELOAD_STATE_FILE")) {
                OnPressed = () => {
                        Mod.MultiPacePingManager.GetSelectedPing().ReloadStateFile();
                        // Live-update ping settings
                        string pingName = Mod.MultiPacePingManager.GetSelectedPing().State.PingName;
                        PacePingLiveUpdate(sliderCurrentPing,
                                           togglePacePingButton,
                                           testButton,
                                           importMessageButton,
                                           mapSpecificPing,
                                           pbPingSelector);

                },
            });
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_GENERAL_RELOAD_STATE_FILE_HINT"));

            //Map Specific Settings
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_MAP_SPECIFIC_HEADER")} ==="));

            subMenu.Add(menuItem = mapSpecificPing);
            subMenu.Add(pbPingSelector);

            
            string roomAddition = hasCurrentRoom ? $" ({Mod.CurrentChapterPath.CurrentRoom.GetFormattedRoomName(StatManager.RoomNameType)})" : "";
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_TITLE")}{roomAddition} ==="));

            subMenu.Add(togglePacePingButton);
            subMenu.AddDescription(menu, togglePacePingButton, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_CURRENT_ROOM_PING_THIS_ROOM_HINT"));
            subMenu.Add(importMessageButton);
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
                        Mod.MultiPacePingManager.GetSelectedPing().SaveAllDeathsMessage(text);
                    } catch (Exception ex) {
                        Mod.Log($"Couldn't import all deaths message from clipboard: {ex}");
                    }
                },
            });

            subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_WEBHOOK_IMPORT")).Pressed(() => {
                string text = TextInput.GetClipboardText();
                Mod.Log($"Importing WebHook url from clipboard...");
                try {
                    Mod.MultiPacePingManager.GetSelectedPing().SaveDiscordWebhookAllDeaths(text);
                } catch (Exception ex) {
                    Mod.Log($"Couldn't import WebHook url from clipboard: {ex}");
                }
            }));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_WEBHOOK_IMPORT_HINT_1"));
            subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_ALL_DEATHS_WEBHOOK_IMPORT_HINT_2"));
          
            subMenu.Add(new TextMenu.SubHeader($"=== {Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_DANGER_ZONE_TITLE")} ==="));
            DoubleConfirmButton deleteButton = new DoubleConfirmButton(Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_DANGER_ZONE_DELETE"))
            {
                Disabled = Mod.MultiPacePingManager.PacePingManagers.Count <= 1,
                HighlightColor = Color.Red,
            };
            deleteButton.OnDoubleConfirmation = () =>
            {
                int index = Mod.MultiPacePingManager.CurrSelected;
                bool didDelete = Mod.MultiPacePingManager.DeleteCurrentPing();

                if (didDelete)
                {
                    sliderCurrentPing.Values.RemoveAt(index);
                    PacePingLiveUpdate(sliderCurrentPing,
                                        togglePacePingButton,
                                        testButton,
                                        importMessageButton,
                                        mapSpecificPing,
                                        pbPingSelector);

                }

                deleteButton.Disabled = Mod.MultiPacePingManager.PacePingManagers.Count <= 1;
            };
            subMenu.Add(deleteButton);
            subMenu.AddDescription(menu, deleteButton, Dialog.Clean("MODOPTION_CCT_PACE_PING_SETTINGS_DANGER_ZONE_DELETE_HINT_1"));

            menu.Add(subMenu);
        }
        #endregion

        #region FAQ
        [JsonIgnore]
        public bool FAQ { get; set; } = false;

        //[SettingIgnore]
        //public bool PacePingEnabled { get; set; } = false;

        public void CreateFAQEntry(TextMenu menu, bool inGame) {
            TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu(Dialog.Clean("MODOPTION_CCT_FAQ_TITLE"), false);

            List<FAQEntry.FAQSectionModel> faq = new List<FAQEntry.FAQSectionModel>() {
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_WHAT_IS_A_PATH_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_WHAT_IS_A_PATH_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_WHAT_IS_A_PATH_SEG_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_WHAT_IS_A_PATH_SEG_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_HOW_DO_I_RECORD_A_PATH_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_HOW_DO_I_RECORD_A_PATH_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_HOW_DO_I_RENAME_CP_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_HOW_DO_I_RENAME_CP_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_HOW_DO_I_EDIT_PATH_FILE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_HOW_DO_I_EDIT_PATH_FILE_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_SCREW_UP_VANILLA_PATH_FILE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_SCREW_UP_VANILLA_PATH_FILE_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_DONT_SEE_FC_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PATH_MANAGEMENT_DONT_SEE_FC_A"),
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_STATS_MANAGEMENT_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_STATS_MANAGEMENT_WHAT_STATS_ARE_THERE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_STATS_MANAGEMENT_WHAT_STATS_ARE_THERE_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_STATS_MANAGEMENT_WHERE_ARE_THE_STATS_SAVED_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_STATS_MANAGEMENT_WHERE_ARE_THE_STATS_SAVED_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_STATS_MANAGEMENT_ACCIDENTALLY_COLLECTED_GOLDEN_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_STATS_MANAGEMENT_ACCIDENTALLY_COLLECTED_GOLDEN_A"),
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){ 
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){ 
                        new FAQEntry.FAQEntryModel(){ 
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_WHAT_IS_LIVE_DATA_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_WHAT_IS_LIVE_DATA_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_WHAT_IS_LIVE_DATA_FORMAT_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_WHAT_IS_LIVE_DATA_FORMAT_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_DONT_SEE_XY_IN_MY_LIST_OF_FORMAT_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_DONT_SEE_XY_IN_MY_LIST_OF_FORMAT_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_HOW_CAN_I_MAKE_FORMAT_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_HOW_CAN_I_MAKE_FORMAT_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_ADD_STAT_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_LIVE_DATA_ADD_STAT_A"),
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_OVERLAY_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_OVERLAY_WHY_DOES_IT_SAY_PATH_EVERYWHERE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_OVERLAY_WHY_DOES_IT_SAY_PATH_EVERYWHERE_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_REMOVE_OVERLAY_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_REMOVE_OVERLAY_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_CUSTOMIZE_FORMAT_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_CUSTOMIZE_FORMAT_A"),
                        },
                    },
                },
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_SUMMARY_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_SUMMARY_WHAT_IS_THE_IN_GAME_SUMMARY_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_SUMMARY_WHAT_IS_THE_IN_GAME_SUMMARY_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_SUMMARY_HOW_TO_USE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_IN_GAME_SUMMARY_HOW_TO_USE_A"),
                        },
                    },
                },
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_EXTERNAL_TOOLS_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_EXTERNAL_TOOLS_IS_RUNNING_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_EXTERNAL_TOOLS_IS_RUNNING_A"),
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_PHYSICS_INSPECTOR_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PHYSICS_INSPECTOR_WHAT_IS_THE_PHYSICS_INSPECTOR_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PHYSICS_INSPECTOR_WHAT_IS_THE_PHYSICS_INSPECTOR_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PHYSICS_INSPECTOR_HOW_TO_USE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PHYSICS_INSPECTOR_HOW_TO_USE_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PHYSICS_INSPECTOR_OWN_MOD_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PHYSICS_INSPECTOR_OWN_MOD_A"),
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_WHAT_IS_PACE_PING_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_WHAT_IS_PACE_PING_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_FOR_WHOM_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_FOR_WHOM_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_HOW_TO_SETUP_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_HOW_TO_SETUP_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_DIFFERENT_PING_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_DIFFERENT_PING_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_PING_ROLE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_PING_ROLE_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_EMOTE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_PACE_PING_EMOTE_A"),
                        },
                    }
                },
                new FAQEntry.FAQSectionModel(){
                    Title = Dialog.Clean("MODOPTION_CCT_FAQ_OTHER_TITLE"),
                    Entries = new List<FAQEntry.FAQEntryModel>(){
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_OTHER_NOT_LISTED_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_OTHER_NOT_LISTED_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_OTHER_LOVE_Q"),
                            Answer = Dialog.Clean("MODOPTION_CCT_FAQ_OTHER_LOVE_A"),
                        },
                        new FAQEntry.FAQEntryModel(){
                            Question = Dialog.Clean("MODOPTION_CCT_FAQ_OTHER_HATE_Q"),
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
        [SettingSubHeader("Full-Game-Run Hotkeys")]
        public ButtonBinding ButtonFgrToggleInRun { get; set; }
        public ButtonBinding ButtonFgrTeleportToPreviousMap { get; set; }
        public ButtonBinding ButtonFgrTeleportToNextMap { get; set; }
        public ButtonBinding ButtonFgrReset { get; set; }
        
        [SettingSubHeader("In-Game Overlay Hotkeys")]
        public ButtonBinding ButtonToggleTextOverlayEnabled { get; set; }
        public ButtonBinding ButtonToggleTextOverlayText1 { get; set; }
        public ButtonBinding ButtonToggleTextOverlayText2 { get; set; }
        public ButtonBinding ButtonToggleTextOverlayText3 { get; set; }
        public ButtonBinding ButtonToggleTextOverlayText4 { get; set; }
        
        public ButtonBinding ButtonToggleDifficultyGraph { get; set; }

        [SettingSubHeader("Tracking/Data Hotkeys")]
        public ButtonBinding ButtonTogglePauseDeathTracking { get; set; }
        public ButtonBinding ButtonAddRoomSuccess { get; set; }
        public ButtonBinding ButtonRemoveRoomLastAttempt { get; set; }
        public ButtonBinding ButtonRemoveRoomDeathStreak { get; set; }
        
        public ButtonBinding ButtonImportCustomRoomNameFromClipboard { get; set; }

        [SettingSubHeader("Misc Hotkeys")]
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
