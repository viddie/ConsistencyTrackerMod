using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Utility;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     Stats to implement:
     {room:diffInCpPercent} - The share of difficulty of the current room in the chapter
     {room:diffInChapterPercent} - The share of difficulty of the current room in the chapter
     {checkpoint:diffInChapterPercent} - The share of difficulty of the current checkpoint in the chapter
         */

    public class DifficultyWeightStats : Stat {

        public static string RoomDiffInCpPercent = "{room:diffInCpPercent}";
        public static string RoomDiffInChapterPercent = "{room:diffInChapterPercent}";
        public static string CheckpointDiffInChapterPercent = "{checkpoint:diffInChapterPercent}";
        
        public static string RoomDiffChapterProgress = "{room:diffChapterProgress}";
        public static string RoomLiveGp = "{room:liveGpValue}";
        public static string RoomLiveGoldenTier = "{room:liveGoldenTier}";
        public static string RoomLiveMaxGp = "{room:liveMaxGpValue}";
        public static string RoomLiveMaxGoldenTier = "{room:liveMaxGoldenTier}";

        public static List<string> IDs = new List<string>() {
            RoomDiffInCpPercent, RoomDiffInChapterPercent, CheckpointDiffInChapterPercent,
            RoomDiffChapterProgress,
            RoomLiveGp, RoomLiveGoldenTier,
            RoomLiveMaxGp, RoomLiveMaxGoldenTier,
        };

        public DifficultyWeightStats() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingValueFormatPercent(format, RoomDiffInCpPercent);
                format = StatManager.MissingValueFormatPercent(format, RoomDiffInChapterPercent);
                format = StatManager.MissingValueFormatPercent(format, CheckpointDiffInChapterPercent);
                
                format = StatManager.MissingValueFormatPercent(format, RoomDiffChapterProgress);
                
                format = StatManager.MissingValueFormat(format, RoomLiveGp);
                format = StatManager.MissingValueFormat(format, RoomLiveGoldenTier);
                
                format = StatManager.MissingValueFormat(format, RoomLiveMaxGp);
                format = StatManager.MissingValueFormat(format, RoomLiveMaxGoldenTier);
                return format;
            }
            
            int decimalPlaces = StatManager.DecimalPlaces;

            if (chapterPath.CurrentRoom == null) { //Player is not on path
                format = StatManager.NotOnPathFormatPercent(format, RoomDiffInCpPercent);
                format = StatManager.NotOnPathFormatPercent(format, RoomDiffInChapterPercent);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointDiffInChapterPercent);
                
                format = StatManager.NotOnPathFormatPercent(format, RoomDiffChapterProgress);
                
                format = StatManager.NotOnPathFormat(format, RoomLiveGp);
                format = StatManager.NotOnPathFormat(format, RoomLiveGoldenTier);
                
                format = StatManager.NotOnPathFormat(format, RoomLiveMaxGp);
                format = StatManager.NotOnPathFormat(format, RoomLiveMaxGoldenTier);
                return format;
            } 
            
            RoomInfo rInfo = chapterPath.CurrentRoom;
            
            var chokeRateData = ChokeRateStat.GetRoomData(chapterPath, chapterStats);
            int roomDifficulty = rInfo.DifficultyWeight;
            string tilde = "";
            if (roomDifficulty == -1) {
                roomDifficulty = ConsoleCommands.GetRoomDifficultyBasedOnStats(chokeRateData, rInfo);
                tilde = "~";
            }
            
            int cpWeight = rInfo.Checkpoint.Stats.DifficultyWeight;
            int chapterWeight = chapterPath.Stats.DifficultyWeight;
            double roomInCpPercent = Math.Round((double)roomDifficulty / cpWeight * 100, decimalPlaces);
            double roomInChapterPercent = Math.Round((double)roomDifficulty / chapterWeight * 100, decimalPlaces);
            double cpInChapterPercent = Math.Round((double)cpWeight / chapterWeight * 100, decimalPlaces);
            
            format = format.Replace(RoomDiffInCpPercent, $"{tilde}{roomInCpPercent}%");
            format = format.Replace(RoomDiffInChapterPercent, $"{tilde}{roomInChapterPercent}%");
            format = format.Replace(CheckpointDiffInChapterPercent, $"{cpInChapterPercent}%");
            
            
            //Room diff in chapter progress
            int pastDifficulty = 0;
            foreach (RoomInfo tempRoom in chapterPath.WalkPath()) {
                if (tempRoom.RoomNumberInChapter == rInfo.RoomNumberInChapter) break; //Current room is uncleared, so stop before this is counted
                int roomDiff = tempRoom.DifficultyWeight;
                if (roomDiff == -1) {
                    roomDiff = ConsoleCommands.GetRoomDifficultyBasedOnStats(chokeRateData, tempRoom);
                }
                pastDifficulty += roomDiff;
            }
            double pastDiffRatio = (double)pastDifficulty / chapterWeight;
            double roomDiffChapterProgressPercent = Math.Round(pastDiffRatio * 100, decimalPlaces);
            format = format.Replace(RoomDiffChapterProgress, $"{roomDiffChapterProgressPercent}%");
            
            //Live GP
            double maxGp = chapterPath.GoldenPoints == -1 ? chapterPath.Tier.GetGp() : chapterPath.GoldenPoints;
            double gpFraction = Math.Round(maxGp * pastDiffRatio, decimalPlaces);
            int enduranceSelected = chapterPath.EnduranceFactor;
            int endurancePowerSelected = chapterPath.EndurancePower;
            
            /*
             //magnitudes of 10: 0.0001, 0.001, 0.01, 0.1, ~1, 10, 100, 1000, 10000
            double enduranceConstant;
            enduranceConstant = Math.Pow(10, -(5 - enduranceSelected));
            if (enduranceConstant == 1) enduranceConstant = 1.00001f; //Close to 1, but can't be exactly 1
            double enduranceFactor = EnduranceFactor(pastDiffRatio, enduranceConstant);
            */
            
            /*
            //Attempt 2:
            */
            //1 = 1, 2 = 1.5, 3 = 2, 4 = 2.5, 5 = 3, 6 = 3.5, 7 = 4, 8 = 4.5, 9 = 5
            double enduranceConstant = 1 + enduranceSelected / 10f;
            double endurancePower = endurancePowerSelected / 10f;
            double enduranceFactor = EnduranceFactor(pastDiffRatio, enduranceConstant, endurancePower);
            
            /*
            //Attempt 3:
            double enduranceConstant = Math.Max(1, enduranceSelected);
            double enduranceFactor = EnduranceFactor(pastDiffRatio, enduranceConstant);
            */
            
            
            //double liveGp = Math.Round(enduranceFactor * gpFraction, decimalPlaces);
            double liveGp = Math.Round(enduranceFactor * maxGp, decimalPlaces);
            
            GoldenTier inverseTier = GoldenTier.GetTierByGp(liveGp);
            GoldenTier inverseMaxtier = GoldenTier.GetTierByGp(maxGp);
            
            format = format.Replace(RoomLiveGp, $"{liveGp} gp");
            format = format.Replace(RoomLiveGoldenTier, $"{inverseTier.GetTierString(true)}");
            
            format = format.Replace(RoomLiveMaxGp, $"{Math.Round(maxGp, decimalPlaces)} gp");
            format = format.Replace(RoomLiveMaxGoldenTier, $"{inverseMaxtier.GetTierString(true)}");
            
            //Add stat: current room gp value
            //Add graph to text menu
            
            return format;
        }

        /**
         * t goes from 0 to 1
         * a can be anything above 0, except 1. if the value is between 0 and 1 its a concave slope, above 1 its a convex slope
         * return value also goes from 0 to 1
         * calculates the following function:
         * f(t) = (a^t - 1) / (a - 1)
        public static double EnduranceFactor(double t, double a = 5) {
            return (Math.Pow(a, t) - 1) / (a - 1);
        }
         */
        
        /*
         Different function:
         f(t) = ((t^a) / (t^a + (1 - t)^a)) ^b
         */
        public static double EnduranceFactor(double t, double a, double b) {
            return Math.Pow(Math.Pow(t, a) / (Math.Pow(t, a) + Math.Pow(1 - t, a)), b);
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>("{room:diffInCpPercent}", "The share of difficulty of the current room in the checkpoint"),
                new KeyValuePair<string, string>("{room:diffInChapterPercent}", "The share of difficulty of the current room in the chapter"),
                new KeyValuePair<string, string>("{checkpoint:diffInChapterPercent}", "The share of difficulty of the current checkpoint in the chapter"),
                
                new KeyValuePair<string, string>("{room:diffChapterProgress}", "How much difficulty the player has passed in the chapter so far"),
                
                new KeyValuePair<string, string>($"{RoomLiveGp}", "Live goldberry points value of the current run."),
                new KeyValuePair<string, string>($"{RoomLiveGoldenTier}", "Live golden tier of the current run."),
                new KeyValuePair<string, string>($"{RoomLiveMaxGp}", "Live goldberry points value of the current run."),
                new KeyValuePair<string, string>($"{RoomLiveMaxGoldenTier}", "Live golden tier of the current run."),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {};
        }
    }
}
