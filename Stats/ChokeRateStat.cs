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
        public static List<string> IDs = new List<string>() { RoomChokeRate, RoomChokeRateSession, CheckpointChokeRate, CheckpointChokeRateSession };

        public ChokeRateStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RoomChokeRate);
                format = StatManager.MissingPathFormat(format, RoomChokeRateSession);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRate);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRateSession);
                return format;
            }

            int decimalPlaces = ConsistencyTrackerModule.Instance.ModSettings.LiveDataDecimalPlaces;

            CheckpointInfo currentCpInfo = null;

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
                        currentCpInfo = cpInfo;
                        pastRoom = true;
                        goldenDeathsInRoom[0] = rStats.GoldenBerryDeaths;
                        goldenDeathsInRoom[1] = rStats.GoldenBerryDeathsSession;
                    }
                }
            }

            float crRoom, crRoomSession;

            //Calculate
            if (goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0] == 0) crRoom = float.NaN;
            else crRoom = (float)goldenDeathsInRoom[0] / (goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0]);

            if (goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1] == 0) crRoomSession = float.NaN;
            else crRoomSession = (float)goldenDeathsInRoom[1] / (goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1]);

            //Format
            if (float.IsNaN(crRoom) || pastRoom == false) { //pastRoom is false when player is not on path
                format = format.Replace(RoomChokeRate, $"-%");
            } else {
                format = format.Replace(RoomChokeRate, $"{Math.Round(crRoom * 100, decimalPlaces)}%");
            }

            if (float.IsNaN(crRoomSession) || pastRoom == false) {
                format = format.Replace(RoomChokeRateSession, $"-%");
            } else {
                format = format.Replace(RoomChokeRateSession, $"{Math.Round(crRoomSession * 100, decimalPlaces)}%");
            }

            //======== Checkpoint ========

            if (currentCpInfo != null) { //Check if player is on path
                int[] goldenDeathsInCP = new int[] { 0, 0 };
                int[] goldenDeathsAfterCP = new int[] { 0, 0 };

                bool pastCP = false;

                foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                    if (pastCP) {
                        goldenDeathsAfterCP[0] += cpInfo.Stats.CountGoldenBerryDeaths;
                        goldenDeathsAfterCP[1] += cpInfo.Stats.CountGoldenBerryDeathsSession;
                    }

                    if (cpInfo == currentCpInfo) {
                        pastCP = true;
                        goldenDeathsInCP[0] = cpInfo.Stats.CountGoldenBerryDeaths;
                        goldenDeathsInCP[1] = cpInfo.Stats.CountGoldenBerryDeathsSession;
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
                    format = format.Replace(CheckpointChokeRate, $"{Math.Round(crCheckpoint * 100, decimalPlaces)}%");
                }

                if (float.IsNaN(crCheckpointSession)) {
                    format = format.Replace(CheckpointChokeRateSession, $"-%");
                } else {
                    format = format.Replace(CheckpointChokeRateSession, $"{Math.Round(crCheckpointSession * 100, decimalPlaces)}%");
                }
            } else {
                //Player is not on path
                format = format.Replace(CheckpointChokeRate, $"-%");
                format = format.Replace(CheckpointChokeRateSession, $"-%");
            }


            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }
    }
}
