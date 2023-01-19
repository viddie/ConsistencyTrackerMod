using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class ModState {

        public bool PlayerIsHoldingGolden { get; set; } = false;
        public bool ChapterCompleted { get; set; } = false;
        public bool GoldenDone { get; set; } = false;
        public bool DeathTrackingPaused { get; set; } = false;
        public bool RecordingPath { get; set; } = false;
        public string OverlayVersion { get; set; } = null;
        public string ModVersion { get; set; } = null;
        public bool ChapterHasPath { get; set; } = false;


        //{ModSettings.PauseDeathTracking};{ModSettings.RecordPath};{OverlayVersion};{_PlayerIsHoldingGolden}
        public override string ToString() {
            return $"{DeathTrackingPaused};{RecordingPath};{OverlayVersion};{PlayerIsHoldingGolden}";
        }

        public string ToJson() {
            return $"{{ \"deathTrackingPaused\":{JsonFormatBool(DeathTrackingPaused)}" +
                $", \"recordingPath\":{JsonFormatBool(RecordingPath)}" +
                $", \"overlayVersion\":\"{OverlayVersion}\"" +
                $", \"playerIsHoldingGolden\":{JsonFormatBool(PlayerIsHoldingGolden)}" +
                $", \"chapterHasPath\":{JsonFormatBool(ChapterHasPath)} }}";
        }

        private string JsonFormatBool(bool b) {
            return b ? "true" : "false";
        }
    }
}
