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

        [JsonProperty("averageRunDistance")]
        public float AverageRunDistance { get; set; }
        
        [JsonProperty("averageRunDistanceSession")]
        public float AverageRunDistanceSession { get; set; }
    }
}
