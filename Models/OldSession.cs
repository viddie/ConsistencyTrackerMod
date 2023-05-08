using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class OldSession {
        [JsonProperty("sessionStarted")]
        public DateTime SessionStarted { get; set; }

        [JsonProperty("totalGoldenDeaths")]
        public int TotalGoldenDeaths { get; set; }

        [JsonProperty("totalGoldenDeathsSession")]
        public int TotalGoldenDeathsSession { get; set; }

        [JsonProperty("totalSuccessRate")]
        public float TotalSuccessRate { get; set; }

        [JsonProperty("lastGoldenRuns")]
        public List<string> LastGoldenRuns { get; set; } = new List<string>();

        [JsonProperty("pbRoomName")]
        public string PBRoomName { get; set; }
        
        [JsonProperty("sessionPbRoomName")]
        public string SessionPBRoomName { get; set; }
        [JsonProperty("sessionPbRoomDeaths")]
        public int SessionPBRoomDeaths { get; set; }

        [JsonProperty("averageRunDistance")]
        public float AverageRunDistance { get; set; }
        
        [JsonProperty("averageRunDistanceSession")]
        public float AverageRunDistanceSession { get; set; }

        /// <summary>
        /// Checks if nothing has happened in a session compared to another session.
        /// </summary>
        /// <returns>True if nothing happened in a session, False if there was some change in data</returns>
        public static bool IsSessionEmpty(OldSession session, OldSession compareTo) {
            bool isEmpty = true;
            isEmpty &= session.TotalGoldenDeaths == compareTo.TotalGoldenDeaths;
            isEmpty &= session.TotalGoldenDeathsSession == 0;
            isEmpty &= session.TotalSuccessRate == compareTo.TotalSuccessRate;
            isEmpty &= session.LastGoldenRuns.Count == 0;
            isEmpty &= session.AverageRunDistance == compareTo.AverageRunDistance;
            isEmpty &= session.AverageRunDistanceSession == 0;
            return isEmpty;
        }
    }
}
