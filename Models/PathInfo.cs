using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.EverestInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.ConsistencyTracker.Models {

    [Serializable]
    public class PathInfo {

        [JsonProperty("campaignName")]
        public string CampaignName { get; set; }

        [JsonProperty("chapterName")]
        public string ChapterName { get; set; }

        [JsonProperty("chapterSID")]
        public string ChapterSID { get; set; }

        [JsonProperty("sideName")]
        public string SideName { get; set; }

        [JsonProperty("checkpoints")]
        public List<CheckpointInfo> Checkpoints { get; set; } = new List<CheckpointInfo>();

        [JsonProperty("roomCount")]
        public int RoomCount {
            get {
                return Checkpoints.Sum((cpInfo) => cpInfo.Rooms.Count);
            }
        }

        [JsonProperty("ignoredRooms")]
        public List<string> IgnoredRooms { get; set; } = new List<string>();

        [JsonIgnore]
        public AggregateStats Stats { get; set; } = null;
        
        [JsonIgnore]
        public RoomInfo CurrentRoom { get; set; } = null;
        
        [JsonIgnore]
        public RoomInfo SpeedrunToolSaveStateRoom { get; set; } = null;

        [JsonIgnore]
        public string ParseError { get; set; }

        public static PathInfo GetTestPathInfo() {
            return new PathInfo() {
                Checkpoints = new List<CheckpointInfo>() {
                    new CheckpointInfo(){ Name="Start", Abbreviation="0M" },
                    new CheckpointInfo(){ Name="500 M", Abbreviation="500M" },
                },
            };
        }

        public RoomInfo FindRoom(RoomStats roomStats) {
            return FindRoom(roomStats.DebugRoomName);
        }
        public RoomInfo FindRoom(string roomName) {
            foreach (CheckpointInfo cpInfo in Checkpoints) {
                RoomInfo rInfo = cpInfo.Rooms.Find((r) => r.DebugRoomName == roomName);
                if (rInfo != null) return rInfo;
            }

            return null;
        }

        public override string ToString() {
            List<string> lines = new List<string>();

            foreach (CheckpointInfo cpInfo in Checkpoints) {
                lines.Add(cpInfo.ToString());
            }

            return string.Join("\n", lines);
        }
        public static PathInfo ParseString(string content) {
            ConsistencyTrackerModule.Instance.Log($"Parsing path info string");
            List<string> lines = content.Trim().Split(new string[] { "\n" }, StringSplitOptions.None).ToList();

            PathInfo pathInfo = new PathInfo();

            foreach (string line in lines) {
                ConsistencyTrackerModule.Instance.Log($"\tParsing line '{line}'");
                pathInfo.Checkpoints.Add(CheckpointInfo.ParseString(line));
            }

            return pathInfo;
        }

        public void SetCheckpointRefs() {
            foreach (CheckpointInfo cpInfo in Checkpoints) {
                cpInfo.SetCheckpointRefs();
            }
        }
    }

    [Serializable]
    public class CheckpointInfo {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }

        [JsonProperty("roomCount")]
        public int RoomCount {
            get => Rooms.Count;
            private set {
            }
        }

        [JsonProperty("rooms")]
        public List<RoomInfo> Rooms { get; set; } = new List<RoomInfo>();

        [JsonIgnore]
        public AggregateStats Stats { get; set; } = null;
        [JsonIgnore]
        public int CPNumberInChapter { get; set; } = -1;
        [JsonIgnore]
        public double GoldenChance { get; set; } = 1;

        public override string ToString() {
            string toRet = $"{Name};{Abbreviation};{Rooms.Count}";
            string debugNames = string.Join(",", Rooms);
            return $"{toRet};{debugNames}";
        }

        public static CheckpointInfo ParseString(string line) {
            List<string> parts = line.Trim().Split(new string[] { ";" }, StringSplitOptions.None).ToList();
            string name = parts[0];
            string abbreviation = parts[1];

            List<string> rooms = parts[3].Split(new string[] { "," }, StringSplitOptions.None).ToList();
            List<RoomInfo> roomInfos = new List<RoomInfo>();

            CheckpointInfo cpInfo = new CheckpointInfo() {
                Name = name,
                Abbreviation = abbreviation,
            };

            foreach (string room in rooms) {
                roomInfos.Add(new RoomInfo() { DebugRoomName = room, Checkpoint = cpInfo });
            }

            cpInfo.Rooms = roomInfos;

            return cpInfo;
        }

        public void SetCheckpointRefs() {
            foreach (RoomInfo rInfo in Rooms) {
                rInfo.Checkpoint = this;
            }
        }
    }

    [Serializable]
    public class RoomInfo {

        [JsonIgnore]
        public CheckpointInfo Checkpoint { get; set; }

        [JsonProperty("debugRoomName")]
        public string DebugRoomName { get; set; }
        public override string ToString() {
            return DebugRoomName;
        }

        [JsonIgnore]
        public int RoomNumberInCP { get; set; } = -1;
        [JsonIgnore]
        public int RoomNumberInChapter { get; set; } = -1;

        public string GetFormattedRoomName(RoomNameDisplayType format) {
            switch (format) {
                case RoomNameDisplayType.AbbreviationAndRoomNumberInCP:
                    return $"{Checkpoint.Abbreviation}-{RoomNumberInCP}";
                case RoomNameDisplayType.FullNameAndRoomNumberInCP:
                    return $"{Checkpoint.Name}-{RoomNumberInCP}";
                case RoomNameDisplayType.DebugRoomName:
                    return DebugRoomName;
            }

            return DebugRoomName;
        }

    }



    public class AggregateStats {
        public int CountSuccesses { get; set; } = 0;
        public int CountAttempts { get; set; } = 0;
        public int CountFailures {
            get {
                return CountAttempts - CountSuccesses;
            }
        }
        public float SuccessRate {
            get {
                if (CountAttempts == 0) return 0;

                return (float)CountSuccesses / CountAttempts;
            }
        }

        public int GoldenBerryDeaths { get; set; } = 0;
        public int GoldenBerryDeathsSession { get; set; } = 0;

        public float GoldenChance { get; set; } = 1;
    }
}
