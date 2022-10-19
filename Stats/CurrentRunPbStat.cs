using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     Stats to implement:
     {run:currentPbStatus}          - Format e.g. "Current run: 75. best run", "Current run: 4. best run", "Current run: PB"
     {run:currentPbStatusSession}          - Format e.g. "Current run: 75. best run", "Current run: 4. best run", "Current run: PB"
     {run:currentPbStatusPercent}   - Format e.g. "Current run better than 0% of all runs", "Current run better than 72.39% of all runs", "Current run better than 100% of all runs"
     {run:currentPbStatusPercentSession}   - Format e.g. "Current run better than 0% of all runs", "Current run better than 72.39% of all runs", "Current run better than 100% of all runs"

         */

    public class CurrentRunPbStat : Stat {

        public static string RunCurrentPbStatus = "{run:currentPbStatus}";
        public static string RunCurrentPbStatusSession = "{run:currentPbStatusSession}";
        public static string RunCurrentPbStatusPercent = "{run:currentPbStatusPercent}";
        public static string RunCurrentPbStatusPercentSession = "{run:currentPbStatusPercentSession}";
        public static List<string> IDs = new List<string>() { RunCurrentPbStatus, RunCurrentPbStatusSession, RunCurrentPbStatusPercent, RunCurrentPbStatusPercentSession };


        public CurrentRunPbStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatus);
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatusSession);
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatusPercent);
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatusPercentSession);
                return format;
            }

            if (!chapterStats.ModState.PlayerIsHoldingGolden) { //If player is not holding the golden berry 
                format = format.Replace(RunCurrentPbStatus, "-");
                format = format.Replace(RunCurrentPbStatusSession, "-");
                format = format.Replace(RunCurrentPbStatusPercent, "-%");
                format = format.Replace(RunCurrentPbStatusPercentSession, "-%");
                return format;

            } else if (chapterPath.CurrentRoom == null) { //or is not on the path
                format = StatManager.NotOnPathFormat(format, RunCurrentPbStatus);
                format = StatManager.NotOnPathFormat(format, RunCurrentPbStatusSession);
                format = StatManager.NotOnPathFormatPercent(format, RunCurrentPbStatusPercent);
                format = StatManager.NotOnPathFormatPercent(format, RunCurrentPbStatusPercentSession);
                return format;
            }


            int totalGoldenDeaths = chapterPath.Stats.GoldenBerryDeaths;
            int totalGoldenDeathsSession = chapterPath.Stats.GoldenBerryDeathsSession;

            int goldenDeathsUntilRoom = 0;
            int goldenDeathsUntilRoomSession = 0;

            bool foundRoom = false;
            //Walk path
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        foundRoom = true;
                        break;
                    }
                    goldenDeathsUntilRoom += chapterStats.GetRoom(rInfo.DebugRoomName).GoldenBerryDeaths;
                    goldenDeathsUntilRoomSession += chapterStats.GetRoom(rInfo.DebugRoomName).GoldenBerryDeathsSession;
                }

                if (foundRoom) break;
            }

            //Output Run Status
            int runStatus = (totalGoldenDeaths - goldenDeathsUntilRoom) + 1;
            int runStatusSession = (totalGoldenDeathsSession - goldenDeathsUntilRoomSession) + 1;

            string runStatusStr = runStatus == 1 ? $"PB" : $"{runStatus}";
            string runStatusSessionStr = runStatusSession == 1 ? $"PB" : $"{runStatusSession}";

            format = format.Replace(RunCurrentPbStatus, runStatusStr);
            format = format.Replace(RunCurrentPbStatusSession, runStatusSessionStr);

            //Output Run Status Percent
            string runStatusPercentStr, runStatusPercentSessionStr;

            if (totalGoldenDeaths == 0) {
                runStatusPercentStr = "100%";
            } else {
                runStatusPercentStr = $"{StatManager.FormatPercentage(goldenDeathsUntilRoom, totalGoldenDeaths)}";
            }

            if (totalGoldenDeathsSession == 0) {
                runStatusPercentSessionStr = "100%";
            } else {
                runStatusPercentSessionStr = $"{StatManager.FormatPercentage(goldenDeathsUntilRoomSession, totalGoldenDeathsSession)}";
            }

            format = format.Replace(RunCurrentPbStatusPercent, runStatusPercentStr);
            format = format.Replace(RunCurrentPbStatusPercentSession, runStatusPercentSessionStr);


            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }
    }
}
