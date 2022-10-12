using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     Stats to implement:
     {room:successRate} - Success Rate of the current room
     {checkpoint:successRate} - Average Success Rate of current checkpoint
     {chapter:successRate} - Average Success Rate of the entire chapter

         */
    
    public class SuccessRateStat : Stat {

        public static string RoomSuccessRate = "{room:successRate}";
        public static string CheckpointSuccessRate = "{checkpoint:successRate}";
        public static string ChapterSuccessRate = "{chapter:successRate}";
        public static List<string> IDs = new List<string>() { RoomSuccessRate, CheckpointSuccessRate, ChapterSuccessRate };

        public SuccessRateStat() : base(IDs) {}

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            int decimalPlaces = ConsistencyTrackerModule.Instance.ModSettings.LiveDataDecimalPlaces;
            int attemptCount = ConsistencyTrackerModule.Instance.ModSettings.LiveDataSelectedAttemptCount;

            format = format.Replace(RoomSuccessRate, $"{Math.Round(chapterStats.CurrentRoom.AverageSuccessOverN(attemptCount) * 100, decimalPlaces)}%");
            format = format.Replace(ChapterSuccessRate, $"{Math.Round(chapterPath.Stats.SuccessRate * 100, decimalPlaces)}%");

            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, CheckpointSuccessRate);
                return format;
            }

            bool foundRoom = false;

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        format = format.Replace(CheckpointSuccessRate, $"{Math.Round(cpInfo.Stats.SuccessRate * 100, decimalPlaces)}% ");
                        foundRoom = true;
                        break;
                    }
                }
            }

            if (!foundRoom) {
                format = format.Replace(CheckpointSuccessRate, $"-%");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }
    }
}
