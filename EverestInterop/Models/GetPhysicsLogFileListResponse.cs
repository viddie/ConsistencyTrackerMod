using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models {
    public class GetPhysicsLogFileListResponse : Response {

        [JsonProperty("isInRecording")]
        public bool IsInRecording { get; set; }

        [JsonProperty("count")]
        public int Count => PhysicsLogFiles.Count;

        [JsonProperty("physicsLogFiles")]
        public List<PhysicsLogFile> PhysicsLogFiles { get; set; } = new List<PhysicsLogFile>();

        public class PhysicsLogFile {
            //Properties: ChapterName, SideName, RecordingStarted, FrameCount

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
