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

        public static List<string> IDs = new List<string>() {
            RoomDiffInCpPercent, RoomDiffInChapterPercent, CheckpointDiffInChapterPercent,
            RoomDiffChapterProgress
        };

        public DifficultyWeightStats() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingValueFormatPercent(format, RoomDiffInCpPercent);
                format = StatManager.MissingValueFormatPercent(format, RoomDiffInChapterPercent);
                format = StatManager.MissingValueFormatPercent(format, CheckpointDiffInChapterPercent);
                
                format = StatManager.MissingValueFormatPercent(format, RoomDiffChapterProgress);
                return format;
            }
            
            int decimalPlaces = StatManager.DecimalPlaces;

            if (chapterPath.CurrentRoom == null) { //Player is not on path
                format = StatManager.NotOnPathFormatPercent(format, RoomDiffInCpPercent);
                format = StatManager.NotOnPathFormatPercent(format, RoomDiffInChapterPercent);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointDiffInChapterPercent);
                
                format = StatManager.NotOnPathFormatPercent(format, RoomDiffChapterProgress);
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
                if (tempRoom == rInfo) break; //Current room is uncleared, so stop before this is counted
                int roomDiff = tempRoom.DifficultyWeight;
                if (roomDiff == -1) {
                    roomDiff = ConsoleCommands.GetRoomDifficultyBasedOnStats(chokeRateData, tempRoom);
                }
                pastDifficulty += roomDiff;
            }
            double roomDiffChapterProgressPercent = Math.Round((double)pastDifficulty / chapterWeight * 100, decimalPlaces);
            format = format.Replace(RoomDiffChapterProgress, $"{roomDiffChapterProgressPercent}%");
            
            
            return format;
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
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {};
        }
    }
}
