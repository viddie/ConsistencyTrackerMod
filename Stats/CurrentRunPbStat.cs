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


            int totalGoldenRuns = chapterPath.Stats.GoldenBerryDeaths + chapterStats.GoldenCollectedCount;
            int totalGoldenRunsSession = chapterPath.Stats.GoldenBerryDeathsSession + chapterStats.GoldenCollectedCountSession;
            if (chapterStats.GoldenCollectedThisRun) {
                totalGoldenRuns--;
                totalGoldenRunsSession--;
            }

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
            int runStatus = (totalGoldenRuns - goldenDeathsUntilRoom) + 1;
            int runStatusSession = (totalGoldenRunsSession - goldenDeathsUntilRoomSession) + 1;

            string runStatusStr = runStatus == 1 ? $"PB" : $"{runStatus}";
            string runStatusSessionStr = runStatusSession == 1 ? $"PB" : $"{runStatusSession}";

            format = format.Replace(RunCurrentPbStatus, runStatusStr);
            format = format.Replace(RunCurrentPbStatusSession, runStatusSessionStr);

            //Output Run Status Percent
            string runStatusPercentStr, runStatusPercentSessionStr;
            string topXPercentStr, topXPercentSessionStr;

            if (totalGoldenRuns == 0) {
                runStatusPercentStr = "100%";
                topXPercentStr = "0%";
            } else {
                double runStatusPercent = (double)goldenDeathsUntilRoom / totalGoldenRuns;

                runStatusPercentStr = $"{StatManager.FormatPercentage(runStatusPercent)}";
                topXPercentStr = $"{StatManager.FormatPercentage(1 - runStatusPercent)}";
            }

            if (totalGoldenRunsSession == 0) {
                runStatusPercentSessionStr = "100%";
                topXPercentSessionStr = "0%";
            } else {
                double runStatusPercentSession = (double)goldenDeathsUntilRoomSession / totalGoldenRunsSession;

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
                new KeyValuePair<string, string>(RunCurrentPbStatus, Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_CURRENT_PB_STATUS")),
                new KeyValuePair<string, string>(RunCurrentPbStatusPercent, Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_CURRENT_PB_STATUS_PERCENT")),
                new KeyValuePair<string, string>(RunCurrentPbStatusSession, $"{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_CURRENT_PB_STATUS_SESSION_1")} {RunCurrentPbStatus}{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_CURRENT_PB_STATUS_SESSION_2")}"),
                new KeyValuePair<string, string>(RunCurrentPbStatusPercentSession, $"{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_CURRENT_PB_STATUS_PERCENT_SESSION_1")} {RunCurrentPbStatusPercent}{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_CURRENT_PB_STATUS_PERCENT_SESSION_2")}"),
                new KeyValuePair<string, string>(RunTopXPercent, $"{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_TOP_X_PERCENT_1")} {RunCurrentPbStatusPercent}{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_TOP_X_PERCENT_2")}"),
                new KeyValuePair<string, string>(RunTopXPercentSession, $"{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_EXPLANATIONS_RUN_TOP_X_PERCENT_SESSION")} {RunCurrentPbStatusPercentSession}"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_FORMAT_TITLE_BASIC_CURRENT_RUN"), $"{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_FORMAT_CONTENT_BASIC_CURRENT_RUN_1")}: #{RunCurrentPbStatus} ({Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_FORMAT_CONTENT_BASIC_CURRENT_RUN_2")} {RunTopXPercent})"),
                new StatFormat(Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_FORMAT_TITLE_BASIC_CURRENT_RUN_SESSION"), $"{Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_FORMAT_CONTENT_BASIC_CURRENT_RUN_SESSION_1")}: #{RunCurrentPbStatusSession} ({Dialog.Clean("CCT_STAT_CURRENT_RUN_PB_FORMAT_CONTENT_BASIC_CURRENT_RUN_SESSION_2")} {RunTopXPercentSession})")
            };
        }
    }
}
