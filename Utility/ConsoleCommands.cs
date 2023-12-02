using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Monocle;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public static class ConsoleCommands {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        [Command("cct-name", "get or set the custom room name of any room. use \"_\" for spaces in names. set the name to \"-\" to remove the name.")]
        public static void CctName(string roomName, string newName = null) {
            if (string.IsNullOrEmpty(roomName)) {
                Engine.Commands.Log($"Please provide a room name. Example: cct-name EH-6");
                return;
            }

            roomName = EscapeStringParameter(roomName);

            PathInfo path = Mod.CurrentChapterPath;
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    string rName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore);
                    string customName = rInfo.CustomRoomName ?? "-";
                    if (rName != roomName) continue;

                    if (string.IsNullOrEmpty(newName)) {
                        Engine.Commands.Log($"Checkpoint {cpInfo.CPNumberInChapter} ({cpInfo.Name}) - Room '{rName}' has custom name '{customName}'");
                    } else {
                        newName = EscapeStringParameter(newName);
                        if (newName == "-") {
                            rInfo.CustomRoomName = null;
                            Engine.Commands.Log($"Checkpoint {cpInfo.CPNumberInChapter} ({cpInfo.Name}) - Room '{rName}' custom name removed");
                        } else {
                            rInfo.CustomRoomName = newName;
                            Engine.Commands.Log($"Checkpoint {cpInfo.CPNumberInChapter} ({cpInfo.Name}) - Room '{rName}' custom name set to '{newName}'");
                        }
                        Mod.SavePathToFile();
                        Mod.SaveChapterStats();
                    }
                }
            }
        }


        public static string EscapeStringParameter(string parameter) {
            if (string.IsNullOrEmpty(parameter)) return parameter;
            parameter = parameter.Replace("\\_", "{us875813}");
            parameter = parameter.Replace("_", " ");
            parameter = parameter.Replace("{us875813}", "_");
            return parameter;
        }
    }
}
