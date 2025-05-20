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
            Events.Events.InvokeGoldenDeath();
            mod.UpdatePlayerHoldingGolden();
            mod.SaveChapterStats(); //Call events to notify pace ping, physics logger, external listeners...

            mod.Log($"Added golden death to '{roomName}'");
            return true;
        }

        /// <summary>
        /// Adds a golden death to the specified room without triggering CCT Event 'OnGoldenDeath'
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
            mod.UpdatePlayerHoldingGolden();
            mod.SaveChapterStats();
            //Don't invoke event

            mod.Log($"Added golden death to '{roomName}'");
            return true;
        }
    }
}
