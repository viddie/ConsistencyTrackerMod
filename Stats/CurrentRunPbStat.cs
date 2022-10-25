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
     {run:currentPbStatus}                 - Format e.g. "Current run: 75. best run", "Current run: 4. best run", "Current run: PB"
     {run:currentPbStatusSession}          - Format e.g. "Current run: 75. best run", "Current run: 4. best run", "Current run: PB"
     {run:currentPbStatusPercent}          - Format e.g. "Current run better than 0% of all runs", "Current run better than 72.39% of all runs", "Current run better than 100% of all runs"
     {run:currentPbStatusPercentSession}   - Format e.g. "Current run better than 0% of all runs", "Current run better than 72.39% of all runs", "Current run better than 100% of all runs"

     {run:topXPercent}                     - Opposite percentage of {run:currentPbStatusPercent}
     {run:topXPercentSession}              - Opposite percentage of {run:currentPbStatusPercentSession}
         */

    public class CurrentRunPbStat : Stat {

        public static string RunCurrentPbStatus = "{run:currentPbStatus}";
        public static string RunCurrentPbStatusSession = "{run:currentPbStatusSession}";
        public static string RunCurrentPbStatusPercent = "{run:currentPbStatusPercent}";
        public static string RunCurrentPbStatusPercentSession = "{run:currentPbStatusPercentSession}";
        public static string RunTopXPercent = "{run:topXPercent}";
        public static string RunTopXPercentSession = "{run:topXPercentSession}";
        public static List<string> IDs = new List<string>() {
            RunCurrentPbStatus, RunCurrentPbStatusSession,
            RunCurrentPbStatusPercent, RunCurrentPbStatusPercentSession,
            RunTopXPercent, RunTopXPercentSession
        };


        public CurrentRunPbStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatus);
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatusSession);
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatusPercent);
                format = StatManager.MissingPathFormat(format, RunCurrentPbStatusPercentSession);
                format = StatManager.MissingPathFormat(format, RunTopXPercent);
                format = StatManager.MissingPathFormat(format, RunTopXPercentSession);
                return format;
            }

            if (!chapterStats.ModState.PlayerIsHoldingGolden) { //If player is not holding the golden berry 
                format = format.Replace(RunCurrentPbStatus, "-");
                format = format.Replace(RunCurrentPbStatusSession, "-");
                format = format.Replace(RunCurrentPbStatusPercent, "-%");
                format = format.Replace(RunCurrentPbStatusPercentSession, "-%");
                format = format.Replace(RunTopXPercent, "-%");
                format = format.Replace(RunTopXPercentSession, "-%");
                return format;

            } else if (chapterPath.CurrentRoom == null) { //or is not on the path
                format = StatManager.NotOnPathFormat(format, RunCurrentPbStatus);
                format = StatManager.NotOnPathFormat(format, RunCurrentPbStatusSession);
                format = StatManager.NotOnPathFormatPercent(format, RunCurrentPbStatusPercent);
                format = StatManager.NotOnPathFormatPercent(format, RunCurrentPbStatusPercentSession);
                format = StatManager.NotOnPathFormatPercent(format, RunTopXPercent);
                format = StatManager.NotOnPathFormatPercent(format, RunTopXPercentSession);
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
            string topXPercentStr, topXPercentSessionStr;

            if (totalGoldenDeaths == 0) {
                runStatusPercentStr = "100%";
                topXPercentStr = "0%";
            } else {
                double runStatusPercent = (double)goldenDeathsUntilRoom / totalGoldenDeaths;

                runStatusPercentStr = $"{StatManager.FormatPercentage(runStatusPercent)}";
                topXPercentStr = $"{StatManager.FormatPercentage(1 - runStatusPercent)}";
            }

            if (totalGoldenDeathsSession == 0) {
                runStatusPercentSessionStr = "100%";
                topXPercentSessionStr = "0%";
            } else {
                double runStatusPercentSession = (double)goldenDeathsUntilRoomSession / totalGoldenDeathsSession;

                runStatusPercentSessionStr = $"{StatManager.FormatPercentage(runStatusPercentSession)}";
                topXPercentSessionStr = $"{StatManager.FormatPercentage(1 - runStatusPercentSession)}";
            }

            format = format.Replace(RunCurrentPbStatusPercent, runStatusPercentStr);
            format = format.Replace(RunCurrentPbStatusPercentSession, runStatusPercentSessionStr);

            format = format.Replace(RunTopXPercent, topXPercentStr);
            format = format.Replace(RunTopXPercentSession, topXPercentSessionStr);


            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        //current-run-pb;Current run: #{run:currentPbStatus}, better than {run:currentPbStatusPercent} of all runs
        //current-run-pb-session;Current run(Session): #{run:currentPbStatusSession}, better than {run:currentPbStatusPercentSession} of all runs this session
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(RunCurrentPbStatus, "Rank of the current run vs. all other runs, e.g. 3rd best run, or PB run"),
                new KeyValuePair<string, string>(RunCurrentPbStatusPercent, "Percentage of many runs the current tops, e.g. The current run is better than 85% of all runs"),
                new KeyValuePair<string, string>(RunCurrentPbStatusSession, $"Same as {RunCurrentPbStatus}, but only for the current session"),
                new KeyValuePair<string, string>(RunCurrentPbStatusPercentSession, $"Same as {RunCurrentPbStatusPercent}, but only for the current session"),
                new KeyValuePair<string, string>(RunTopXPercent, $"Opposite percentage of {RunCurrentPbStatusPercent}, e.g. The current run is in the top 15% of all runs"),
                new KeyValuePair<string, string>(RunTopXPercentSession, $"Opposite percentage of {RunCurrentPbStatusPercentSession}"),
            };
        }
        public override List<StatFormat> GetStatExamples() {
            return new List<StatFormat>() {
                new StatFormat("current-run-pb", $"Current run: #{RunCurrentPbStatus}, better than {RunCurrentPbStatusPercent} of all runs (Top {RunTopXPercent})"),
                new StatFormat("current-run-pb-session", $"Current run (Session): #{RunCurrentPbStatusSession}, better than {RunCurrentPbStatusPercentSession} of all runs this session (Top {RunTopXPercentSession})")
            };
        }
    }
}
