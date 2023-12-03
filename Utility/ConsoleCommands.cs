using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Monocle;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public static class ConsoleCommands {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        #region Commands

        [Command("cct-name", "get or set the custom room name of any room. use \"_\" for spaces in names. set the name to \"-\" to remove the name.")]
        public static void CctName(string roomName, string newName = null) {
            if (Mod.CurrentChapterPath == null) {
                Engine.Commands.Log("No path for current map found. Please record a path first!");
                return;
            }
            if (string.IsNullOrEmpty(roomName)) {
                RoomInfo exampleRoom = Mod.CurrentChapterPath.CurrentRoom ?? Mod.CurrentChapterPath.Checkpoints[0].Rooms[Mod.CurrentChapterPath.Checkpoints[0].Rooms.Count - 1];
                string exampleName = exampleRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore);
                Engine.Commands.Log($"Please provide a room name. Example: cct-name "+exampleName);
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
                Engine.Commands.Log($"Didn't find room with name '{roomName}' in this chapter");
                return;
            }
            CheckpointInfo foundCheckpoint = foundRoom.Checkpoint;
            string foundRoomName = foundRoom.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore);
            string customName = foundRoom.CustomRoomName;
            string cpString = $"Checkpoint {foundCheckpoint.CPNumberInChapter} ({foundCheckpoint.Name})";

            if (string.IsNullOrEmpty(newName)) {
                if (customName == null) {
                    Engine.Commands.Log($"{cpString} - Room '{foundRoomName}' doesn't have a custom room name");
                } else {
                    Engine.Commands.Log($"{cpString} - Room '{foundRoomName}' has custom name '{customName}'");
                }
            } else {
                newName = EscapeStringParameter(newName);
                if (newName == "-") {
                    foundRoom.CustomRoomName = null;
                    Engine.Commands.Log($"{cpString} - Room '{foundRoomName}' custom name removed");
                } else {
                    foundRoom.CustomRoomName = newName;
                    Engine.Commands.Log($"{cpString} - Room '{foundRoomName}' custom name set to '{newName}'");
                }
                Mod.SavePathToFile();
                Mod.SaveChapterStats();
            }
        }

        [Command("cct-list-names", "lists all room names of the current chapter.")]
        public static void CctListNames() {
            if (Mod.CurrentChapterPath == null) {
                Engine.Commands.Log("No path for current map found. Please record a path first!");
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
            Engine.Commands.Log($"All rooms in '{path.ChapterDisplayName}' [{path.RoomCount} Rooms]:\n{joined}");
        }

        #endregion

        #region Utility

        public static string EscapeStringParameter(string parameter) {
            if (string.IsNullOrEmpty(parameter)) return parameter;
            parameter = parameter.Replace("\\_", "{us875813}");
            parameter = parameter.Replace("_", " ");
            parameter = parameter.Replace("{us875813}", "_");
            return parameter;
        }

        #endregion
    }
}
