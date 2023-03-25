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
        public static string RoomGoldenSuccessRate = "{room:goldenSuccessRate}";
        public static string RoomGoldenSuccessRateSession = "{room:goldenSuccessRateSession}";
        public static string CheckpointGoldenSuccessRate = "{checkpoint:goldenSuccessRate}";
        public static string CheckpointGoldenSuccessRateSession = "{checkpoint:goldenSuccessRateSession}";

        public static string RoomGoldenEntries = "{room:goldenEntries}";
        public static string RoomGoldenEntriesSession = "{room:goldenEntriesSession}";
        public static string RoomGoldenSuccesses = "{room:goldenSuccesses}";
        public static string RoomGoldenSuccessesSession = "{room:goldenSuccessesSession}";
        public static List<string> IDs = new List<string>() {
            RoomChokeRate, RoomChokeRateSession,
            CheckpointChokeRate, CheckpointChokeRateSession,
            RoomGoldenSuccessRate, RoomGoldenSuccessRateSession,
            CheckpointGoldenSuccessRate, CheckpointGoldenSuccessRateSession,
            RoomGoldenEntries, RoomGoldenEntriesSession,
            RoomGoldenSuccesses, RoomGoldenSuccessesSession,
        };

        public ChokeRateStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RoomChokeRate);
                format = StatManager.MissingPathFormat(format, RoomChokeRateSession);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRate);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRateSession);

                format = StatManager.MissingPathFormat(format, RoomGoldenSuccessRate);
                format = StatManager.MissingPathFormat(format, RoomGoldenSuccessRateSession);
                format = StatManager.MissingPathFormat(format, CheckpointGoldenSuccessRate);
                format = StatManager.MissingPathFormat(format, CheckpointGoldenSuccessRateSession);

                format = StatManager.MissingPathFormat(format, RoomGoldenEntries);
                format = StatManager.MissingPathFormat(format, RoomGoldenEntriesSession);
                format = StatManager.MissingPathFormat(format, RoomGoldenSuccesses);
                format = StatManager.MissingPathFormat(format, RoomGoldenSuccessesSession);

                return format;
                
            } else if (chapterPath.CurrentRoom == null) { //Player is not on the path
                format = StatManager.NotOnPathFormatPercent(format, RoomChokeRate);
                format = StatManager.NotOnPathFormatPercent(format, RoomChokeRateSession);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointChokeRate);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointChokeRateSession);

                format = StatManager.NotOnPathFormatPercent(format, RoomGoldenSuccessRate);
                format = StatManager.NotOnPathFormatPercent(format, RoomGoldenSuccessRateSession);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointGoldenSuccessRate);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointGoldenSuccessRateSession);

                format = StatManager.NotOnPathFormat(format, RoomGoldenEntries);
                format = StatManager.NotOnPathFormat(format, RoomGoldenEntriesSession);
                format = StatManager.NotOnPathFormat(format, RoomGoldenSuccesses);
                format = StatManager.NotOnPathFormat(format, RoomGoldenSuccessesSession);

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
            float goldenSuccessRateRoom, goldenSuccessRateRoomSession;

            //Calculate
            int roomEntries = goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0];
            int roomEntriesSession = goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1];
            bool inGoldenRun = chapterStats.ModState.PlayerIsHoldingGolden;
            int roomSuccesses = goldenDeathsAfterRoom[0];
            int roomSuccessesSession = goldenDeathsAfterRoom[1];

            if (roomEntries == 0) crRoom = float.NaN;
            else crRoom = (float)goldenDeathsInRoom[0] / (roomEntries);

            if (roomEntriesSession == 0) crRoomSession = float.NaN;
            else crRoomSession = (float)goldenDeathsInRoom[1] / (roomEntriesSession);

            if (float.IsNaN(crRoom)) goldenSuccessRateRoom = float.NaN;
            else goldenSuccessRateRoom = 1 - crRoom;

            if (float.IsNaN(crRoomSession)) goldenSuccessRateRoomSession = float.NaN;
            else goldenSuccessRateRoomSession = 1 - crRoomSession;

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

            //Golden Success Rate
            if (float.IsNaN(goldenSuccessRateRoom)) {
                format = format.Replace(RoomGoldenSuccessRate, $"-%");
            } else {
                format = format.Replace(RoomGoldenSuccessRate, $"{StatManager.FormatPercentage(goldenSuccessRateRoom)}");
            }

            if (float.IsNaN(goldenSuccessRateRoomSession)) {
                format = format.Replace(RoomGoldenSuccessRateSession, $"-%");
            } else {
                format = format.Replace(RoomGoldenSuccessRateSession, $"{StatManager.FormatPercentage(goldenSuccessRateRoomSession)}");
            }


            // + 1 if you want to count the current run as well, but idk feels weird :(
            format = format.Replace(RoomGoldenEntries, inGoldenRun ? (roomEntries + 0).ToString() : roomEntries.ToString());
            format = format.Replace(RoomGoldenEntriesSession, inGoldenRun ? (roomEntriesSession + 0).ToString() : roomEntriesSession.ToString());

            format = format.Replace(RoomGoldenSuccesses, roomSuccesses.ToString());
            format = format.Replace(RoomGoldenSuccessesSession, roomSuccessesSession.ToString());


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
            float goldenSuccessRateCp, goldenSuccessRateCpSession;

            //Calculate
            if (goldenDeathsInCP[0] + goldenDeathsAfterCP[0] == 0) crCheckpoint = float.NaN;
            else crCheckpoint = (float)goldenDeathsInCP[0] / (goldenDeathsInCP[0] + goldenDeathsAfterCP[0]);

            if (goldenDeathsInCP[1] + goldenDeathsAfterCP[1] == 0) crCheckpointSession = float.NaN;
            else crCheckpointSession = (float)goldenDeathsInCP[1] / (goldenDeathsInCP[1] + goldenDeathsAfterCP[1]);

            if (float.IsNaN(crCheckpoint)) goldenSuccessRateCp = float.NaN;
            else goldenSuccessRateCp = 1 - crCheckpoint;

            if (float.IsNaN(crCheckpointSession)) goldenSuccessRateCpSession = float.NaN;
            else goldenSuccessRateCpSession = 1 - crCheckpointSession;

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

            //Golden Success Rate Checkpoint
            if (float.IsNaN(goldenSuccessRateCp)) {
                format = format.Replace(CheckpointGoldenSuccessRate, $"-%");
            } else {
                format = format.Replace(CheckpointGoldenSuccessRate, $"{StatManager.FormatPercentage(goldenSuccessRateCp)}");
            }

            if (float.IsNaN(goldenSuccessRateCpSession)) {
                format = format.Replace(CheckpointGoldenSuccessRateSession, $"-%");
            } else {
                format = format.Replace(CheckpointGoldenSuccessRateSession, $"{StatManager.FormatPercentage(goldenSuccessRateCpSession)}");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(RoomChokeRate, "Choke Rate of the current room (how many golden runs died to this room / how many golden runs entered this room)"),
                new KeyValuePair<string, string>(RoomChokeRateSession, "Choke Rate of the current room in the current session"),
                new KeyValuePair<string, string>(CheckpointChokeRate, "Choke Rate of the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointChokeRateSession, "Choke Rate of the current checkpoint in the current session"),
                new KeyValuePair<string, string>(RoomGoldenSuccessRate, "Golden Success Rate of the current room (how many golden runs completed this room / how many golden runs entered this room)"),
                new KeyValuePair<string, string>(RoomGoldenSuccessRateSession, "Golden Success Rate of the current room in the current session"),
                new KeyValuePair<string, string>(CheckpointGoldenSuccessRate, "Golden Success Rate of the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointGoldenSuccessRateSession, "Golden Success Rate of the current checkpoint in the current session"),

                new KeyValuePair<string, string>(RoomGoldenEntries, "Count of entries into a room with the golden berry"),
                new KeyValuePair<string, string>(RoomGoldenEntriesSession, "Count of entries into a room with the golden berry in the current session"),
                new KeyValuePair<string, string>(RoomGoldenSuccesses, "Count of successes of a room with the golden berry"),
                new KeyValuePair<string, string>(RoomGoldenSuccessesSession, "Count of successes of a room with the golden berry in the current session"),

            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat("basic-choke-rate", $"Choke Rate: {RoomChokeRate} (CP: {CheckpointChokeRate})"),
                new StatFormat("basic-golden-success-rate", $"Golden Success Rate: {RoomGoldenSuccessRate} ({RoomGoldenSuccesses}/{RoomGoldenEntries})"),
            };
        }
    }
}
