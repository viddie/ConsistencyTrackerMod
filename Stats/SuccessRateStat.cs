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
            int attemptCount = StatManager.AttemptCount;

            format = format.Replace(RoomSuccessRate, $"{StatManager.FormatPercentage(chapterStats.CurrentRoom.AverageSuccessOverN(attemptCount))}");

            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, CheckpointSuccessRate);
                format = StatManager.MissingPathFormat(format, ChapterSuccessRate);
                return format;
            }

            format = format.Replace(ChapterSuccessRate, $"{StatManager.FormatPercentage(chapterPath.Stats.SuccessRate)}");

            bool foundRoom = false;

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        format = format.Replace(CheckpointSuccessRate, $"{StatManager.FormatPercentage(cpInfo.Stats.SuccessRate)}");
                        foundRoom = true;
                        break;
                    }
                }
            }

            if (!foundRoom) {
                format = StatManager.NotOnPathFormatPercent(format, CheckpointSuccessRate);
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }
    }
}
