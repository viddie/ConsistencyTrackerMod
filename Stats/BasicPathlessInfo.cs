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
        public static string ChapterDebugName = "{chapter:debugName}";
        public static string ChapterHasPath = "{chapter:hasPath}";
        //public static string ChapterGoldenDeaths = "{chapter:goldenDeaths}";
        //public static string ChapterGoldenDeathsSession = "{chapter:goldenDeathsSession}";
        //public static string ChapterGoldenChance = "{chapter:goldenChance}";

        public static string CampaignName = "{campaign:name}";

        public static List<string> IDs = new List<string>() {
            PlayerHoldingGolden, PlayerGoldenDone, PlayerChapterCompleted,
            ModTrackingPaused, ModRecordingPath, ModModVersion, ModOverlayVersion,
            RoomDebugName, RoomGoldenDeaths, RoomGoldenDeathsSession,
            ChapterName, ChapterSideName, ChapterDebugName, ChapterSID, ChapterSanitizedSID, ChapterHasPath,
            CampaignName
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


            format = format.Replace(RoomDebugName, $"{chapterStats.CurrentRoom.DebugRoomName}");
            format = format.Replace(RoomGoldenDeaths, $"{chapterStats.CurrentRoom.GoldenBerryDeaths}");
            format = format.Replace(RoomGoldenDeathsSession, $"{chapterStats.CurrentRoom.GoldenBerryDeathsSession}");

            format = format.Replace(ChapterDebugName, $"{chapterStats.ChapterDebugName}");
            format = format.Replace(ChapterName, $"{chapterStats.ChapterName}");
            format = format.Replace(ChapterSideName, $"{chapterStats.SideName}");
            format = format.Replace(CampaignName, $"{chapterStats.CampaignName}");
            format = format.Replace(ChapterSID, $"{chapterStats.ChapterSID}");
            format = format.Replace(ChapterSanitizedSID, $"{chapterStats.ChapterSIDDialogSanitized}");
            format = format.Replace(ChapterHasPath, $"{StatManager.FormatBool(chapterPath != null)}");

            

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

                new StatFormat("full-success-rate", $"{BasicInfoStat.RoomName}: {SuccessRateStat.RoomSuccessRate} ({SuccessRateStat.RoomSuccesses}/{SuccessRateStat.RoomAttempts})" +
                $"\\nCP: {SuccessRateStat.CheckpointSuccessRate}" +
                $"\\nTotal: {SuccessRateStat.ChapterSuccessRate}"),

                new StatFormat("full-golden-attempts-info", $"Attempts: {BasicInfoStat.ChapterGoldenDeaths} ({BasicInfoStat.ChapterGoldenDeathsSession}) [{BasicPathlessInfo.RoomGoldenDeaths} ({BasicPathlessInfo.RoomGoldenDeathsSession})]" +
                $"\\nPB: {PersonalBestStat.PBBest} ({PersonalBestStat.PBBestRoomNumber}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\nSession PB: {PersonalBestStat.PBBestSession} ({PersonalBestStat.PBBestRoomNumberSession}/{LiveProgressStat.ChapterRoomCount})"),

                
                new StatFormat("full-viddie-style", $"Current: {BasicInfoStat.RoomName} ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount}) " +
                $"| SR: {SuccessRateStat.RoomSuccessRate} ({SuccessRateStat.RoomSuccesses}/{SuccessRateStat.RoomAttempts})" +
                $"\\nRoom Choke Rate: {ChokeRateStat.RoomChokeRate} | Streak: {StreakStat.RoomCurrentStreak}" +
                $"\\nLow Death: {ListCheckpointDeathsStat.ListCheckpointDeathsIndicator}" +
                $"\\nSession avg | Last 10 avg | Best 10 avg:" +
                $"\\n({AverageLastRunsStat.ChapterAverageRunDistanceSession} | {{chapter:averageRunDistanceSession#10}} | {AverageLastRunsStat.ChapterHighestAverageOver10Runs}) " +
                $"/ {LiveProgressStat.ChapterRoomCount}"),

                new StatFormat("full-parrot-style", $"Current: {BasicInfoStat.RoomName} | Room: ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\nCurrent run: #{CurrentRunPbStat.RunCurrentPbStatus} (Top {CurrentRunPbStat.RunTopXPercent})" +
                $"\\nPB: {PersonalBestStat.PBBest} ({PersonalBestStat.PBBestRoomNumber}/{LiveProgressStat.ChapterRoomCount}) | Session: {PersonalBestStat.PBBestSession} ({PersonalBestStat.PBBestRoomNumberSession}/{LiveProgressStat.ChapterRoomCount})"),

                
                new StatFormat("full-golden-run-info", $"Current Run: #{CurrentRunPbStat.RunCurrentPbStatus} (Session: #{CurrentRunPbStat.RunCurrentPbStatusSession})" +
                $"\\nChoke Rate: {ChokeRateStat.RoomChokeRate} (CP: {ChokeRateStat.CheckpointChokeRate})" +
                $"\\nCP Golden Chance: {BasicInfoStat.CheckpointGoldenChance}" +
                $"\\nChapter Golden Chance: {BasicInfoStat.ChapterGoldenChance}"),

                new StatFormat("full-savestate-run", $"This run: {BasicInfoStat.SaveStateRoomName} ({LiveProgressStat.SaveStateRoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount}) -> {BasicInfoStat.RoomName} ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\n'{BasicInfoStat.RoomName}' Streak: {StreakStat.RoomCurrentStreak}"),


                new StatFormat("full-room-info", $"{BasicInfoStat.CheckpointName} | {BasicInfoStat.RoomName} ({LiveProgressStat.RoomNumberInChapter}/{LiveProgressStat.ChapterRoomCount})" +
                $"\\nSuccess Rate: {SuccessRateStat.RoomSuccessRate} | Choke Rate: {ChokeRateStat.RoomChokeRate} ({ChokeRateStat.RoomChokeRateSession})" +
                $"\\nGolden Deaths: {BasicPathlessInfo.RoomGoldenDeaths} ({BasicPathlessInfo.RoomGoldenDeathsSession}) | Rarity: {RunGoldenChanceStat.RunGoldenChanceFromStart}"),

                new StatFormat("full-checkpoint-info", $"{BasicInfoStat.CheckpointName} ({BasicInfoStat.CheckpointAbbreviation}) | {LiveProgressStat.CheckpointRoomCount} Rooms" +
                $"\\nSuccess Rate: {SuccessRateStat.CheckpointSuccessRate} | Choke Rate: {ChokeRateStat.CheckpointChokeRate} ({ChokeRateStat.CheckpointChokeRateSession}) | Golden Deaths: {BasicInfoStat.CheckpointGoldenDeaths} ({BasicInfoStat.CheckpointGoldenDeathsSession})" +
                $"\\nLight greens (95%+ rooms): {SuccessRateColorsStat.CheckpointColorLightGreen} | Greens (80-95% rooms): {SuccessRateColorsStat.CheckpointColorGreen}" +
                $"\\nYellows (50-80% rooms): {SuccessRateColorsStat.CheckpointColorYellow} | Reds (<50% rooms): {SuccessRateColorsStat.CheckpointColorRed}"),
               
                new StatFormat("full-chapter-info", $"{BasicPathlessInfo.ChapterName} ({BasicPathlessInfo.ChapterSideName})" +
                $"\\nGolden Deaths: {BasicInfoStat.ChapterGoldenDeaths} ({BasicInfoStat.ChapterGoldenDeathsSession}) | Golden Chance: {BasicInfoStat.ChapterGoldenChance}" +
                $"\\nLight greens (95%+ rooms): {SuccessRateColorsStat.ChapterColorLightGreen} | Greens (80-95% rooms): {SuccessRateColorsStat.ChapterColorGreen}" +
                $"\\nYellows (50-80% rooms): {SuccessRateColorsStat.ChapterColorYellow} | Reds (<50% rooms): {SuccessRateColorsStat.ChapterColorRed}" +
                $"\\nChapter reds: {SuccessRateColorsStat.ChapterListColorRed}"),


                
                new StatFormat("basic-current-map", $"Map: {ChapterName} {ChapterSideName}"),
                new StatFormat("basic-current-map-no-side", $"Map: {ChapterName}"),
                new StatFormat("basic-current-map-campaign", $"Map: '{ChapterName}' from '{CampaignName}'"),
                
                new StatFormat("basic-savestate-run-gd-style", $"This run: {LiveProgressStat.SaveStateChapterProgressPercent} -> {LiveProgressStat.RoomChapterProgressPercent}"),
            };
        }
    }
}
