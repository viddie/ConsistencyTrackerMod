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

    public class BasicPathlessInfo : Stat {

        public static string PlayerHoldingGolden = "{player:holdingGolden}";
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
        public static string ChapterSID = "{chapter:sid}";
        public static string ChapterSanitizedSID = "{chapter:sidSanitized}";
        public static string ChapterDebugName = "{chapter:debugName}";
        //public static string ChapterGoldenDeaths = "{chapter:goldenDeaths}";
        //public static string ChapterGoldenDeathsSession = "{chapter:goldenDeathsSession}";
        //public static string ChapterGoldenChance = "{chapter:goldenChance}";

        public static string CampaignName = "{campaign:name}";

        public static List<string> IDs = new List<string>() {
            PlayerHoldingGolden,
            ModTrackingPaused, ModRecordingPath, ModModVersion, ModOverlayVersion,
            RoomDebugName, RoomGoldenDeaths, RoomGoldenDeathsSession,
            ChapterName, ChapterDebugName,
            CampaignName
        };

        public BasicPathlessInfo() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            format = format.Replace(PlayerHoldingGolden, $"{StatManager.FormatBool(chapterStats.ModState.PlayerIsHoldingGolden)}");
            format = format.Replace(ModTrackingPaused, $"{StatManager.FormatBool(chapterStats.ModState.DeathTrackingPaused)}");
            format = format.Replace(ModRecordingPath, $"{StatManager.FormatBool(chapterStats.ModState.RecordingPath)}");
            format = format.Replace(ModModVersion, $"{chapterStats.ModState.ModVersion}");
            format = format.Replace(ModOverlayVersion, $"{chapterStats.ModState.OverlayVersion}");


            format = format.Replace(RoomDebugName, $"{chapterStats.CurrentRoom.DebugRoomName}");
            format = format.Replace(RoomGoldenDeaths, $"{chapterStats.CurrentRoom.GoldenBerryDeaths}");
            format = format.Replace(RoomGoldenDeathsSession, $"{chapterStats.CurrentRoom.GoldenBerryDeathsSession}");

            format = format.Replace(ChapterDebugName, $"{chapterStats.ChapterDebugName}");
            format = format.Replace(ChapterName, $"{chapterStats.ChapterName}");
            format = format.Replace(CampaignName, $"{chapterStats.CampaignName}");
            format = format.Replace(ChapterSID, $"{chapterStats.ChapterSID}");
            format = format.Replace(ChapterSanitizedSID, $"{chapterStats.ChapterSIDDialogSanitized}");

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {

            };
        }
        public override List<StatFormat> GetStatExamples() {
            return new List<StatFormat>() {
                
            };
        }
    }
}
