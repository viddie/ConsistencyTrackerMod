using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models {
    public class InfoResponse : Response {
        public string message { get; set; }
        public string modVersion { get; set; }
        public List<StatFormat> formatsLoaded { get; set; }
        public bool hasPath { get; set; }
        public bool hasStats { get; set; }
    }
}
