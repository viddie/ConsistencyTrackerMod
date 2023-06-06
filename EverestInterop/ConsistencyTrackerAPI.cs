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
        /// <returns>True if success, False if there was some issue</returns>
        public static bool AddGoldenDeath(string roomName) {
            ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
            if (mod.CurrentChapterStats == null) return false; //Just started the game and not entered a map yet
            
            RoomStats rStats = mod.CurrentChapterStats.GetRoom(roomName);
            if (rStats == null) return false;

            mod.CurrentChapterStats.AddGoldenBerryDeath(roomName);
            mod.SaveChapterStats();
            mod.Log($"Added golden death to '{roomName}'");

            return true;
        }
    }
}
