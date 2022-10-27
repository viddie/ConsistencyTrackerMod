using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
        Red: {chapter:color-red}
        Yellow: {chapter:color-yellow}
        Green: {chapter:color-green}
        Light-Green: {chapter:color-lightGreen}

         */

    public class SuccessRateColorsStat : Stat {

        public static string ColorRed = "{chapter:color-red}";
        public static string ColorYellow = "{chapter:color-yellow}";
        public static string ColorGreen = "{chapter:color-green}";
        public static string ColorLightGreen = "{chapter:color-lightGreen}";

        public static string ColorRedCP = "{checkpoint:color-red}";
        public static string ColorYellowCP = "{checkpoint:color-yellow}";
        public static string ColorGreenCP = "{checkpoint:color-green}";
        public static string ColorLightGreenCP = "{checkpoint:color-lightGreen}";
        public static List<string> IDs = new List<string>() {
            ColorRed, ColorYellow, ColorGreen, ColorLightGreen,
            ColorRedCP, ColorYellowCP, ColorGreenCP, ColorLightGreenCP
        };

        public SuccessRateColorsStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ColorRed);
                format = StatManager.MissingPathFormat(format, ColorYellow);
                format = StatManager.MissingPathFormat(format, ColorGreen);
                format = StatManager.MissingPathFormat(format, ColorLightGreen);

                format = StatManager.MissingPathFormat(format, ColorRedCP);
                format = StatManager.MissingPathFormat(format, ColorYellowCP);
                format = StatManager.MissingPathFormat(format, ColorGreenCP);
                format = StatManager.MissingPathFormat(format, ColorLightGreenCP);
                return format;
            }


            //Light Green, Green, Yellow, Red
            int[] colorCounts = new int[] { 0, 0, 0, 0 };
            int[] colorCountsCP = new int[] { 0, 0, 0, 0 };
             
            //Walk path
            bool foundRoom = false;
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                int[] tempColorCountsCp = new int[] { 0, 0, 0, 0 };

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = chapterStats.GetRoom(rInfo.DebugRoomName);

                    float successRate = rStats.AverageSuccessOverSelectedN();

                    if (successRate >= 0.95) {
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
                    }

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        foundRoom = true;
                    }
                }

                if (foundRoom) {
                    foundRoom = false;
                    colorCountsCP = tempColorCountsCp;
                }
            }

            format = format.Replace(ColorLightGreen, $"{colorCounts[0]}");
            format = format.Replace(ColorGreen, $"{colorCounts[1]}");
            format = format.Replace(ColorYellow, $"{colorCounts[2]}");
            format = format.Replace(ColorRed, $"{colorCounts[3]}");

            format = format.Replace(ColorLightGreenCP, $"{colorCountsCP[0]}");
            format = format.Replace(ColorGreenCP, $"{colorCountsCP[1]}");
            format = format.Replace(ColorYellowCP, $"{colorCountsCP[2]}");
            format = format.Replace(ColorRedCP, $"{colorCountsCP[3]}");

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        //color-tracker;Reds: {chapter:color-red}, Yellows: {chapter:color-yellow}, Greens: {chapter:color-green}, Light-Greens: {chapter:color-lightGreen}
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(ColorRed, "Count of red rooms (success rate <50%)"),
                new KeyValuePair<string, string>(ColorYellow, "Count of yellow rooms (success rate 50%-80%)"),
                new KeyValuePair<string, string>(ColorGreen, "Count of green rooms (success rate 80%-95%)"),
                new KeyValuePair<string, string>(ColorLightGreen, "Count of light green rooms (success rate 95%-100%)"),

                new KeyValuePair<string, string>(ColorRedCP, "Count of red rooms in the current checkpoint"),
                new KeyValuePair<string, string>(ColorYellowCP, "Count of yellow rooms in the current checkpoint"),
                new KeyValuePair<string, string>(ColorGreenCP, "Count of green rooms in the current checkpoint"),
                new KeyValuePair<string, string>(ColorLightGreenCP, "Count of light green rooms in the current checkpoint"),
            };
        }
        public override List<StatFormat> GetStatExamples() {
            return new List<StatFormat>() {
                new StatFormat("color-tracker", $"Reds: {ColorRed}, Yellows: {ColorYellow}, Greens: {ColorGreen}, Light-Greens: {ColorLightGreen}"),
                new StatFormat("color-tracker-cp", $"Checkpoint: Reds: {ColorRedCP}, Yellows: {ColorYellowCP}, Greens: {ColorGreenCP}, Light-Greens: {ColorLightGreenCP}")
            };
        }
    }
}
