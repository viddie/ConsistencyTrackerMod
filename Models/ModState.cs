using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class ModState {

        public bool PlayerIsHoldingGolden { get; set; } = false;
        public bool DeathTrackingPaused { get; set; } = false;
        public bool RecordingPath { get; set; } = false;
        public string OverlayVersion { get; set; } = null;
        public string ModVersion { get; set; } = null;
        

        //{ModSettings.PauseDeathTracking};{ModSettings.RecordPath};{OverlayVersion};{_PlayerIsHoldingGolden}
        public override string ToString() {
            return $"{DeathTrackingPaused};{RecordingPath};{OverlayVersion};{PlayerIsHoldingGolden}";
        }

    }
}
