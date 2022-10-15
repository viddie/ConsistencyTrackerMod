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
     {run:chapterProgressNumber} - On what room number out of the entire chapter is the player rn
     {run:checkpointProgressNumber} - On what room number out of the current checkpoint is the player rn
     
     {run:chapterProgressPercent} - ({run:chapterProgressNumber} - {chapter:roomCount})%
     {run:checkpointProgressPercent} - ({run:checkpointProgressNumber} - {checkpoint:roomCount})%

         */

    public class LiveProgressStat : Stat {

        public static string ChapterRoomCount = "{chapter:roomCount}";
        public static string CheckpointRoomCount = "{checkpoint:roomCount}";
        public static string RunChapterProgressNumber = "{run:chapterProgressNumber}";
        public static string RunCheckpointProgressNumber = "{run:checkpointProgressNumber}";
        public static string RunChapterProgressPercent = "{run:chapterProgressPercent}";
        public static string RunCheckpointProgressPercent = "{run:checkpointProgressPercent}";
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
                format = format.Replace(CheckpointRoomCount, $"-");
                format = format.Replace(RunChapterProgressNumber, $"-");
                format = format.Replace(RunCheckpointProgressNumber, $"-");
                format = format.Replace(RunChapterProgressPercent, $"-%");
                format = format.Replace(RunCheckpointProgressPercent, $"-%");

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
    }
}
