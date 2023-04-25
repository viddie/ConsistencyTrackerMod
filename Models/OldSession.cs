using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class OldSession {
        public DateTime SessionStarted { get; set; }
        public int TotalGoldenDeaths { get; set; }
        public int TotalGoldenDeathsSession { get; set; }
        public float TotalSuccessRate { get; set; }
        public List<string> LastGoldenRuns { get; set; } = new List<string>();
    }
}
