using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog {
    public class PhysicsRecordingsState {

        [JsonProperty("savedRecordings")]
        public List<PhysicsRecording> SavedRecordings { get; set; } = new List<PhysicsRecording>();

        [JsonProperty("idCounter")]
        public int IDCounter { get; set; } = 0;

        public class PhysicsRecording {
            [JsonProperty("id")]
            public int ID { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("chapterName")]
            public string ChapterName { get; set; }

            [JsonProperty("sideName")]
            public string SideName { get; set; }

            [JsonProperty("recordingStarted")]
            public DateTime RecordingStarted { get; set; }

            [JsonProperty("frameCount")]
            public int FrameCount { get; set; }
        }
    }
}
