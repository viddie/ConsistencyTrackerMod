using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
        Red: {chapter:color-red}
        Yellow: {chapter:color-yellow}
        Green: {chapter:color-green}
        Light-Green: {chapter:color-lightGreen}

         */

    public class SuccessRateColorsStat : Stat {

        public static string ChapterColorRed = "{chapter:color-red}";
        public static string ChapterColorYellow = "{chapter:color-yellow}";
        public static string ChapterColorGreen = "{chapter:color-green}";
        public static string ChapterColorLightGreen = "{chapter:color-lightGreen}";

        public static string CheckpointColorRed = "{checkpoint:color-red}";
        public static string CheckpointColorYellow = "{checkpoint:color-yellow}";
        public static string CheckpointColorGreen = "{checkpoint:color-green}";
        public static string CheckpointColorLightGreen = "{checkpoint:color-lightGreen}";

        public static string ChapterListColorRed = "{chapter:listColor-red}";
        public static string CheckpointListColorRed = "{checkpoint:listColor-red}";

        public static List<string> IDs = new List<string>() {
            ChapterColorRed, ChapterColorYellow, ChapterColorGreen, ChapterColorLightGreen,
            CheckpointColorRed, CheckpointColorYellow, CheckpointColorGreen, CheckpointColorLightGreen,
            ChapterListColorRed, CheckpointListColorRed,
        };

        public SuccessRateColorsStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ChapterColorRed);
                format = StatManager.MissingPathFormat(format, ChapterColorYellow);
                format = StatManager.MissingPathFormat(format, ChapterColorGreen);
                format = StatManager.MissingPathFormat(format, ChapterColorLightGreen);

                format = StatManager.MissingPathFormat(format, CheckpointColorRed);
                format = StatManager.MissingPathFormat(format, CheckpointColorYellow);
                format = StatManager.MissingPathFormat(format, CheckpointColorGreen);
                format = StatManager.MissingPathFormat(format, CheckpointColorLightGreen);

                format = StatManager.MissingPathFormat(format, ChapterListColorRed);
                format = StatManager.MissingPathFormat(format, CheckpointListColorRed);
                return format;
            }


            //Light Green, Green, Yellow, Red
            int[] colorCounts = new int[] { 0, 0, 0, 0 };
            int[] colorCountsCP = new int[] { 0, 0, 0, 0 };

            List<string> listColorRedChapter = new List<string>();
            List<string> listColorRedCheckpoint = new List<string>();

            //Walk path
            bool foundRoom = false;
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                int[] tempColorCountsCp = new int[] { 0, 0, 0, 0 };
                List<string> tempColorListCp = new List<string>();

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = chapterStats.GetRoom(rInfo.DebugRoomName);

                    float successRate = rStats.AverageSuccessOverN(StatManager.AttemptCount);

                    if (successRate >= 0.949999) {
                        colorCounts[0]++;
                        tempColorCountsCp[0]++;

                    } else if (successRate >= 0.8) {
                        colorCounts[1]++;
                        tempColorCountsCp[1]++;

                    } else if (successRate >= 0.5) {
                        colorCounts[2]++;
                        tempColorCountsCp[2]++;

                    } else {
                        colorCounts[3]++;
                        tempColorCountsCp[3]++;
                        listColorRedChapter.Add(StatManager.GetFormattedRoomName(rInfo));
                        tempColorListCp.Add(StatManager.GetFormattedRoomName(rInfo));
                    }

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        foundRoom = true;
                    }
                }

                if (foundRoom) {
                    foundRoom = false;
                    colorCountsCP = tempColorCountsCp;
                    listColorRedCheckpoint = tempColorListCp;
                }
            }

            format = format.Replace(ChapterColorLightGreen, $"{colorCounts[0]}");
            format = format.Replace(ChapterColorGreen, $"{colorCounts[1]}");
            format = format.Replace(ChapterColorYellow, $"{colorCounts[2]}");
            format = format.Replace(ChapterColorRed, $"{colorCounts[3]}");

            if (chapterPath.CurrentRoom == null) {
                format = StatManager.NotOnPathFormat(format, CheckpointColorLightGreen);
                format = StatManager.NotOnPathFormat(format, CheckpointColorGreen);
                format = StatManager.NotOnPathFormat(format, CheckpointColorYellow);
                format = StatManager.NotOnPathFormat(format, CheckpointColorRed);

                format = StatManager.NotOnPathFormat(format, CheckpointListColorRed);
            } else {
                format = format.Replace(CheckpointColorLightGreen, $"{colorCountsCP[0]}");
                format = format.Replace(CheckpointColorGreen, $"{colorCountsCP[1]}");
                format = format.Replace(CheckpointColorYellow, $"{colorCountsCP[2]}");
                format = format.Replace(CheckpointColorRed, $"{colorCountsCP[3]}");

                format = format.Replace(CheckpointListColorRed, $"{string.Join(", ", listColorRedCheckpoint)}");
            }

            format = format.Replace(ChapterListColorRed, $"{string.Join(", ", listColorRedChapter)}");

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        //color-tracker;Reds: {chapter:color-red}, Yellows: {chapter:color-yellow}, Greens: {chapter:color-green}, Light-Greens: {chapter:color-lightGreen}
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(ChapterColorRed, "Count of red rooms (success rate <50%)"),
                new KeyValuePair<string, string>(ChapterColorYellow, "Count of yellow rooms (success rate 50%-80%)"),
                new KeyValuePair<string, string>(ChapterColorGreen, "Count of green rooms (success rate 80%-95%)"),
                new KeyValuePair<string, string>(ChapterColorLightGreen, "Count of light green rooms (success rate 95%-100%)"),

                new KeyValuePair<string, string>(CheckpointColorRed, "Count of red rooms in the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointColorYellow, "Count of yellow rooms in the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointColorGreen, "Count of green rooms in the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointColorLightGreen, "Count of light green rooms in the current checkpoint"),

                new KeyValuePair<string, string>(ChapterListColorRed, "Lists all red rooms in the chapter by name"),
                new KeyValuePair<string, string>(CheckpointListColorRed, "Lists all red rooms in the current checkpoint by name"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat("color-tracker", $"Reds: {ChapterColorRed}, Yellows: {ChapterColorYellow}, Greens: {ChapterColorGreen}, Light-Greens: {ChapterColorLightGreen}"),
                new StatFormat("color-tracker-cp", $"Checkpoint: Reds: {CheckpointColorRed}, Yellows: {CheckpointColorYellow}, Greens: {CheckpointColorGreen}, Light-Greens: {CheckpointColorLightGreen}"),
                new StatFormat("red-rooms-list", $"All red rooms: {ChapterListColorRed}\\nIn CP: {CheckpointListColorRed}")
            };
        }
    }
}
