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
     {chapter:goldenDeaths} path
     {chapter:goldenDeathsSession} path
     {chapter:goldenChance} path

     "path" note means a recorded path is required for this stat
     "path+" note additionally means that the player needs to be ON the path for this stat
         */

    public class BasicInfoStat : Stat {

        public static string PlayerHoldingGolden = "{player:holdingGolden}";
        public static string ModTrackingPaused = "{mod:trackingPaused}";
        public static string ModRecordingPath = "{mod:recordingPath}";
        public static string ModModVersion = "{mod:modVersion}";
        public static string ModOverlayVersion = "{mod:overlayVersion}";


        public static string RoomName = "{room:name}";
        public static string RoomDebugName = "{room:debugName}";
        public static string RoomGoldenDeaths = "{room:goldenDeaths}";
        public static string RoomGoldenDeathsSession = "{room:goldenDeathsSession}";

        public static string CheckpointName = "{checkpoint:name}";
        public static string CheckpointAbbreviation = "{checkpoint:abbreviation}";
        public static string CheckpointGoldenDeaths = "{checkpoint:goldenDeaths}";
        public static string CheckpointGoldenDeathsSession = "{checkpoint:goldenDeathsSession}";
        public static string CheckpointGoldenChance = "{checkpoint:goldenChance}";

        public static string ChapterDebugName = "{chapter:debugName}";
        public static string ChapterGoldenDeaths = "{chapter:goldenDeaths}";
        public static string ChapterGoldenDeathsSession = "{chapter:goldenDeathsSession}";
        public static string ChapterGoldenChance = "{chapter:goldenChance}";

        public static List<string> IDs = new List<string>() {
            PlayerHoldingGolden,
            ModTrackingPaused, ModRecordingPath, ModModVersion, ModOverlayVersion,
            RoomName, RoomDebugName, RoomGoldenDeaths, RoomGoldenDeathsSession,
            CheckpointName, CheckpointAbbreviation, CheckpointGoldenDeaths, CheckpointGoldenDeathsSession, CheckpointGoldenChance,
            ChapterDebugName, ChapterGoldenDeaths, ChapterGoldenDeathsSession, ChapterGoldenChance
        };

        public BasicInfoStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            format = format.Replace(PlayerHoldingGolden, $"{StatManager.FormatBool(chapterStats.ModState.PlayerIsHoldingGolden)}");
            format = format.Replace(ModTrackingPaused, $"{StatManager.FormatBool(chapterStats.ModState.DeathTrackingPaused)}");
            format = format.Replace(ModRecordingPath, $"{StatManager.FormatBool(chapterStats.ModState.RecordingPath)}");
            format = format.Replace(ModModVersion, $"{chapterStats.ModState.ModVersion}");
            format = format.Replace(ModOverlayVersion, $"{chapterStats.ModState.OverlayVersion}");


            format = format.Replace(RoomDebugName, $"{chapterStats.CurrentRoom.DebugRoomName}");
            format = format.Replace(RoomGoldenDeaths, $"{chapterStats.CurrentRoom.GoldenBerryDeaths}");
            format = format.Replace(RoomGoldenDeathsSession, $"{chapterStats.CurrentRoom.GoldenBerryDeathsSession}");

            format = format.Replace(ChapterDebugName, $"{chapterStats.ChapterName}");

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
                return format;
            }

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
                return format;
            }

            RoomInfo rInfo = chapterPath.CurrentRoom;

            RoomNameDisplayType nameType = StatManager.RoomNameType;
            format = format.Replace(RoomName, $"{chapterPath.CurrentRoom.GetFormattedRoomName(nameType)}");

            CheckpointInfo cpInfo = rInfo.Checkpoint;

            format = format.Replace(CheckpointName, $"{cpInfo.Name}");
            format = format.Replace(CheckpointAbbreviation, $"{cpInfo.Abbreviation}");
            format = format.Replace(CheckpointGoldenDeaths, $"{cpInfo.Stats.GoldenBerryDeaths}");
            format = format.Replace(CheckpointGoldenDeathsSession, $"{cpInfo.Stats.GoldenBerryDeathsSession}");
            format = format.Replace(CheckpointGoldenChance, $"{StatManager.FormatPercentage(cpInfo.Stats.GoldenChance)}");

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }
    }
}
