using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses
{
    public class GetPhysicsLogFileListResponse : Response
    {

        [JsonProperty("isRecording")]
        public bool IsRecording { get; set; }

        [JsonProperty("countRecent")]
        public int CountRecent => RecentPhysicsLogFiles.Count;

        [JsonProperty("countSaved")]
        public int CountSaved => SavedPhysicsRecordings.Count;

        [JsonProperty("recentPhysicsLogFiles")]
        public List<PhysicsLogFile> RecentPhysicsLogFiles { get; set; } = new List<PhysicsLogFile>();

        [JsonProperty("savedPhysicsRecordings")]
        public List<PhysicsLogFile> SavedPhysicsRecordings { get; set; } = new List<PhysicsLogFile>();

        public class PhysicsLogFile
        {
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
