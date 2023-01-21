using Celeste.Mod.ConsistencyTracker.EverestInterop;
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
            string deathTrackingPaused = DebugRcPage.FormatFieldJson("deathTrackingPaused", DeathTrackingPaused);
            string recordingPath = DebugRcPage.FormatFieldJson("recordingPath", RecordingPath);
            string overlayVersion = DebugRcPage.FormatFieldJson("overlayVersion", OverlayVersion);
            string playerIsHoldingGolden = DebugRcPage.FormatFieldJson("playerIsHoldingGolden", PlayerIsHoldingGolden);
            string chapterHasPath = DebugRcPage.FormatFieldJson("chapterHasPath", ChapterHasPath);

            return DebugRcPage.FormatJson(deathTrackingPaused, recordingPath, overlayVersion, playerIsHoldingGolden, chapterHasPath);
        }

        private string JsonFormatBool(bool b) {
            return b ? "true" : "false";
        }
    }
}
