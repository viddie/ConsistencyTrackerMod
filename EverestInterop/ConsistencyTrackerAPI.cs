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
        /// <param name="roomName">The room to add a golden death to</param>
        /// <param name="invokeEvent">Whether to invoke the usual CCT event for a golden death. If true, will ignore roomName parameter and add the golden death to the current room.</param>
        /// <returns>True if success, False if there was some issue</returns>
        public static bool AddGoldenDeath(string roomName, bool invokeEvent = true) {
            ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
            if (mod.CurrentChapterStats == null) return false; //Just started the game and not entered a map yet
            
            RoomStats rStats = mod.CurrentChapterStats.GetRoom(roomName);
            RoomStats currentRoom = mod.CurrentChapterStats.CurrentRoom;
            if (rStats == null && currentRoom == null) return false;
            
            if (invokeEvent && currentRoom != null) {
                //Let the event handle all the data changing. WORKS ONLY FOR THE CURRENT ROOM!!
                Events.Events.InvokeGoldenDeath();
            } else {
                //Don't invoke events. Instead manually change the stats
                mod.CurrentChapterStats.AddGoldenBerryDeath(roomName);
                mod.SaveChapterStats();
            }

            mod.Log($"Added golden death to '{roomName}'");

            return true;
        }
    }
}
