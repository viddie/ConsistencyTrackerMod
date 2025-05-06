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
                Mod.SavePathToFile();
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
        
        [Command("cct-diff", "Set the difficulty shares of the current room.")]
        public static void CctDiff(int difficulty = Int32.MinValue) {
            PathInfo path = Mod.CurrentChapterPath;

            if (path == null) {
                ConsolePrint("No path available. Please enter a map with a path first.");
                return;
            } else if (path.CurrentRoom == null) {
                ConsolePrint("This room is not on the paht.");
                return;
            }

            if (difficulty == Int32.MinValue) {
                ConsolePrint($"Difficulty of room '{path.CurrentRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType)}' is: {path.CurrentRoom.DifficultyWeight}");
                return;
            }

            path.CurrentRoom.DifficultyWeight = difficulty;
            Mod.SavePathToFile();
            Mod.SaveChapterStats();
            ConsolePrint($"Set difficulty of room '{path.CurrentRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType)}' to {difficulty}");
        }

        [Command("cct-diff-all", "Set the difficulty shares of ALL rooms.")]
        public static void CctDiffAll(int difficulty = Int32.MinValue) {
            PathInfo path = Mod.CurrentChapterPath;

            if (path == null) {
                ConsolePrint("No path available. Please enter a map with a path first.");
                return;
            }

            if (difficulty == Int32.MinValue) {
                ConsolePrint($"Add a difficulty parameter to overwrite the difficulty of ALL rooms. Example: cct-diff-all 10");
                return;
            }

            foreach (RoomInfo rInfo in path.WalkPath()) {
                rInfo.DifficultyWeight = difficulty;
            }
            
            Mod.SavePathToFile();
            Mod.SaveChapterStats();
            ConsolePrint($"Set difficulty of room '{path.CurrentRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType)}' to {difficulty}");
        }

        [Command("cct-test", "A test command used in development of CCT. Effect can change at random and is not known. Use at your own risk.")]
        public static void CctTest(float correctionFactor = 1f) {
            PathInfo path = Mod.CurrentChapterPath;
            ChapterStats stats = Mod.CurrentChapterStats;

            if (stats == null || path == null) {
                ConsolePrint("No stats available. Please enter a map with a path first.");
                return;
            }
            
            ConsolePrint($"Predicted time to clear each room in a golden run:");

            Dictionary<RoomInfo, Tuple<int, int, int, int>> roomData = ChokeRateStat.GetRoomDataInts(path, stats);
            List<RoomInfo> rooms = path.WalkPath().ToList();

            long totalPredictedRunLength = 0;

            for (int i = 0; i < rooms.Count; i++) {
                RoomStats rStats = stats.GetRoom(rooms[i]);
                long timeSpent = rStats.TimeSpentInRoomInRuns;
                int totalAttempts = roomData[rooms[i]].Item1;
                int totalSuccesses = roomData[rooms[i]].Item2;

                long predictedTimeToClearRoomInRun = (long)((double)timeSpent / totalAttempts / ((double)totalSuccesses / totalAttempts) * correctionFactor);
                totalPredictedRunLength += predictedTimeToClearRoomInRun;
                ConsolePrint($"{rooms[i].GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType)}: {TicksToString(predictedTimeToClearRoomInRun)}");
            }

            ConsolePrint($"\nTotal predicted run length: {TicksToString(totalPredictedRunLength)}");
        }

        #endregion

        #region Utility
        private static TimeCategory GetSelectedTimeCategory(int selectedTime) {
            if (selectedTime == 1) {
                return TimeCategory.FirstPlaythrough;
            } else if (selectedTime == 2) {
                return TimeCategory.Practice;
            } else if (selectedTime == 3) {
                return TimeCategory.Runs;
            } else {
                return TimeCategory.Total;
            }
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
        #endregion
    }
}
