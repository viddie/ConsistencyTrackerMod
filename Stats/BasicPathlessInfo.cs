using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using GameData = Celeste.Mod.ConsistencyTracker.Utility.GameData;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     
     {player:holdingGolden}
     {player:goldenDone}
     {player:chapterCompleted}

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
     {campaign:gamebananaId}

     "path" note means a recorded path is required for this stat
     "path+" note additionally means that the player needs to be ON the path for this stat
         */

    public class BasicPathlessInfo : Stat {
        
        public static string PlayerHoldingGolden = "{player:holdingGolden}";
        public static string PlayerGoldenDone = "{player:goldenDone}";
        public static string PlayerChapterCompleted = "{player:chapterCompleted}";

        public static string ModTrackingPaused = "{mod:trackingPaused}";
        public static string ModRecordingPath = "{mod:recordingPath}";
        public static string ModModVersion = "{mod:modVersion}";
        public static string ModOverlayVersion = "{mod:overlayVersion}";


        //public static string RoomName = "{room:name}";
        public static string RoomDebugName = "{room:debugName}";
        public static string RoomGoldenDeaths = "{room:goldenDeaths}";
        public static string RoomGoldenDeathsSession = "{room:goldenDeathsSession}";

        //public static string CheckpointName = "{checkpoint:name}";
        //public static string CheckpointAbbreviation = "{checkpoint:abbreviation}";
        //public static string CheckpointGoldenDeaths = "{checkpoint:goldenDeaths}";
        //public static string CheckpointGoldenDeathsSession = "{checkpoint:goldenDeathsSession}";
        //public static string CheckpointGoldenChance = "{checkpoint:goldenChance}";

        public static string ChapterName = "{chapter:name}";
        public static string ChapterSideName = "{chapter:sideName}";
        public static string ChapterSID = "{chapter:sid}";
        public static string ChapterSanitizedSID = "{chapter:sidSanitized}";
        public static string ChapterUID = "{chapter:uid}";
        public static string ChapterHasPath = "{chapter:hasPath}";
        //public static string ChapterGoldenDeaths = "{chapter:goldenDeaths}";
        //public static string ChapterGoldenDeathsSession = "{chapter:goldenDeathsSession}";
        //public static string ChapterGoldenChance = "{chapter:goldenChance}";

        public static string CampaignName = "{campaign:name}";
        public static string CampaignGamebananaId = "{campaign:gamebananaId}";

        public static List<string> IDs = new List<string>() {
            PlayerHoldingGolden, PlayerGoldenDone, PlayerChapterCompleted,
            ModTrackingPaused, ModRecordingPath, ModModVersion, ModOverlayVersion, 
            RoomDebugName, RoomGoldenDeaths, RoomGoldenDeathsSession,
            ChapterName, ChapterSideName, ChapterUID, ChapterSID, ChapterSanitizedSID, ChapterHasPath,
            CampaignName, CampaignGamebananaId
        };

        public BasicPathlessInfo() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            format = format.Replace(PlayerHoldingGolden, $"{StatManager.FormatBool(chapterStats.ModState.PlayerIsHoldingGolden)}");
            format = format.Replace(PlayerGoldenDone, $"{StatManager.FormatBool(chapterStats.ModState.GoldenDone)}");
            format = format.Replace(PlayerChapterCompleted, $"{StatManager.FormatBool(chapterStats.ModState.ChapterCompleted)}");

            format = format.Replace(ModTrackingPaused, $"{StatManager.FormatBool(chapterStats.ModState.DeathTrackingPaused)}");
            format = format.Replace(ModRecordingPath, $"{StatManager.FormatBool(chapterStats.ModState.RecordingPath)}");
            format = format.Replace(ModModVersion, $"{chapterStats.ModState.ModVersion}");
            format = format.Replace(ModOverlayVersion, $"{chapterStats.ModState.OverlayVersion}");


            format = format.Replace(RoomDebugName, $"{chapterStats.CurrentRoom.ActualDebugRoomName}");
            format = format.Replace(RoomGoldenDeaths, $"{chapterStats.CurrentRoom.GoldenBerryDeaths}");
            format = format.Replace(RoomGoldenDeathsSession, $"{chapterStats.CurrentRoom.GoldenBerryDeathsSession}");

            format = format.Replace(ChapterUID, $"{chapterStats.ChapterUID}");
            format = format.Replace(ChapterName, $"{chapterStats.ChapterName}");
            format = format.Replace(ChapterSideName, $"{chapterStats.SideName}");
            format = format.Replace(ChapterSID, $"{chapterStats.ChapterSID}");
            format = format.Replace(ChapterSanitizedSID, $"{chapterStats.ChapterSIDDialogSanitized}");
            format = format.Replace(ChapterHasPath, $"{StatManager.FormatBool(chapterPath != null)}");

            format = format.Replace(CampaignName, $"{chapterStats.CampaignName}");
            format = format.Replace(CampaignGamebananaId, $"{GameData.GetModGamebananaId()}");
            

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {

            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                //Add important formats here

                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_SUCCESS_RATE"), $"{BasicInfoStat.RoomName}: {SuccessRateStat.RoomSuccessRate} ({SuccessRateStat.RoomSuccesses}/{SuccessRateStat.RoomAttempts})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_SUCCESS_RATE_1")}: {SuccessRateStat.CheckpointSuccessRate}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_SUCCESS_RATE_2")}: {SuccessRateStat.ChapterSuccessRate}"),

                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_GOLDEN_ATTEMPTS_INFO"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_ATTEMPTS_INFO_1")}: {BasicInfoStat.ChapterGoldenDeaths} ({BasicInfoStat.ChapterGoldenDeathsSession}) [{BasicPathlessInfo.RoomGoldenDeaths} ({BasicPathlessInfo.RoomGoldenDeathsSession})]" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_ATTEMPTS_INFO_2")}: {PersonalBestStat.PBBest} ({PersonalBestStat.PBBestRoomNumber}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_ATTEMPTS_INFO_3")}: {PersonalBestStat.PBBestSession} ({PersonalBestStat.PBBestRoomNumberSession}/{LiveProgressStat.ChapterRoomCount})"),

                
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_VIDDIE_STYLE"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_STYLE_1")}: {BasicInfoStat.RoomName} ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount}) " +
                $"| {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_STYLE_2")}: {SuccessRateStat.RoomSuccessRate} ({SuccessRateStat.RoomSuccesses}/{SuccessRateStat.RoomAttempts})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_STYLE_3")}: {ChokeRateStat.RoomChokeRate} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_STYLE_4")}: {StreakStat.RoomCurrentStreak}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_STYLE_5")}: {ListCheckpointDeathsStat.ListCheckpointDeathsIndicator}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_STYLE_6")}" +
                $"\\n({AverageLastRunsStat.ChapterAverageRunDistanceSession} | {{chapter:averageRunDistanceSession#10}} | {AverageLastRunsStat.ChapterHighestAverageOver10Runs}) " +
                $"/ {LiveProgressStat.ChapterRoomCount}"),
                
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_VIDDIE_ALT_STYLE"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_ALT_STYLE_1")}: {BasicInfoStat.RoomName} ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount}) " +
                $"| {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_ALT_STYLE_2")}: {StreakStat.RoomCurrentStreak} ({StreakStat.RoomCurrentStreakBest}) " +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_ALT_STYLE_3")}: {ChokeRateStat.RoomGoldenSuccessRate} [{ChokeRateStat.RoomGoldenSuccesses}/{ChokeRateStat.RoomGoldenEntries}] ({ChokeRateStat.RoomGoldenSuccessRateSession})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_ALT_STYLE_4")}: {ChokeRateStat.RoomGoldenEntryChance} ({ChokeRateStat.RoomGoldenEntryChanceSession})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_VIDDIE_ALT_STYLE_5")}: #{CurrentRunPbStat.RunCurrentPbStatus} (#{CurrentRunPbStat.RunCurrentPbStatusSession})"),

                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_PARROT_STYLE"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_PARROT_STYLE_1")}: {BasicInfoStat.RoomName} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_PARROT_STYLE_2")}: ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_PARROT_STYLE_3")}: #{CurrentRunPbStat.RunCurrentPbStatus} ({Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_PARROT_STYLE_4")} {CurrentRunPbStat.RunTopXPercent})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_PARROT_STYLE_5")}: {PersonalBestStat.PBBest} ({PersonalBestStat.PBBestRoomNumber}/{LiveProgressStat.ChapterRoomCount}) | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_PARROT_STYLE_6")}: {PersonalBestStat.PBBestSession} ({PersonalBestStat.PBBestRoomNumberSession}/{LiveProgressStat.ChapterRoomCount})"),

                
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_GOLDEN_RUN_INFO"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_RUN_INFO_1")}: #{CurrentRunPbStat.RunCurrentPbStatus} ({Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_RUN_INFO_2")}: #{CurrentRunPbStat.RunCurrentPbStatusSession})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_RUN_INFO_3")}: {ChokeRateStat.RoomChokeRate} ({Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_RUN_INFO_4")}: {ChokeRateStat.CheckpointChokeRate})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_RUN_INFO_5")}: {BasicInfoStat.CheckpointGoldenChance}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_GOLDEN_RUN_INFO_6")}: {BasicInfoStat.ChapterGoldenChance}"),

                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_SAVESTATE_RUN"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_SAVESTATE_RUN_1")}: {BasicInfoStat.SaveStateRoomName} ({LiveProgressStat.SaveStateRoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount}) -> {BasicInfoStat.RoomName} ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\n'{BasicInfoStat.RoomName}' {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_SAVESTATE_RUN_2")}: {StreakStat.RoomCurrentStreak}"),


                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_ROOM_INFO"), $"{BasicInfoStat.CheckpointName} | {BasicInfoStat.RoomName} ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_ROOM_INFO_1")}: {SuccessRateStat.RoomSuccessRate} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_ROOM_INFO_2")}: {ChokeRateStat.RoomChokeRate} ({ChokeRateStat.RoomChokeRateSession})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_ROOM_INFO_3")}: {BasicPathlessInfo.RoomGoldenDeaths} ({BasicPathlessInfo.RoomGoldenDeathsSession}) | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_ROOM_INFO_4")}: {RunGoldenChanceStat.RunGoldenChanceFromStart}"),

                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_CHECKPOINT_INFO"), $"{BasicInfoStat.CheckpointName} ({BasicInfoStat.CheckpointAbbreviation}) | {LiveProgressStat.CheckpointRoomCount} {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_1")}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_2")}: {SuccessRateStat.CheckpointSuccessRate} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_3")}: {ChokeRateStat.CheckpointChokeRate} ({ChokeRateStat.CheckpointChokeRateSession}) | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_4")}: {BasicInfoStat.CheckpointGoldenDeaths} ({BasicInfoStat.CheckpointGoldenDeathsSession})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_5")}: {SuccessRateColorsStat.CheckpointColorLightGreen} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_6")}: {SuccessRateColorsStat.CheckpointColorGreen}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_7")}: {SuccessRateColorsStat.CheckpointColorYellow} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHECKPOINT_INFO_8")}: {SuccessRateColorsStat.CheckpointColorRed}"),
               
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_FULL_CHAPTER_INFO"), $"{BasicPathlessInfo.ChapterName} ({BasicPathlessInfo.ChapterSideName})" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHAPTER_INFO_1")}: {BasicInfoStat.ChapterGoldenDeaths} ({BasicInfoStat.ChapterGoldenDeathsSession}) | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHAPTER_INFO_2")}: {BasicInfoStat.ChapterGoldenChance}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHAPTER_INFO_3")}: {SuccessRateColorsStat.ChapterColorLightGreen} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHAPTER_INFO_4")}: {SuccessRateColorsStat.ChapterColorGreen}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHAPTER_INFO_5")}: {SuccessRateColorsStat.ChapterColorYellow} | {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHAPTER_INFO_6")}: {SuccessRateColorsStat.ChapterColorRed}" +
                $"\\n{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_FULL_CHAPTER_INFO_7")}: {SuccessRateColorsStat.ChapterListColorRed}"),


                
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_BASIC_CURRENT_MAP"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_BASIC_CURRENT_MAP")}: {ChapterName} {ChapterSideName}"),
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_BASIC_CURRENT_MAP_NO_SIDE"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_BASIC_CURRENT_MAP_NO_SIDE")}: {ChapterName}"),
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_BASIC_CURRENT_MAP_CAMPAIGN"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_BASIC_CURRENT_MAP_CAMPAIGN_1")}: '{ChapterName}' {Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_BASIC_CURRENT_MAP_CAMPAIGN_2")} '{CampaignName}'"),
                
                new StatFormat(Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_TITLE_BASIC_SAVESTATE_RUN_GD_STYLE"), $"{Dialog.Clean("CCT_STAT_BASIC_PATHLESS_INFO_FORMAT_CONTENT_BASIC_SAVESTATE_RUN_GD_STYLE")}: {LiveProgressStat.SaveStateChapterProgressPercent} -> {LiveProgressStat.RoomChapterProgressPercent}"),
            };
        }
    }
}
