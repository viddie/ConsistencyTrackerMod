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
        public static string ChapterAllRoomsCount = "{chapter:allRoomsCount}";

        public static string CheckpointRoomCount = "{checkpoint:roomCount}";
        public static string CheckpointAllRoomsCount = "{checkpoint:allRoomsCount}";
        public static string RoomNumberInChapter = "{room:roomNumberInChapter}";
        public static string RoomNumberInCheckpoint = "{room:roomNumberInCheckpoint}";
        public static string RoomChapterProgressPercent = "{room:chapterProgressPercent}";
        public static string RoomCheckpointProgressPercent = "{room:checkpointProgressPercent}";

        public static string SaveStateCheckpointRoomCount = "{savestate:checkpointRoomCount}";
        public static string SaveStateRoomNumberInChapter = "{savestate:roomNumberInChapter}";
        public static string SaveStateRoomNumberInCheckpoint = "{savestate:roomNumberInCheckpoint}";
        public static string SaveStateChapterProgressPercent = "{savestate:chapterProgressPercent}";
        public static string SaveStateCheckpointProgressPercent = "{savestate:checkpointProgressPercent}";

        public static List<string> IDs = new List<string>() {
            ChapterRoomCount,
            
            CheckpointRoomCount,
            RoomNumberInChapter, RoomNumberInCheckpoint,
            RoomChapterProgressPercent, RoomCheckpointProgressPercent,
            
            SaveStateCheckpointRoomCount,
            SaveStateRoomNumberInChapter, SaveStateRoomNumberInCheckpoint,
            SaveStateChapterProgressPercent, SaveStateCheckpointProgressPercent
        };

        public LiveProgressStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ChapterRoomCount);
                format = StatManager.MissingPathFormat(format, ChapterAllRoomsCount);

                format = StatManager.MissingPathFormat(format, CheckpointRoomCount);
                format = StatManager.MissingPathFormat(format, CheckpointAllRoomsCount);
                format = StatManager.MissingPathFormat(format, RoomNumberInChapter);
                format = StatManager.MissingPathFormat(format, RoomNumberInCheckpoint);
                format = StatManager.MissingPathFormat(format, RoomChapterProgressPercent);
                format = StatManager.MissingPathFormat(format, RoomCheckpointProgressPercent);

                format = StatManager.MissingPathFormat(format, SaveStateCheckpointRoomCount);
                format = StatManager.MissingPathFormat(format, SaveStateRoomNumberInChapter);
                format = StatManager.MissingPathFormat(format, SaveStateRoomNumberInCheckpoint);
                format = StatManager.MissingPathFormat(format, SaveStateChapterProgressPercent);
                format = StatManager.MissingPathFormat(format, SaveStateCheckpointProgressPercent);
                return format;
            }
            
            int decimalPlaces = StatManager.DecimalPlaces;

            int chapterRoomCount = chapterPath.GameplayRoomCount;
            format = format.Replace(ChapterRoomCount, $"{chapterRoomCount}");
            format = format.Replace(ChapterAllRoomsCount, $"{chapterPath.RoomCount}");


            if (chapterPath.CurrentRoom == null) { //Player is not on path
                format = StatManager.NotOnPathFormat(format, CheckpointRoomCount);
                format = StatManager.NotOnPathFormat(format, CheckpointAllRoomsCount);

                format = StatManager.NotOnPathFormat(format, RoomNumberInChapter);
                format = StatManager.NotOnPathFormat(format, RoomNumberInCheckpoint);
                format = StatManager.NotOnPathFormatPercent(format, RoomChapterProgressPercent);
                format = StatManager.NotOnPathFormatPercent(format, RoomCheckpointProgressPercent);
                
            } else {
                RoomInfo rInfo = chapterPath.CurrentRoom;

                int checkpointRoomCount = rInfo.Checkpoint.GameplayRooms.Count;
                int checkpointAllRoomsCount = rInfo.Checkpoint.Rooms.Count;
                
                format = format.Replace(CheckpointRoomCount, $"{checkpointRoomCount}");
                format = format.Replace(CheckpointAllRoomsCount, $"{checkpointAllRoomsCount}");

                int roomNumberInChapter = rInfo.RoomNumberInChapter;
                int roomNumberInCP = rInfo.RoomNumberInCP;

                if (rInfo.IsNonGameplayRoom) {
                    //Use the data from next room, or 100% if no next room in CP
                    RoomInfo nextRoom = rInfo.NextGameplayRoomInChapter;
                    if (nextRoom == null) {
                        nextRoom = rInfo.PreviousGameplayRoomInChapter;
                        if (nextRoom == null)
                            nextRoom = rInfo;
                    }
                    
                    roomNumberInChapter = nextRoom.RoomNumberInChapter;
                    roomNumberInCP = nextRoom.RoomNumberInCP;
                }

                format = format.Replace(RoomNumberInChapter, $"{roomNumberInChapter}");
                format = format.Replace(RoomNumberInCheckpoint, $"{roomNumberInCP}");

                double chapterProgressPercent = Math.Round(((double)roomNumberInChapter / chapterRoomCount) * 100, decimalPlaces);
                double checkpointProgressPercent = Math.Round(((double)roomNumberInCP / checkpointRoomCount) * 100, decimalPlaces);

                format = format.Replace(RoomChapterProgressPercent, $"{chapterProgressPercent}%");
                format = format.Replace(RoomCheckpointProgressPercent, $"{checkpointProgressPercent}%");
            }

            //Save State
            if (chapterPath.SpeedrunToolSaveStateRoom == null) {
                format = StatManager.NotOnPathFormat(format, SaveStateRoomNumberInChapter);
                format = StatManager.NotOnPathFormat(format, SaveStateRoomNumberInCheckpoint);
                format = StatManager.NotOnPathFormatPercent(format, SaveStateChapterProgressPercent);
                format = StatManager.NotOnPathFormatPercent(format, SaveStateCheckpointProgressPercent);
                
            } else { 
                int ssCpRoomCount = chapterPath.SpeedrunToolSaveStateRoom.Checkpoint.Rooms.Count;
                int ssNumberInChapter = chapterPath.SpeedrunToolSaveStateRoom.RoomNumberInChapter;
                int ssNumberInCp = chapterPath.SpeedrunToolSaveStateRoom.RoomNumberInCP;

                format = format.Replace(SaveStateCheckpointRoomCount, $"{ssCpRoomCount}");
                format = format.Replace(SaveStateRoomNumberInChapter, $"{ssNumberInChapter}");
                format = format.Replace(SaveStateRoomNumberInCheckpoint, $"{ssNumberInCp}");

                double chapterProgressPercent = Math.Round(((double)ssNumberInChapter / chapterRoomCount) * 100, decimalPlaces);
                double checkpointProgressPercent = Math.Round(((double)ssNumberInCp / ssCpRoomCount) * 100, decimalPlaces);

                format = format.Replace(SaveStateChapterProgressPercent, $"{chapterProgressPercent}%");
                format = format.Replace(SaveStateCheckpointProgressPercent, $"{checkpointProgressPercent}%");
            }


            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                //Moved to BasicInfoStat
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat("basic-chapter-progress", $"Room {RoomNumberInChapter}/{ChapterRoomCount} ({RoomChapterProgressPercent})" +
                    $" | Room in CP: {RoomNumberInCheckpoint}/{CheckpointRoomCount} ({RoomCheckpointProgressPercent})")
            };
        }
    }
}
