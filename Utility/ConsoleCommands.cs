using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Monocle;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Celeste.Mod.ConsistencyTracker.Entities;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public static class ConsoleCommands {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        #region Commands

        [Command("cct-name", "get or set the custom room name of any room. use \"_\" for spaces in names. set the name to \"-\" to remove the name.")]
        public static void CctName(string roomName, string newName = null) {
            if (Mod.CurrentChapterPath == null) {
                ConsolePrint("No path for current map found. Please record a path first!");
                return;
            }
            if (string.IsNullOrEmpty(roomName)) {
                RoomInfo exampleRoom = Mod.CurrentChapterPath.CurrentRoom ?? Mod.CurrentChapterPath.Checkpoints[0].Rooms[Mod.CurrentChapterPath.Checkpoints[0].Rooms.Count - 1];
                string exampleName = exampleRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore);
                ConsolePrint($"Please provide a room name. Example: cct-name "+exampleName);
                return;
            }

            roomName = EscapeStringParameter(roomName);

            PathInfo path = Mod.CurrentChapterPath;
            RoomInfo foundRoom = null;
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    string rName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore);
                    if (rName != roomName) continue;

                    foundRoom = rInfo;
                    break;
                }
            }

            if (foundRoom == null) {
                ConsolePrint($"Didn't find room with name '{roomName}' in this chapter");
                return;
            }
            CheckpointInfo foundCheckpoint = foundRoom.Checkpoint;
            string foundRoomName = foundRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore);
            string customName = foundRoom.CustomRoomName;
            string cpString = $"Checkpoint {foundCheckpoint.CPNumberInChapter} ({foundCheckpoint.Name})";

            if (string.IsNullOrEmpty(newName)) {
                if (customName == null) {
                    ConsolePrint($"{cpString} - Room '{foundRoomName}' doesn't have a custom room name");
                } else {
                    ConsolePrint($"{cpString} - Room '{foundRoomName}' has custom name '{customName}'");
                }
            } else {
                newName = EscapeStringParameter(newName);
                if (newName == "-") {
                    foundRoom.CustomRoomName = null;
                    ConsolePrint($"{cpString} - Room '{foundRoomName}' custom name removed");
                } else {
                    foundRoom.CustomRoomName = newName;
                    ConsolePrint($"{cpString} - Room '{foundRoomName}' custom name set to '{newName}'");
                }
                Mod.SaveActivePath();
                Mod.SaveChapterStats();
            }
        }

        [Command("cct-list-names", "lists all room names of the current chapter.")]
        public static void CctListNames() {
            if (Mod.CurrentChapterPath == null) {
                ConsolePrint("No path for current map found. Please record a path first!");
                return;
            }

            PathInfo path = Mod.CurrentChapterPath;
            List<string> checkpointLines = new List<string>();
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                string line = $"- {cpInfo.Name} ({cpInfo.Abbreviation}) [{cpInfo.RoomCount} Rooms]: ";
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    string rName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType);
                    line += $"{rName}, ";
                }
                line = line.Substring(0, line.Length - 2);
                checkpointLines.Add(line);
            }

            string joined = string.Join("\n", checkpointLines);
            ConsolePrint($"All rooms in '{path.ChapterDisplayName}' [{path.RoomCount} Rooms]:\n{joined}");
        }

        [Command("cct-export-room-time", "lists and exports the time spent in all rooms of current chapter. selectedTime -> 0: total time, 1: first playthrough, 2: practice, 3: golden runs")]
        public static void CctExportRoomTime(int selectedTime = 0, bool usePath = true, bool includeTransitionRooms = false, string separator = "\t") {
            PathInfo path = Mod.CurrentChapterPath;
            ChapterStats stats = Mod.CurrentChapterStats;

            if (stats == null) {
                ConsolePrint("No stats available. Please enter a map first.");
                return;
            }

            TimeCategory timeCategory = GetSelectedTimeCategory(selectedTime);

            List<Tuple<string, string>> data = new List<Tuple<string, string>>();
            if (path == null || !usePath) {
                foreach (var pair in stats.Rooms.OrderBy(p => p.Key)) {
                    RoomStats rStats = pair.Value;
                    long timeSpent = rStats.GetTimeForCategory(timeCategory);
                    data.Add(Tuple.Create(rStats.DebugRoomName, TicksToString(timeSpent)));
                }
            } else {
                foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                    foreach (RoomInfo rInfo in cpInfo.Rooms) {
                        if (!includeTransitionRooms && rInfo.IsNonGameplayRoom) continue;
                        string roomName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType);
                        RoomStats rStats = stats.GetRoom(rInfo);
                        long timeSpent = rStats.GetTimeForCategory(timeCategory);
                        data.Add(Tuple.Create(roomName, TicksToString(timeSpent)));
                    }
                }
            }

            string clipboardString = string.Join("\n", data.Select(t => $"{t.Item1}{separator}{t.Item2}"));
            TextInput.SetClipboardText(clipboardString);

            //string outputString = string.Join("\n", data.Select(t => $"- {t.Item1}: {t.Item2}"));

            int rowCount = 29;
            List<string> outputLines = new List<string>();
            for (int columnStart = 0; columnStart < data.Count; columnStart += rowCount) { 
                int minNameLength = 0;
                int minTimeLength = 0;
                int columnIndexEnd = Math.Min(columnStart + rowCount, data.Count - 1);
                for (int i = columnStart; i <= columnIndexEnd; i++) {
                    if (data[i].Item1.Length > minNameLength) minNameLength = data[i].Item1.Length;
                    if (data[i].Item2.Length > minTimeLength) minTimeLength = data[i].Item2.Length;
                }

                for (int i = columnStart; i <= columnIndexEnd; i++) {
                    string entry = FormatTimeItem(data[i].Item1.PadLeft(minNameLength), data[i].Item2.PadRight(minTimeLength));
                    if (outputLines.Count <= i - columnStart) outputLines.Add(entry);
                    else outputLines[i - columnStart] += "    "+entry;
                }
            }

            string selectedTimeStr = timeCategory == TimeCategory.Total ? "Total" : timeCategory == TimeCategory.FirstPlaythrough ? "First Playthrough" : timeCategory == TimeCategory.Practice ? "Practice" : "Runs";
            string output = string.Join("\n", outputLines);
            ConsolePrint($"Time spent in all rooms ({selectedTimeStr}):\n{output}");
        }
        
        [Command("cct-diff", "Set the difficulty shares of the current (or specified) room. Examples: cct-diff 50, cct-diff 75 EH-8")]
        public static void CctDiff(int difficulty = Int32.MinValue, string roomName = null) {
            PathInfo path = Mod.CurrentChapterPath;
            ChapterStats stats = Mod.CurrentChapterStats;

            if (path == null || stats == null) {
                ConsolePrint("No path available. Please enter a map with a path first.");
                return;
            }

            roomName = EscapeStringParameter(roomName);
            RoomInfo targetRoom = null;
            if (!string.IsNullOrEmpty(roomName)) {
                //Find room by name
                foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                    foreach (RoomInfo rInfo in cpInfo.Rooms) {
                        string rName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore);
                        if (rName != roomName) continue;
                        targetRoom = rInfo;
                        break;
                    }
                }
                if (targetRoom == null) {
                    ConsolePrint($"Didn't find room with name '{roomName}' in this chapter");
                    return;
                }
            } else {
                targetRoom = path.CurrentRoom;
            }
            
            if (targetRoom == null) {
                ConsolePrint("This room is not on the path.");
                return;
            }

            if (difficulty == Int32.MinValue) {
                string calculatedWeight = "";
                if (targetRoom.DifficultyWeight == -1) {
                    var chokeRateData = ChokeRateStat.GetRoomData(path, stats);
                    int roomDifficulty = GetRoomDifficultyBasedOnStats(chokeRateData, targetRoom);
                    calculatedWeight = $" (calculated difficulty: {roomDifficulty})";
                }
                ConsolePrint($"Difficulty of room '{targetRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType)}' is: {targetRoom.DifficultyWeight}{calculatedWeight}");
                return;
            }

            Mod.ChangeRoomDifficultyWeight(difficulty, targetRoom);
            
            ConsolePrint($"Set difficulty of room '{targetRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType)}' to {difficulty}");
        }

        [Command("cct-diff-all", "Set the difficulty shares of ALL rooms. Use without parameter to get a list of all room difficulties.")]
        public static void CctDiffAll(int difficulty = Int32.MinValue) {
            PathInfo path = Mod.CurrentChapterPath;
            ChapterStats stats = Mod.CurrentChapterStats;

            if (path == null || stats == null) {
                ConsolePrint("No path available. Please enter a map with a path first.");
                return;
            }

            CustomNameBehavior customNameBehavior = Mod.ModSettings.LiveDataCustomNameBehavior;
            if (path.GameplayRoomCount > 200) {
                customNameBehavior = CustomNameBehavior.Ignore;
            }

            if (difficulty == Int32.MinValue) {
                var chokeRateData = ChokeRateStat.GetRoomData(path, stats);
                string toPrint = "";
                foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                    toPrint += $"\n{cpInfo.Name} ({cpInfo.Abbreviation}) (Total: {cpInfo.Stats.DifficultyWeight}): ";
                    foreach (RoomInfo rInfo in cpInfo.GameplayRooms) {
                        string roomName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, customNameBehavior);
                        int roomDifficulty = rInfo.DifficultyWeight;
                        string tilde = "";
                        if (roomDifficulty == -1) {
                            roomDifficulty = GetRoomDifficultyBasedOnStats(chokeRateData, rInfo);
                            tilde = "~";
                        }
                        toPrint += $"{roomName}: {tilde}{roomDifficulty} | ";
                    }
                    toPrint = toPrint.Substring(0, toPrint.Length - 3);
                }
                ConsolePrint($"All room difficulties (Total: {path.Stats.DifficultyWeight}):{toPrint}\n" +
                             $"Difficulties with a '~' are -1 and calculated dynamically based on your choke rate stats.");
                return;
            }

            Mod.ChangeAllRoomDifficultyWeights(difficulty);
            ConsolePrint($"Set difficulty of all rooms to {difficulty}");
        }
        
        [Command("cct-diff-auto", "Sets all room difficulties based on your current golden stats.")]
        public static void CctDiffAuto(int baseValue = 10, bool overwriteExistingDifficulties = false, float baseChokePercent = 0.05f) {
            PathInfo path = Mod.CurrentChapterPath;
            ChapterStats stats = Mod.CurrentChapterStats;

            if (path == null || stats == null) {
                ConsolePrint("No stats available. Please enter a map with a path first.");
                return;
            }

            if (baseValue < 1) {
                ConsolePrint($"Base value must be at least 1. Example: cct-diff-auto 10");
                return;
            }
            
            var roomData = ChokeRateStat.GetRoomData(path, stats);

            string toPrint = "";
            foreach (RoomInfo rInfo in path.WalkPath()) {
                string roomName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType);
                if (rInfo.DifficultyWeight != -1 && !overwriteExistingDifficulties) {
                    toPrint += $"{roomName}: {rInfo.DifficultyWeight} (Unchanged)\n";
                } else {
                    rInfo.DifficultyWeight = GetRoomDifficultyBasedOnStats(roomData, rInfo);
                    toPrint += $"{roomName}: {rInfo.DifficultyWeight}\n";
                }
            }

            toPrint = toPrint.Substring(0, toPrint.Length - 1);
            
            Mod.SaveActivePath();
            Mod.SaveChapterStats();
            ConsolePrint($"Auto assigned room difficulties based on your stats:\n{toPrint}");
        }

        public static int GetRoomDifficultyBasedOnStats(Dictionary<RoomInfo, Tuple<int, float, int, float>> chokeRateData, RoomInfo rInfo, int baseValue = 10, float baseChokePercent = 0.05f) {
            int diff = baseValue;
            if (chokeRateData.ContainsKey(rInfo)) {
                var data = chokeRateData[rInfo];
                float successRate = data.Item2;
                if (float.IsNaN(successRate)) {
                    diff = baseValue;
                } else if (successRate == 1) {
                    diff = 0;
                } else {
                    float chokeRate = 1 - successRate;
                    float diffNom = baseChokePercent / chokeRate;
                    diff = (int)(baseValue / diffNom);
                }
            } else {
                diff = baseValue;
            }
            return diff;
        }

        [Command("cct-test", "A test command used in development of CCT. Effect can change at random and is not known. Use at your own risk.")]
        public static void CctTest(int index) {
            Color[] colors = {
                new Color(1f, 0f, 0f),   // red
                new Color(0f, 1f, 0f),   // green
                new Color(0f, 0f, 1f),   // blue

                new Color(1f, 1f, 0f),   // yellow
                new Color(0f, 1f, 1f),   // cyan
                new Color(1f, 0f, 1f),   // magenta

                new Color(1f, 0.5f, 0f), // orange
                new Color(0.5f, 0f, 1f), // purple
                new Color(0.6f, 0.3f, 0.1f), // brown

                new Color(1f, 0.75f, 0.8f), // pink
                new Color(0.5f, 1f, 0.5f),  // light green
                new Color(0.5f, 0.5f, 1f),  // light blue

                new Color(0f, 0f, 0f),   // black
                new Color(0.25f, 0.25f, 0.25f), // dark gray
                new Color(0.5f, 0.5f, 0.5f),    // gray
                new Color(0.75f, 0.75f, 0.75f), // light gray
                new Color(1f, 1f, 1f),   // white

                new Color(0.1f, 0.1f, 0.1f), // near black
                new Color(0.9f, 0.9f, 0.9f), // near white

                new Color(0.2f, 0.8f, 0.6f), // teal-ish
                new Color(0.8f, 0.2f, 0.6f), // odd magenta
                new Color(0.6f, 0.8f, 0.2f), // lime-ish
            };
            
            if (index < 0 || index >= colors.Length) {
                ConsolePrint($"Index out of bounds. Please provide an index between 0 and {colors.Length - 1}.");
                return;
            }
            
            GraphOverlay.testColor = colors[index];
            ConsolePrint($"Set test color to index {index}.");
        }
        
        [Command("cct-fgr", "Transforms a list of chapter UIDs into an FGR path. Call without parameters to get a more in-depth explanation.")]
        public static void CctFgr(string input = null, bool copyStatsFromFirstMap = false) {
            string[] pathUIDs;
            if (string.IsNullOrEmpty(input)) {
                ConsolePrint($"- This command is used to create a full game run (FGR) path. You need to provide a list of chapter UIDs (separated by semi-colons, DONT USE SPACES) that should be included in the FGR path. Example:");
                ConsolePrint("> cct-fgr tom_0_1-space_Normal;tom_0_2-city_Normal;tom_0_3-temple_Normal");
                ConsolePrint("- This will create an FGR path of the 3 maps in the campaign 'Lunar Ruins'");
                ConsolePrint("");
                ConsolePrint("- You can also let cct copy the stats from the first map to the FGR stats by adding the parameter 'true' at the end. Example:");
                ConsolePrint("> cct-fgr tom_0_1-space_Normal;tom_0_2-city_Normal;tom_0_3-temple_Normal true");
                ConsolePrint("");
                ConsolePrint("- YOU NEED TO HAVE RECORDED A PATH FOR EVERY MAP YOU PROVIDE HERE. CCT will take the currently selected path segment for all maps you provided.");
                ConsolePrint("");
                ConsolePrint("- Use the command 'cct-list-uids' to get the UIDs of all maps in your current campaign. In bigger campaigns you might need to enter a map first to get a list of all maps in the current lobby.");
                ConsolePrint(
                    "- You can create an FGR path automatically for all maps in your current campaign by passing 'default' instead of a list of UIDs. (WARNING: This command only sees maps that you have played at least once on this save file! >> Some big campaigns can fail due to weird campaign/lobby structure. <<)");
                return;
            } else if (input == "default") {
                pathUIDs = GetAllChapterUidsInCampaign();
                if (pathUIDs == null) {
                    ConsolePrint("Please enter a map first to get UIDs of all maps in the campaign.");
                    return;
                }
            } else {
                pathUIDs = input.Split(';');
            }
            
            // Give feedback on which UIDs are being used now
            ConsolePrint($"Creating FGR path with the following UIDs: {string.Join(" -> ", pathUIDs)}");
            ConsolePrint($"(If this list seems incomplete, make sure you didn't use any spaces when providing the UIDs!)");
            
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFile("fgr"));
            
            var pathUIDToSegmentList = new Dictionary<string, PathSegmentList>();
            foreach (string uid in pathUIDs) {
                Mod.Log($"Requested path UID for FGR: {uid}");
                pathUIDToSegmentList.Add(uid, ConsistencyTrackerModule.Instance.GetPathSegmentList(ConsistencyTrackerModule.PathsFolder, uid));
            }
            
            // Check if all paths were found
            bool allFound = true;
            foreach (var pair in pathUIDToSegmentList) {
                if (pair.Value == null) {
                    ConsolePrint($"Could not find path for UID: {pair.Key}");
                    Mod.Log($"Could not find path for UID: {pair.Key}");
                    allFound = false;
                }
            }
            if (!allFound) {
                ConsolePrint("Aborting FGR creation due to missing paths.");
                Mod.Log("Aborting FGR creation due to missing paths.");
                return;
            }
            
            // Create FGR path: Loop through all paths, rename all rooms to FGR format (prefix the sid to the debug room name).
            // Then, concatenate all checkpoints from all paths into a new PathInfo.
            // PathSegmentList contains many segments, but we only care about the currently selected PathInfo in each list.

            PathSegmentList fgrList = pathUIDToSegmentList[pathUIDs[0]];
            PathInfo fgr = fgrList.CurrentPath; // Write all to the first path's PathInfo
            Mod.Log("Creating FGR path...");

            foreach (string uid in pathUIDs) {
                PathSegmentList list = pathUIDToSegmentList[uid];
                PathInfo path = list.CurrentPath;
                Mod.Log($"Processing path for UID {uid}...");

                try {
                    path.MakeFgrChanges();
                } catch (InvalidOperationException) {
                    Mod.Log($"Stopping FGR creation due to unset ChapterSID in path for UID {uid}.");
                    ConsolePrint($"Path for UID '{uid}' does not have a ChapterSID set. Please enter the map once for CCT to fix this automatically.");
                    return;
                }
                
                // Append all checkpoints from this path to the FGR path
                if (path != fgr) {
                    foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                        fgr.Checkpoints.Add(cpInfo);
                    }
                }
            }
            
            // Output result to folder "fgr", file name "fgr_[number].json", where number is the next available number starting at 1.
            int fileNumber = 1;
            string pathPath = null;
            while (pathPath == null) {
                string checkPath = ConsistencyTrackerModule.GetPathToFile("fgr", $"fgr_{fileNumber}_path.json");
                if (!File.Exists(checkPath)) {
                    pathPath = checkPath;
                } else {
                    fileNumber++;
                }
            }
            
            // Remove all other segments from the fgrList
            int selectedIndex = fgrList.SelectedIndex;
            fgrList.SelectedIndex = 0;
            fgrList.Segments.Clear();
            fgrList.Segments.Add(new PathSegment() {
                Name = "FGR",
                Path = fgr
            });
            File.WriteAllText(pathPath, JsonConvert.SerializeObject(fgrList, Formatting.Indented));
            
            // Create stats file from first chapter's stats as a base
            string statsPath = pathPath.Replace("_path.json", "_stats.json");
            ChapterStatsList statsList;

            if (copyStatsFromFirstMap) {
                statsList = ConsistencyTrackerModule.Instance.GetChapterStatsList(ConsistencyTrackerModule.StatsFolder, pathUIDs[0]);
                ChapterStats stats = statsList.GetStats(selectedIndex);
                stats.MakeFgrChanges(pathUIDs[0]);
                statsList.SegmentStats.Clear();
                statsList.SegmentStats.Add(stats);
            } else {
                statsList = new ChapterStatsList();
            }
            File.WriteAllText(statsPath, JsonConvert.SerializeObject(statsList, Formatting.Indented));
                
            ConsolePrint("Total rooms on new path: " + fgr.GameplayRoomCount);
            ConsolePrint($"Creation of FGR path complete! Saved to: {pathPath}.");
            ConsolePrint($"To use this path, go to Mod Settings -> Path Management -> Selected FGR -> FGR {fileNumber}");
        }

        [Command("cct-list-uids", "List all chapter UIDs of the current campaign (that have been played yet). Copies the list to clipboard.")]
        public static void CctListUids() {
            if (SaveData.Instance.CurrentSession == null) {
                ConsolePrint("Please enter a map first to list UIDs of all maps in the campaign.");
                return;
            }

            string[] uids = GetAllChapterUidsInCampaign();
            if (uids == null || uids.Length == 0) {
                ConsolePrint("No chapters played yet in this campaign.");
                return;
            }
            ConsolePrint($"Chapter UIDs played in current campaign:");
            ConsolePrint($"- {string.Join("\n- ", uids)}");
            ConsolePrint($"The list has also been copied to your clipboard.");
            TextInput.SetClipboardText(string.Join("\n", uids));
        }

        private static string[] GetAllChapterUidsInCampaign() {
            if (SaveData.Instance.CurrentSession == null) {
                return null;
            }
            
            var toReturn = new List<string>();
            AreaKey areaKey = SaveData.Instance.CurrentSession.Area;
            LevelSetStats lss = SaveData.Instance.GetLevelSetStatsFor(areaKey.LevelSet);
            foreach (AreaStats stats in lss.AreasIncludingCeleste) {
                string sid = stats.SID;
                for (int i = 0; i < stats.Modes.Length; i++) {
                    AreaModeStats ams = stats.Modes[i];
                    if (ams.TimePlayed == 0) continue;
                    
                    AreaMode mode = GetAreaMode(i);
                    string uid = ChapterMetaInfo.GetChapterUIDForPath(sid, mode);
                    toReturn.Add(uid);
                }
            }
            return toReturn.ToArray();
        }
        
        private static AreaMode GetAreaMode(int index) {
            if (index == 0) return AreaMode.Normal;
            if (index == 1) return AreaMode.BSide;
            if (index == 2) return AreaMode.CSide;
            return AreaMode.Normal;
        }

        #endregion

        #region Utility
        private static TimeCategory GetSelectedTimeCategory(int selectedTime) {
            if (selectedTime == 1) return TimeCategory.FirstPlaythrough;
            if (selectedTime == 2) return TimeCategory.Practice;
            if (selectedTime == 3) return TimeCategory.Runs;
            return TimeCategory.Total;
        }
        private static string FormatTimeItem(string roomName, string timeString) {
            return $"{roomName}: {timeString}";
        }

        public static void ConsolePrint(string message) {
            Engine.Commands.Log(message);
        }
        public static string EscapeStringParameter(string parameter) {
            if (string.IsNullOrEmpty(parameter)) return parameter;
            parameter = parameter.Replace("\\_", "{us875813}");
            parameter = parameter.Replace("_", " ");
            parameter = parameter.Replace("{us875813}", "_");
            return parameter;
        }
        public static string TicksToString(long ticks) {
            TimeSpan timeSpan = TimeSpan.FromTicks(ticks);
            string text = ((!(timeSpan.TotalHours >= 1.0)) ? timeSpan.ToString("mm\\:ss") : ((int)timeSpan.TotalHours + ":" + timeSpan.ToString("mm\\:ss")));
            return text;
        }
        
        /// <summary>
        /// Parses a UID into its SID and area mode components.
        /// </summary>
        /// <param name="uid">The UID to parse.</param>
        /// <returns>A tuple containing the SID and area mode.</returns>
        /// <exception cref="ArgumentException">When the UID is null or empty.</exception>
        /// <exception cref="FormatException">When the UID format is invalid.</exception>
        public static Tuple<string, string> ParseUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("UID cannot be null or empty.", nameof(uid));

            int lastSlash = uid.LastIndexOf('/');
            if (lastSlash < 0)
                throw new FormatException("UID must contain at least one forward slash.");

            // Everything before last "/" is the SID
            string sid = uid.Substring(0, lastSlash);

            // Everything after is area mode
            string areaMode = uid.Substring(lastSlash + 1);

            return new Tuple<string, string>(sid, areaMode);
        }

        public static void LoadLevel(string sid, AreaMode mode = AreaMode.Normal, string level = null, bool endRun = true) {
            Mod.Log($"Loading level: {sid} Mode: {mode} Level: {level}");
            AreaData areaData = AreaData.Get(sid);
            if (areaData == null) {
                ConsolePrint("Could not find area data for empty SID.");
                return;
            }
            AreaKey area = new AreaKey(areaData.ID, mode);
            SaveData.Instance.LastArea_Safe = area;
            Session session = new Session(area);
            if (level != null && session.MapData.Get(level) != null)
            {
                if (AreaData.GetCheckpoint(area, level) != null)
                    session = new Session(area, level)
                    {
                        StartCheckpoint = null
                    };
                else
                    session.Level = level;
                bool flag = level == session.MapData.StartLevel().Name;
                session.FirstLevel = flag;
                session.StartedFromBeginning = flag;
            }

            if (endRun) {
                Mod.EndRun();
            }
            Engine.Scene = new LevelLoader(session);
        }

        public static void LoadRoom(RoomInfo rInfo, bool endRun = true) {
            if (Equals(Mod.CurrentChapterPath.CurrentRoom, rInfo)) {
                Mod.Log("Already in the requested room.");
                return;
            }

            string uid = rInfo.Checkpoint.Chapter.ChapterUID;
            string level = rInfo.DebugRoomName;
            if (rInfo.IsFGR) {
                uid = rInfo.UID;
                level = rInfo.ActualDebugRoomName;
            }

            var parsed = ParseUid(uid);
            bool success = Enum.TryParse(parsed.Item2, false, out AreaMode mode);
            if (!success) {
                Mod.Log($"Failed to parse area mode from '{parsed.Item2}'");
                return;
            }
                            
            LoadLevel(parsed.Item1, mode, level, endRun:endRun);
        }
        #endregion
    }
}
