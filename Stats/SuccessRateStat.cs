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

     {room:successes} - Count of successful room clears within the last X attempts
     {room:failures} - Count of deaths trying to clear the room within the last X attempts
     {room:attempts} - Count of at max. X last attempts

         */

    public class SuccessRateStat : Stat {

        public static string RoomSuccessRate = "{room:successRate}";
        public static string CheckpointSuccessRate = "{checkpoint:successRate}";
        public static string ChapterSuccessRate = "{chapter:successRate}";
        public static string RoomSuccesses = "{room:successes}";
        public static string RoomFailures = "{room:failures}";
        public static string RoomAttempts = "{room:attempts}";
        public static List<string> IDs = new List<string>() {
            RoomSuccessRate, CheckpointSuccessRate, ChapterSuccessRate,
            RoomSuccesses, RoomFailures, RoomAttempts
        };

        public SuccessRateStat() : base(IDs) {}

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            int attemptCount = StatManager.AttemptCount;

            format = format.Replace(RoomSuccessRate, $"{StatManager.FormatPercentage(chapterStats.CurrentRoom.AverageSuccessOverN(attemptCount))}");

            int successes = chapterStats.CurrentRoom.SuccessesOverN(attemptCount);
            int attempts = chapterStats.CurrentRoom.AttemptsOverN(attemptCount);
            int failures = attempts - successes;

            format = format.Replace(RoomSuccesses, $"{successes}");
            format = format.Replace(RoomFailures, $"{failures}");
            format = format.Replace(RoomAttempts, $"{attempts}");


            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, CheckpointSuccessRate);
                format = StatManager.MissingPathFormat(format, ChapterSuccessRate);
                return format;
            }

            format = format.Replace(ChapterSuccessRate, $"{StatManager.FormatPercentage(chapterPath.Stats.SuccessRate)}");

            bool foundRoom = false;

            //Walk the path
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


        //success-rate;Room SR: {room:successRate} | CP: {checkpoint:successRate} | Total: {chapter:successRate}
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(RoomSuccessRate, Dialog.Clean("CCT_STAT_SUCCESS_RATE_EXPLANATIONS_ROOM_SUCCESS_RATE")),
                new KeyValuePair<string, string>(CheckpointSuccessRate, Dialog.Clean("CCT_STAT_SUCCESS_RATE_EXPLANATIONS_CHECKPOINT_SUCCESS_RATE")),
                new KeyValuePair<string, string>(ChapterSuccessRate, Dialog.Clean("CCT_STAT_SUCCESS_RATE_EXPLANATIONS_CHAPTER_SUCCESS_RATE")),

                new KeyValuePair<string, string>(RoomSuccesses, Dialog.Clean("CCT_STAT_SUCCESS_RATE_EXPLANATIONS_ROOM_SUCCESSES")),
                new KeyValuePair<string, string>(RoomFailures, Dialog.Clean("CCT_STAT_SUCCESS_RATE_EXPLANATIONS_ROOM_FAILURES")),
                new KeyValuePair<string, string>(RoomAttempts, Dialog.Clean("CCT_STAT_SUCCESS_RATE_EXPLANATIONS_ROOM_ATTEMPTS")),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_SUCCESS_RATE_FORMAT_TITLE_BASIC_SUCCESS_RATE"), $"{Dialog.Clean("CCT_STAT_SUCCESS_RATE_FORMAT_CONTENT_BASIC_SUCCESS_RATE_1")}: {RoomSuccessRate} ({RoomSuccesses}/{RoomAttempts}) | {Dialog.Clean("CCT_STAT_SUCCESS_RATE_FORMAT_CONTENT_BASIC_SUCCESS_RATE_2")}: {CheckpointSuccessRate} | {Dialog.Clean("CCT_STAT_SUCCESS_RATE_FORMAT_CONTENT_BASIC_SUCCESS_RATE_3")}: {ChapterSuccessRate}")
            };
        }
    }
}
