using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     
     {player:holdingGolden}
     {mod:trackingPaused}
     {mod:recordingPath}
     {mod:modVersion}
     {mod:overlayVersion}
     
     {room:name} path+
     {room:debugName}
     {room:goldenDeaths}
     {room:goldenDeathsSession}

     {checkpoint:name} path+
     {checkpoint:abbreviation} path+
     {checkpoint:goldenDeaths} path+
     {checkpoint:goldenDeathsSession} path+
     {checkpoint:goldenChance} path+

     {chapter:debugName}
     {chapter:name}
     {chapter:goldenDeaths} path
     {chapter:goldenDeathsSession} path
     {chapter:goldenChance} path
     
     {campaign:name}

     "path" note means a recorded path is required for this stat
     "path+" note additionally means that the player needs to be ON the path for this stat
         */

    public class BasicInfoStat : Stat {

        public static string RoomName = "{room:name}";

        public static string CheckpointName = "{checkpoint:name}";
        public static string CheckpointAbbreviation = "{checkpoint:abbreviation}";
        public static string CheckpointGoldenDeaths = "{checkpoint:goldenDeaths}";
        public static string CheckpointGoldenDeathsSession = "{checkpoint:goldenDeathsSession}";
        public static string CheckpointGoldenChance = "{checkpoint:goldenChance}";

        public static string ChapterGoldenDeaths = "{chapter:goldenDeaths}";
        public static string ChapterGoldenDeathsSession = "{chapter:goldenDeathsSession}";
        public static string ChapterGoldenChance = "{chapter:goldenChance}";
        
        public static string SaveStateRoomName = "{savestate:roomName}";

        public static string PathSegmentName = "{segment:name}";

        public static List<string> IDs = new List<string>() {
            //PlayerHoldingGolden,
            //ModTrackingPaused, ModRecordingPath, ModModVersion, ModOverlayVersion,
            RoomName, /*RoomDebugName, RoomGoldenDeaths, RoomGoldenDeathsSession,*/
            CheckpointName, CheckpointAbbreviation, CheckpointGoldenDeaths, CheckpointGoldenDeathsSession, CheckpointGoldenChance,
            /*ChapterName, ChapterDebugName,*/ ChapterGoldenDeaths, ChapterGoldenDeathsSession, ChapterGoldenChance,
            //CampaignName
            SaveStateRoomName,
            PathSegmentName,
        };

        public BasicInfoStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RoomName);

                format = StatManager.MissingPathFormat(format, CheckpointName);
                format = StatManager.MissingPathFormat(format, CheckpointAbbreviation);
                format = StatManager.MissingPathFormat(format, CheckpointGoldenDeaths);
                format = StatManager.MissingPathFormat(format, CheckpointGoldenDeathsSession);
                format = StatManager.MissingPathFormat(format, CheckpointGoldenChance);

                format = StatManager.MissingPathFormat(format, ChapterGoldenDeaths);
                format = StatManager.MissingPathFormat(format, ChapterGoldenDeathsSession);
                format = StatManager.MissingPathFormat(format, ChapterGoldenChance);

                format = StatManager.MissingPathFormat(format, PathSegmentName);
                return format;
            }
            
            RoomNameDisplayType nameType = StatManager.RoomNameType;

            format = format.Replace(ChapterGoldenDeaths, $"{chapterPath.Stats.GoldenBerryDeaths}");
            format = format.Replace(ChapterGoldenDeathsSession, $"{chapterPath.Stats.GoldenBerryDeathsSession}");
            format = format.Replace(ChapterGoldenChance, $"{StatManager.FormatPercentage(chapterPath.Stats.GoldenChance)}");

            if (chapterPath.CurrentRoom == null) { //Not on path
                format = StatManager.NotOnPathFormat(format, RoomName, "--");

                format = StatManager.NotOnPathFormat(format, CheckpointName);
                format = StatManager.NotOnPathFormat(format, CheckpointAbbreviation);
                format = StatManager.NotOnPathFormat(format, CheckpointGoldenDeaths);
                format = StatManager.NotOnPathFormat(format, CheckpointGoldenDeathsSession);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointGoldenChance);
                
            } else {
                RoomInfo rInfo = chapterPath.CurrentRoom;

                format = format.Replace(RoomName, $"{chapterPath.CurrentRoom.GetFormattedRoomName(nameType)}");

                CheckpointInfo cpInfo = rInfo.Checkpoint;

                format = format.Replace(CheckpointName, $"{cpInfo.Name}");
                format = format.Replace(CheckpointAbbreviation, $"{cpInfo.Abbreviation}");
                format = format.Replace(CheckpointGoldenDeaths, $"{cpInfo.Stats.GoldenBerryDeaths}");
                format = format.Replace(CheckpointGoldenDeathsSession, $"{cpInfo.Stats.GoldenBerryDeathsSession}");
                format = format.Replace(CheckpointGoldenChance, $"{StatManager.FormatPercentage(cpInfo.Stats.GoldenChance)}");
            }

            if (chapterPath.SpeedrunToolSaveStateRoom == null) {
                format = StatManager.NotOnPathFormat(format, SaveStateRoomName, "--");
            } else {
                format = format.Replace(SaveStateRoomName, $"{chapterPath.SpeedrunToolSaveStateRoom.GetFormattedRoomName(nameType)}");
            }

            //Path Segment
            PathSegment segment = chapterPath.SegmentList.CurrentSegment;
            if (segment == null) {
                format = format.Replace(PathSegmentName, $"{StatManager.ValueNotAvailable}");
            } else {
                format = format.Replace(PathSegmentName, $"{segment.Name}");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(BasicPathlessInfo.CampaignName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CAMPAIGN_NAME")),
                new KeyValuePair<string, string>(BasicPathlessInfo.CampaignGamebananaId, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CAMPAIGN_GAMEBANANA_ID")),

                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_NAME")),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterSideName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_SIDE_NAME")),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterUID, $"[{Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_DEV")}] {Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_DEBUG_NAME")}"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterSID, $"[{Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_DEV")}] {Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_SID")}"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterSanitizedSID, $"[{Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_DEV")}] {Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_SANITIZED_SID")}"),
                new KeyValuePair<string, string>(ChapterGoldenDeaths, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_GOLDEN_DEATHS")),
                new KeyValuePair<string, string>(ChapterGoldenDeathsSession, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_GOLDEN_DEATHS_SESSION")),
                new KeyValuePair<string, string>(ChapterGoldenChance, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_GOLDEN_CHANCE")),
                new KeyValuePair<string, string>(LiveProgressStat.ChapterRoomCount, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHAPTER_ROOM_COUNT")),

                new KeyValuePair<string, string>(CheckpointName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHECKPOINT_NAME")),
                new KeyValuePair<string, string>(CheckpointAbbreviation, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHECKPOINT_ABBREVIATION")),
                new KeyValuePair<string, string>(CheckpointGoldenDeaths, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHECKPOINT_GOLDEN_DEATHS")),
                new KeyValuePair<string, string>(CheckpointGoldenDeathsSession, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHECKPOINT_GOLDEN_DEATHS_SESSION")),
                new KeyValuePair<string, string>(CheckpointGoldenChance, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHECKPOINT_GOLDEN_CHANCE")),
                new KeyValuePair<string, string>(LiveProgressStat.CheckpointRoomCount, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_CHECKPOINT_ROOM_COUNT")),

                new KeyValuePair<string, string>(RoomName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_NAME")),
                new KeyValuePair<string, string>(BasicPathlessInfo.RoomDebugName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_DEBUG_NAME")),
                new KeyValuePair<string, string>(BasicPathlessInfo.RoomGoldenDeaths, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_GOLDEN_DEATHS")),
                new KeyValuePair<string, string>(BasicPathlessInfo.RoomGoldenDeathsSession, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_GOLDEN_DEATHS_SESSION")),

                //Live Progress stats
                new KeyValuePair<string, string>(LiveProgressStat.RoomNumberInChapter, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_NUMBER_IN_CHAPTER")),
                new KeyValuePair<string, string>(LiveProgressStat.RoomChapterProgressPercent, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_CHAPTER_PROGRESS_PERCENT")),
                new KeyValuePair<string, string>(LiveProgressStat.RoomNumberInCheckpoint, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_NUMBER_IN_CHECKPOINT")),
                new KeyValuePair<string, string>(LiveProgressStat.RoomCheckpointProgressPercent, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_CHECKPOINT_PROGRESS_PERCENT")),

                new KeyValuePair<string, string>(LiveProgressStat.RoomChapterProgressBar.GetPlaceholder("X"), Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_ROOM_CHAPTER_PROGRESS_BAR_X")),

                new KeyValuePair<string, string>(SaveStateRoomName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_SAVE_STATE_ROOM_NAME")),
                new KeyValuePair<string, string>(LiveProgressStat.SaveStateCheckpointRoomCount, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_SAVE_STATE_CHECKPOINT_ROOM_COUNT")),
                new KeyValuePair<string, string>(LiveProgressStat.SaveStateRoomNumberInChapter, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_SAVE_STATE_ROOM_NUMBER_IN_CHAPTER")),
                new KeyValuePair<string, string>(LiveProgressStat.SaveStateRoomNumberInCheckpoint, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_SAVE_STATE_ROOM_NUMBER_IN_CHECKPOINT")),
                new KeyValuePair<string, string>(LiveProgressStat.SaveStateChapterProgressPercent, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_SAVE_STATE_CHAPTER_PROGRESS_PERCENT")),
                new KeyValuePair<string, string>(LiveProgressStat.SaveStateCheckpointProgressPercent, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_SAVE_STATE_CHECKPOINT_PROGRESS_PERCENT")),
                
                //Mod State
                new KeyValuePair<string, string>(BasicPathlessInfo.PlayerHoldingGolden, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_PLAYER_HOLDING_GOLDEN")),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModTrackingPaused, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_MOD_TRACKING_PAUSED")),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModRecordingPath, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_MOD_RECORDING_PATH")),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModModVersion, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_MOD_MOD_VERSION")),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModOverlayVersion, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_MOD_OVERLAY_VERSION")),

                //Path Segment
                new KeyValuePair<string, string>(PathSegmentName, Dialog.Clean("CCT_STAT_BASIC_INFO_EXPLANATIONS_PATH_SEGMENT_NAME")),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_TITLE_BASIC_INFO"), 
                    $"{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_1")} {BasicPathlessInfo.ChapterName} " +
                    $"({BasicPathlessInfo.ChapterUID})\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_2")} " +
                    $"{BasicPathlessInfo.ChapterSideName}\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_3")} " +
                    $"{BasicPathlessInfo.CampaignName}\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_4")} " +
                    $"{BasicPathlessInfo.ChapterHasPath}\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_5")} " +
                    $"{ChapterGoldenDeaths} ({ChapterGoldenDeathsSession})\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_6")} " +
                    $"{ChapterGoldenChance}\n" +
                    $"\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_7")} {CheckpointName} ({CheckpointAbbreviation})\n" +
                    $"{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_5")} {CheckpointGoldenDeaths} ({CheckpointGoldenDeathsSession})\n" +
                    $"{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_6")} {CheckpointGoldenChance}\n" +
                    $"\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_8")} {RoomName} ({BasicPathlessInfo.RoomDebugName})\n" +
                    $"{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_5")} {BasicPathlessInfo.RoomGoldenDeaths} ({BasicPathlessInfo.RoomGoldenDeathsSession})\n" +
                    $"\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_9")} {BasicPathlessInfo.ModTrackingPaused}\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_10")} " +
                    $"{BasicPathlessInfo.ModRecordingPath}\n{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_11")} " +
                    $"{BasicPathlessInfo.PlayerHoldingGolden} | {Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_12")} " +
                    $"{BasicPathlessInfo.PlayerChapterCompleted} | {Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_13")} {BasicPathlessInfo.PlayerGoldenDone}\n" +
                    $"{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_14")} {BasicPathlessInfo.ModModVersion}\n" +
                    $"{Dialog.Clean("CCT_STAT_BASIC_INFO_FORMAT_CONTENT_BASIC_INFO_15")} {BasicPathlessInfo.ModOverlayVersion}"),
            };
        }
    }
}
