using Celeste.Mod.ConsistencyTracker.Models;
using MonoMod.ModInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop {
    [ModExportName("ConsistencyTracker")]
    public static class ConsistencyTrackerAPI {
        /// <summary>
        /// Adds a golden death to the specified room
        /// </summary>
        /// <param name="roomName">The room to add a golden death to. null adds to the current room</param>
        /// <returns>True if success, False if there was some issue</returns>
        public static bool AddGoldenDeath(string roomName) {
            ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
            if (mod.CurrentChapterStats == null) return false; //Just started the game and not entered a map yet
            
            mod.Log($"Invoked API.AddGoldenDeath");

            RoomStats selectedRoom;
            if (!string.IsNullOrEmpty(roomName)) {
                if (mod.IsInFgrMode) roomName = mod.ResolveRoomNameInActiveChapter(roomName);
                selectedRoom = mod.CurrentChapterStats.GetRoom(roomName);

                if (selectedRoom == null) {
                    mod.Log($"Didn't find room with debug name: '{roomName}'");
                    return false;
                }
            } else {
                selectedRoom = mod.CurrentChapterStats.CurrentRoom;

                if (selectedRoom == null) {
                    mod.Log($"No roomName given and player does not have a 'CurrentRoom'");
                    return false;
                }
            }

            mod.CurrentChapterStats.AddGoldenBerryDeath(selectedRoom.DebugRoomName);
            mod.EndRun(true);
            mod.SaveChapterStats(); //Call events to notify pace ping, physics logger, external listeners...

            mod.Log($"Added golden death to '{roomName}'");
            return true;
        }

        /// <summary>
        /// Adds a golden death to the specified room without triggering CCT Events for the run ending
        /// </summary>
        /// <param name="roomName">The room to add a golden death to. null adds to the current room</param>
        /// <returns>True if success, False if there was some issue</returns>
        public static bool AddGoldenDeathNoEvent(string roomName) {
            ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
            if (mod.CurrentChapterStats == null) return false; //Just started the game and not entered a map yet

            mod.Log($"Invoked API.AddGoldenDeathNoEvent");

            RoomStats selectedRoom;
            if (!string.IsNullOrEmpty(roomName)) {
                selectedRoom = mod.CurrentChapterStats.GetRoom(roomName);

                if (selectedRoom == null) {
                    mod.Log($"Didn't find room with debug name: '{roomName}'");
                    return false;
                }
            } else {
                selectedRoom = mod.CurrentChapterStats.CurrentRoom;

                if (selectedRoom == null) {
                    mod.Log($"No roomName given and player does not have a 'CurrentRoom'");
                    return false;
                }
            }

            mod.CurrentChapterStats.AddGoldenBerryDeath(selectedRoom.DebugRoomName);
            mod.SaveChapterStats();
            //Don't invoke event

            mod.Log($"Added golden death to '{roomName}'");
            return true;
        }

        /// <summary>
        /// Gets the debug room names of all gameplay rooms on the current path, grouped by checkpoint
        /// </summary>
        /// <returns>One list of debug room names per checkpoint, or null if there is no path for the current map</returns>
        public static List<List<string>> GetSimplifiedPath() {
            ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
            PathInfo path = mod.CurrentChapterPath;
            if (path == null) return null;

            return path.Checkpoints
                .Select(cpInfo => cpInfo.GameplayRooms.Select(rInfo => rInfo.DebugRoomName).ToList())
                .ToList();
        }

        /// <summary>
        /// Gets the names of all checkpoints on the current path, in the same order as GetSimplifiedPath
        /// </summary>
        /// <param name="abbreviated">If true, returns the checkpoint abbreviations instead of the full names</param>
        /// <returns>The list of checkpoint names, or null if there is no path for the current map</returns>
        public static List<string> GetCheckpointNames(bool abbreviated = false) {
            ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
            PathInfo path = mod.CurrentChapterPath;
            if (path == null) return null;

            return path.Checkpoints.Select(cpInfo => abbreviated ? cpInfo.Abbreviation : cpInfo.Name).ToList();
        }

        /// <summary>
        /// Gets the saved attempts of a room in the current chapter stats
        /// </summary>
        /// <param name="roomName">The debug room name of the room</param>
        /// <returns>The list of previous attempts (true = success, false = death), oldest first. null if the room has no stats</returns>
        public static List<bool> GetRoomAttempts(string roomName) {
            ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
            if (mod.CurrentChapterStats == null || string.IsNullOrEmpty(roomName)) return null;

            if (mod.IsInFgrMode && !roomName.Contains(":")) roomName = mod.ResolveRoomNameInActiveChapter(roomName);

            //Don't use ChapterStats.GetRoom, as that would create the room if it doesn't exist
            if (!mod.CurrentChapterStats.Rooms.TryGetValue(roomName, out RoomStats rStats)) return null;
            return new List<bool>(rStats.PreviousAttempts);
        }
    }
}
