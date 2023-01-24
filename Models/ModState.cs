using Celeste.Mod.ConsistencyTracker.EverestInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class ModState {

        [JsonProperty("playerIsHoldingGolden")]
        public bool PlayerIsHoldingGolden { get; set; } = false;

        [JsonProperty("chapterCompleted")]
        public bool ChapterCompleted { get; set; } = false;

        [JsonProperty("goldenDone")]
        public bool GoldenDone { get; set; } = false;

        [JsonProperty("deathTrackingPaused")]
        public bool DeathTrackingPaused { get; set; } = false;

        [JsonProperty("recordingPath")]
        public bool RecordingPath { get; set; } = false;

        [JsonProperty("overlayVersion")]
        public string OverlayVersion { get; set; } = null;

        [JsonProperty("modVersion")]
        public string ModVersion { get; set; } = null;

        [JsonProperty("chapterHasPath")]
        public bool ChapterHasPath { get; set; } = false;


        //{ModSettings.PauseDeathTracking};{ModSettings.RecordPath};{OverlayVersion};{_PlayerIsHoldingGolden}
        public override string ToString() {
            return $"{DeathTrackingPaused};{RecordingPath};{OverlayVersion};{PlayerIsHoldingGolden}";
        }

        private string JsonFormatBool(bool b) {
            return b ? "true" : "false";
        }
    }
}
