using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     Stats to implement:
     {run:goldenChanceFromStart}          - Format e.g. "Start to current room: 8.5%"
     {run:goldenChanceToEnd}              - Format e.g. "Current room to end: 0.025%"
     
         */

    public class RunGoldenChanceStat : Stat {

        public static string RunGoldenChanceFromStart = "{run:goldenChanceFromStart}";
        public static string RunGoldenChanceToEnd = "{run:goldenChanceToEnd}";
        public static List<string> IDs = new List<string>() { RunGoldenChanceFromStart, RunGoldenChanceToEnd };
        
        public RunGoldenChanceStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RunGoldenChanceFromStart);
                format = StatManager.MissingPathFormat(format, RunGoldenChanceToEnd);
                return format;
            }

            if (chapterPath.CurrentRoom == null) { //or is not on the path
                format = StatManager.NotOnPathFormatPercent(format, RunGoldenChanceFromStart);
                format = StatManager.NotOnPathFormatPercent(format, RunGoldenChanceToEnd);
                return format;
            }

            float runFromStart = 1, runToEnd = 1;
            bool foundRoom = false;

            //Walk the path
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = chapterStats.GetRoom(rInfo.DebugRoomName);

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        foundRoom = true;
                    }

                    float successRate = rStats.AverageSuccessOverN(StatManager.AttemptCount);
                    if (StatManager.IgnoreUnplayedRooms && rStats.IsUnplayed) {
                        successRate = 1;
                    }

                    if (!foundRoom) { //Before the room was found
                        runFromStart *= successRate;
                    } else { //After the room was found
                        runToEnd *= successRate;
                    }
                }
            }

            format = format.Replace(RunGoldenChanceFromStart, StatManager.FormatPercentage(runFromStart));
            format = format.Replace(RunGoldenChanceToEnd, StatManager.FormatPercentage(runToEnd));

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        //run-golden-chance;Start->Room: {run:goldenChanceFromStart}\nRoom->End: {run:goldenChanceToEnd}
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(RunGoldenChanceFromStart, Dialog.Clean("CCT_STAT_RUN_GOLDEN_CHANCE_EXPLANATIONS_RUN_GOLDEN_CHANCE_FROM_START")),
                new KeyValuePair<string, string>(RunGoldenChanceToEnd, Dialog.Clean("CCT_STAT_RUN_GOLDEN_CHANCE_EXPLANATIONS_RUN_GOLDEN_CHANCE_TO_END")),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_RUN_GOLDEN_CHANCE_FORMAT_TITLE_BASIC_RUN_GOLDEN_CHANCE"),
                    $"{Dialog.Clean("CCT_STAT_RUN_GOLDEN_CHANCE_FORMAT_CONTENT_BASIC_RUN_GOLDEN_CHANCE_1")}: {RunGoldenChanceFromStart}" +
                    $"\\n{Dialog.Clean("CCT_STAT_RUN_GOLDEN_CHANCE_FORMAT_CONTENT_BASIC_RUN_GOLDEN_CHANCE_2")}: {RunGoldenChanceToEnd}")
            };
        }
    }
}
