using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     Stats to implement:
     {chapter:roomCount} - How many rooms on entire path
     {checkpoint:roomCount} - How many rooms in current CP
     {room:roomNumberInChapter} - On what room number out of the entire chapter is the player rn
     {room:roomNumberInCheckpoint} - On what room number out of the current checkpoint is the player rn
     
     {room:chapterProgressPercent} - ({run:chapterProgressNumber} - {chapter:roomCount})%
     {room:checkpointProgressPercent} - ({run:checkpointProgressNumber} - {checkpoint:roomCount})%

         */

    public class LiveProgressStat : Stat {

        public static string ChapterRoomCount = "{chapter:roomCount}";
        public static string CheckpointRoomCount = "{checkpoint:roomCount}";
        public static string RunChapterProgressNumber = "{room:roomNumberInChapter}";
        public static string RunCheckpointProgressNumber = "{room:roomNumberInCheckpoint}";
        public static string RunChapterProgressPercent = "{room:chapterProgressPercent}";
        public static string RunCheckpointProgressPercent = "{room:checkpointProgressPercent}";
        public static List<string> IDs = new List<string>() {
            ChapterRoomCount, CheckpointRoomCount, RunChapterProgressNumber,
            RunCheckpointProgressNumber, RunChapterProgressPercent, RunCheckpointProgressPercent
        };

        public LiveProgressStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ChapterRoomCount);
                format = StatManager.MissingPathFormat(format, CheckpointRoomCount);
                format = StatManager.MissingPathFormat(format, RunChapterProgressNumber);
                format = StatManager.MissingPathFormat(format, RunCheckpointProgressNumber);
                format = StatManager.MissingPathFormat(format, RunChapterProgressPercent);
                format = StatManager.MissingPathFormat(format, RunCheckpointProgressPercent);
                return format;
            }

            int chapterRoomCount = chapterPath.RoomCount;
            format = format.Replace(ChapterRoomCount, $"{chapterRoomCount}");


            if (chapterPath.CurrentRoom == null) { //Player is not on path
                format = StatManager.NotOnPathFormat(format, CheckpointRoomCount);
                format = StatManager.NotOnPathFormat(format, RunChapterProgressNumber);
                format = StatManager.NotOnPathFormat(format, RunCheckpointProgressNumber);
                format = StatManager.NotOnPathFormatPercent(format, RunChapterProgressPercent);
                format = StatManager.NotOnPathFormatPercent(format, RunCheckpointProgressPercent);

                return format;
            } else {
                int checkpointRoomCount = chapterPath.CurrentRoom.Checkpoint.Rooms.Count;

                int roomNumberInChapter = chapterPath.CurrentRoom.RoomNumberInChapter;
                int roomNumberInCP = chapterPath.CurrentRoom.RoomNumberInCP;


                format = format.Replace(CheckpointRoomCount, $"{checkpointRoomCount}");
                format = format.Replace(RunChapterProgressNumber, $"{roomNumberInChapter}");
                format = format.Replace(RunCheckpointProgressNumber, $"{roomNumberInCP}");

                int decimalPlaces = ConsistencyTrackerModule.Instance.ModSettings.LiveDataDecimalPlaces;
                double chapterProgressPercent = Math.Round(((double)roomNumberInChapter / chapterRoomCount) * 100, decimalPlaces);
                double checkpointProgressPercent = Math.Round(((double)roomNumberInCP / checkpointRoomCount) * 100, decimalPlaces);

                format = format.Replace(RunChapterProgressPercent, $"{chapterProgressPercent}%");
                format = format.Replace(RunCheckpointProgressPercent, $"{checkpointProgressPercent}%");
            }



            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        //live-progress;Room {room:roomNumberInChapter}/{chapter:roomCount} ({room:chapterProgressPercent})
        // | Room in CP: {room:roomNumberInCheckpoint}/{checkpoint:roomCount} ({room:checkpointProgressPercent})
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(RunChapterProgressNumber, "Number of the room within the entire chapter"),
                new KeyValuePair<string, string>(ChapterRoomCount, "Count of rooms in the chapter"),
                new KeyValuePair<string, string>(RunChapterProgressPercent, "Percent completion of the chapter given the current room"),

                new KeyValuePair<string, string>(RunCheckpointProgressNumber, "Number of the room within the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointRoomCount, "Count of rooms in the current checkpoint"),
                new KeyValuePair<string, string>(RunCheckpointProgressPercent, "Percent completion of the current checkpoint given the current room"),
            };
        }
        public override List<StatFormat> GetStatExamples() {
            return new List<StatFormat>() {
                new StatFormat("live-progress", $"Room {RunChapterProgressNumber}/{ChapterRoomCount} ({RunChapterProgressPercent})" +
                    $" | Room in CP: {RunCheckpointProgressNumber}/{CheckpointRoomCount} ({RunCheckpointProgressPercent})")
            };
        }
    }
}
