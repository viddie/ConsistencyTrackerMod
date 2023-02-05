using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     Stats to implement:
     {room:chokeRate}
     {room:chokeRateSession}
     {checkpoint:chokeRate}
     {checkpoint:chokeRateSession}

         */

    public class ChokeRateStat : Stat {

        public static string RoomChokeRate = "{room:chokeRate}";
        public static string RoomChokeRateSession = "{room:chokeRateSession}";
        public static string CheckpointChokeRate = "{checkpoint:chokeRate}";
        public static string CheckpointChokeRateSession = "{checkpoint:chokeRateSession}";
        
        public static string RoomGoldenEntries = "{room:goldenEntries}";
        public static string RoomGoldenEntriesSession = "{room:goldenEntriesSession}";
        public static List<string> IDs = new List<string>() {
            RoomChokeRate, RoomChokeRateSession,
            CheckpointChokeRate, CheckpointChokeRateSession,
            RoomGoldenEntries, RoomGoldenEntriesSession
        };

        public ChokeRateStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RoomChokeRate);
                format = StatManager.MissingPathFormat(format, RoomChokeRateSession);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRate);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRateSession);

                format = StatManager.MissingPathFormat(format, RoomGoldenEntries);
                format = StatManager.MissingPathFormat(format, RoomGoldenEntriesSession);
                return format;
                
            } else if (chapterPath.CurrentRoom == null) { //Player is not on the path
                format = StatManager.NotOnPathFormatPercent(format, RoomChokeRate);
                format = StatManager.NotOnPathFormatPercent(format, RoomChokeRateSession);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointChokeRate);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointChokeRateSession);
                
                format = StatManager.NotOnPathFormat(format, RoomGoldenEntries);
                format = StatManager.NotOnPathFormat(format, RoomGoldenEntriesSession);
                return format;
            }

            //======== Room ========
            int[] goldenDeathsInRoom = new int[] {0, 0};
            int[] goldenDeathsAfterRoom = new int[] {0, 0};

            bool pastRoom = false;

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = chapterStats.GetRoom(rInfo.DebugRoomName);

                    if (pastRoom) {
                        goldenDeathsAfterRoom[0] += rStats.GoldenBerryDeaths;
                        goldenDeathsAfterRoom[1] += rStats.GoldenBerryDeathsSession;
                    }

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        pastRoom = true;
                        goldenDeathsInRoom[0] = rStats.GoldenBerryDeaths;
                        goldenDeathsInRoom[1] = rStats.GoldenBerryDeathsSession;
                    }
                }
            }

            float crRoom, crRoomSession;

            //Calculate
            int roomEntries = goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0];
            int roomEntriesSession = goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1];
            bool inGoldenRun = chapterStats.ModState.PlayerIsHoldingGolden;

            if (roomEntries == 0) crRoom = float.NaN;
            else crRoom = (float)goldenDeathsInRoom[0] / (roomEntries);

            if (roomEntriesSession == 0) crRoomSession = float.NaN;
            else crRoomSession = (float)goldenDeathsInRoom[1] / (roomEntriesSession);

            //Format
            if (float.IsNaN(crRoom)) { //pastRoom is false when player is not on path
                format = format.Replace(RoomChokeRate, $"-%");
            } else {
                format = format.Replace(RoomChokeRate, $"{StatManager.FormatPercentage(crRoom)}");
            }

            if (float.IsNaN(crRoomSession)) {
                format = format.Replace(RoomChokeRateSession, $"-%");
            } else {
                format = format.Replace(RoomChokeRateSession, $"{StatManager.FormatPercentage(crRoomSession)}");
            }


            // + 1 if you want to count the current run as well, but idk feels weird :(
            format = format.Replace(RoomGoldenEntries, inGoldenRun ? (roomEntries + 0).ToString() : roomEntries.ToString());
            format = format.Replace(RoomGoldenEntriesSession, inGoldenRun ? (roomEntriesSession + 0).ToString() : roomEntriesSession.ToString());


            //======== Checkpoint ========

            CheckpointInfo currentCpInfo = chapterPath.CurrentRoom.Checkpoint;
            
            int[] goldenDeathsInCP = new int[] { 0, 0 };
            int[] goldenDeathsAfterCP = new int[] { 0, 0 };

            bool pastCP = false;

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                if (pastCP) {
                    goldenDeathsAfterCP[0] += cpInfo.Stats.GoldenBerryDeaths;
                    goldenDeathsAfterCP[1] += cpInfo.Stats.GoldenBerryDeathsSession;
                }

                if (cpInfo == currentCpInfo) {
                    pastCP = true;
                    goldenDeathsInCP[0] = cpInfo.Stats.GoldenBerryDeaths;
                    goldenDeathsInCP[1] = cpInfo.Stats.GoldenBerryDeathsSession;
                }
            }

            float crCheckpoint, crCheckpointSession;

            //Calculate
            if (goldenDeathsInCP[0] + goldenDeathsAfterCP[0] == 0) crCheckpoint = float.NaN;
            else crCheckpoint = (float)goldenDeathsInCP[0] / (goldenDeathsInCP[0] + goldenDeathsAfterCP[0]);

            if (goldenDeathsInCP[1] + goldenDeathsAfterCP[1] == 0) crCheckpointSession = float.NaN;
            else crCheckpointSession = (float)goldenDeathsInCP[1] / (goldenDeathsInCP[1] + goldenDeathsAfterCP[1]);

            //Format
            if (float.IsNaN(crCheckpoint)) {
                format = format.Replace(CheckpointChokeRate, $"-%");
            } else {
                format = format.Replace(CheckpointChokeRate, $"{StatManager.FormatPercentage(crCheckpoint)}");
            }

            if (float.IsNaN(crCheckpointSession)) {
                format = format.Replace(CheckpointChokeRateSession, $"-%");
            } else {
                format = format.Replace(CheckpointChokeRateSession, $"{StatManager.FormatPercentage(crCheckpointSession)}");
            }
            
            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(RoomChokeRate, "Choke Rate of the current room (how many golden runs died to this room / how many golden runs passed this room)"),
                new KeyValuePair<string, string>(RoomChokeRateSession, "Choke Rate of the current room in the current session"),
                new KeyValuePair<string, string>(CheckpointChokeRate, "Choke Rate of the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointChokeRateSession, "Choke Rate of the current checkpoint in the current session"),
                
                new KeyValuePair<string, string>(RoomGoldenEntries, "Count of entries into a room with the golden berry"),
                new KeyValuePair<string, string>(RoomGoldenEntriesSession, "Count of entries into a room with the golden berry in the current session"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat("basic-choke-rate", $"Choke Rate: {RoomChokeRate} (CP: {CheckpointChokeRate})")
            };
        }
    }
}
