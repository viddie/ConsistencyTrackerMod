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

        public static List<string> IDs = new List<string>() {
            //PlayerHoldingGolden,
            //ModTrackingPaused, ModRecordingPath, ModModVersion, ModOverlayVersion,
            RoomName, /*RoomDebugName, RoomGoldenDeaths, RoomGoldenDeathsSession,*/
            CheckpointName, CheckpointAbbreviation, CheckpointGoldenDeaths, CheckpointGoldenDeathsSession, CheckpointGoldenChance,
            /*ChapterName, ChapterDebugName,*/ ChapterGoldenDeaths, ChapterGoldenDeathsSession, ChapterGoldenChance,
            //CampaignName
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


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(BasicPathlessInfo.CampaignName, "Name of the campaign"),

                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterName, "Name of the chapter"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterSideName, "Name of the side of the chapter (A/B/C-Side"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterDebugName, "[DEV] Debug name of the chapter"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterSID, "[DEV] SID of chapter"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ChapterSanitizedSID, "[DEV] Dialog sanitized SID of chapter"),
                new KeyValuePair<string, string>(ChapterGoldenDeaths, "Golden Deaths in the chapter"),
                new KeyValuePair<string, string>(ChapterGoldenDeathsSession, "Golden Deaths in the chapter in the current session"),
                new KeyValuePair<string, string>(ChapterGoldenChance, "Golden Chance of the chapter"),

                new KeyValuePair<string, string>(CheckpointName, "Name of the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointAbbreviation, "Abbreviation of the current checkpoint's name"),
                new KeyValuePair<string, string>(CheckpointGoldenDeaths, "Golden Deaths in the current checkpoint"),
                new KeyValuePair<string, string>(CheckpointGoldenDeathsSession, "Golden Deaths in the current checkpoint in the current session"),
                new KeyValuePair<string, string>(CheckpointGoldenChance, "Golden Chance of the current checkpoint"),

                new KeyValuePair<string, string>(RoomName, "Name of the room. Display format can be changed via Mod Options -> Consistency Tracker -> Live Data -> Room Name Format"),
                new KeyValuePair<string, string>(BasicPathlessInfo.RoomDebugName, "Debug name of the current room"),
                new KeyValuePair<string, string>(BasicPathlessInfo.RoomGoldenDeaths, "Golden Deaths in the current room"),
                new KeyValuePair<string, string>(BasicPathlessInfo.RoomGoldenDeathsSession, "Golden Deaths in the current room in the current session"),

                new KeyValuePair<string, string>(BasicPathlessInfo.PlayerHoldingGolden, "Whether the player is holding a golden berry"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModTrackingPaused, "Whether death tracking is currently paused"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModRecordingPath, "Whether the path is currently being recorded"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModModVersion, "Current version of the mod"),
                new KeyValuePair<string, string>(BasicPathlessInfo.ModOverlayVersion, "Most recent version of the overlay"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat("basic-info", $"--- Chapter ---\nName: {BasicPathlessInfo.ChapterName} ({BasicPathlessInfo.ChapterDebugName})\nSide: {BasicPathlessInfo.ChapterSideName}\nCampaign Name: {BasicPathlessInfo.CampaignName}\nPath available: {BasicPathlessInfo.ChapterHasPath}\nGolden Deaths: {ChapterGoldenDeaths} ({ChapterGoldenDeathsSession})\nGolden Chance: {ChapterGoldenChance}\n" +
                $"\n--- Checkpoint ---\nName: {CheckpointName} ({CheckpointAbbreviation})\nGolden Deaths: {CheckpointGoldenDeaths} ({CheckpointGoldenDeathsSession})\nGolden Chance: {CheckpointGoldenChance}\n" +
                $"\n--- Room ---\nName: {RoomName} ({BasicPathlessInfo.RoomDebugName})\nGolden Deaths: {BasicPathlessInfo.RoomGoldenDeaths} ({BasicPathlessInfo.RoomGoldenDeathsSession})\n" +
                $"\n--- Mod State ---\nTracking Paused: {BasicPathlessInfo.ModTrackingPaused}\nRecording Path: {BasicPathlessInfo.ModRecordingPath}\nPlayer Holding Golden: {BasicPathlessInfo.PlayerHoldingGolden} | Chapter completed: {BasicPathlessInfo.PlayerChapterCompleted} | Golden done: {BasicPathlessInfo.PlayerGoldenDone}\nMod Version: {BasicPathlessInfo.ModModVersion}\nOverlay Version: {BasicPathlessInfo.ModOverlayVersion}"),
            };
        }
    }
}
