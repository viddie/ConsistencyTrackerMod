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
     {chapter:averageRunDistance} - Average run distance over all runs ever
     {chapter:averageRunDistanceSession} - Average run distance over all runs of the current session
     
     {chapter:averageRunDistanceSession#x} - Average run distance over the last X runs of the current session
     {chapter:lastRunDistance#x} - Distance of the last #x run

         */

    public class AverageLastRunsStat : Stat {
        public static string ChapterAverageRunDistance = "{chapter:averageRunDistance}";
        public static string ChapterAverageRunDistanceSession = "{chapter:averageRunDistanceSession}";

        public static string ChapterHighestAverageOver10Runs = "{chapter:highestAverageOver10Runs}";

        public static string ChapterAverageRunDistanceSessionOverX = @"\{chapter:averageRunDistanceSession#(.*?)\}";
        public static string ChapterLastRunDistanceOverX = @"\{chapter:lastRunDistance#(.*?)\}";

        public static ValuePlaceholder<int> ListRollingAverageRunDistances = new ValuePlaceholder<int>(@"\{list:rollingAverageRunDistances#(.*?)\}", "{list:rollingAverageRunDistances#(.*?)}");

        public static List<string> IDs = new List<string>() { ChapterAverageRunDistance, ChapterAverageRunDistanceSession, ChapterHighestAverageOver10Runs };

        public AverageLastRunsStat() : base(IDs) {}

        public override bool ContainsIdentificator(string format) {
            if (format.Contains(ChapterAverageRunDistance) || format.Contains(ChapterAverageRunDistanceSession) || format.Contains(ChapterHighestAverageOver10Runs)) return true;

            return ListRollingAverageRunDistances.HasMatch(format) || Regex.IsMatch(format, ChapterAverageRunDistanceSessionOverX) || Regex.IsMatch(format, ChapterLastRunDistanceOverX);
        }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) { //Player doesnt have path
                format = StatManager.MissingPathFormat(format, ChapterAverageRunDistance);
                format = StatManager.MissingPathFormat(format, ChapterAverageRunDistanceSession);
                format = StatManager.MissingPathFormat(format, ChapterHighestAverageOver10Runs);

                format = Regex.Replace(format, ChapterAverageRunDistanceSessionOverX, StatManager.MissingPathOutput);
                format = Regex.Replace(format, ChapterLastRunDistanceOverX, StatManager.MissingPathOutput);
                format = ListRollingAverageRunDistances.ReplaceAll(format, StatManager.MissingPathOutput);
                return format;
            }


            double averageRunDistance = 0;
            int countRunsTotal = 0;

            double averageRunDistanceSession = 0;
            int countRunsTotalSession = 0;

            foreach (RoomStats rStats in chapterStats.Rooms.Values) {
                RoomInfo rInfo = chapterPath.FindRoom(rStats);
                if (rInfo == null) //rStats room is not on the path
                    continue;

                countRunsTotal += rStats.GoldenBerryDeaths;
                averageRunDistance += rInfo.RoomNumberInChapter * rStats.GoldenBerryDeaths;

                countRunsTotalSession += rStats.GoldenBerryDeathsSession;
                averageRunDistanceSession += rInfo.RoomNumberInChapter * rStats.GoldenBerryDeathsSession;
            }

            if(countRunsTotal > 0)
                averageRunDistance /= countRunsTotal;
            if(countRunsTotalSession > 0)
                averageRunDistanceSession /= countRunsTotalSession;




            Dictionary<string, string> invalidFormats = new Dictionary<string, string>();
            Dictionary<int, string> runCountsToFormatSession = new Dictionary<int, string>();
            Dictionary<int, string> lastRunNumbersToFormat = new Dictionary<int, string>();

            // ========= REGEX PART ==========
            MatchCollection matches = Regex.Matches(format, ChapterAverageRunDistanceSessionOverX);
            foreach (Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string runCountStr = match.Groups[i].Value;
                    int runCountInt;
                    try {
                        runCountInt = int.Parse(runCountStr);
                        if (runCountInt < 1) throw new ArgumentException();

                        if (!runCountsToFormatSession.ContainsKey(runCountInt))
                            runCountsToFormatSession.Add(runCountInt, null);

                    } catch (FormatException) {
                        if (invalidFormats.ContainsKey($"{{chapter:averageRunDistanceSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{chapter:averageRunDistanceSession#{match.Groups[i].Value}}}", $"<Invalid run count value: {match.Groups[i].Value}>");
                    } catch (ArgumentException) {
                        if (invalidFormats.ContainsKey($"{{chapter:averageRunDistanceSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{chapter:averageRunDistanceSession#{match.Groups[i].Value}}}", $"<Run count value must be 1 or greater: {match.Groups[i].Value}>");
                    } catch (Exception) {
                        if (invalidFormats.ContainsKey($"{{chapter:averageRunDistanceSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{chapter:averageRunDistanceSession#{match.Groups[i].Value}}}", $"<Invalid run count value: {match.Groups[i].Value}>");
                    }
                }
            }


            matches = Regex.Matches(format, ChapterLastRunDistanceOverX);
            foreach (Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string runNrStr = match.Groups[i].Value;
                    int runNrInt;
                    try {
                        runNrInt = int.Parse(runNrStr);
                        if (runNrInt < 1) throw new ArgumentException();

                        if (!lastRunNumbersToFormat.ContainsKey(runNrInt))
                            lastRunNumbersToFormat.Add(runNrInt, null);

                    } catch (FormatException) {
                        if (invalidFormats.ContainsKey($"{{chapter:lastRunDistance#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{chapter:lastRunDistance#{match.Groups[i].Value}}}", $"<Invalid run count value: {match.Groups[i].Value}>");
                    } catch (ArgumentException) {
                        if (invalidFormats.ContainsKey($"{{chapter:lastRunDistance#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{chapter:lastRunDistance#{match.Groups[i].Value}}}", $"<Run count value must be 1 or greater: {match.Groups[i].Value}>");
                    } catch (Exception) {
                        if (invalidFormats.ContainsKey($"{{chapter:lastRunDistance#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{chapter:lastRunDistance#{match.Groups[i].Value}}}", $"<Invalid run count value: {match.Groups[i].Value}>");
                    }
                }
            }

            // ===========================================

            double averageRunDistanceLastXSession = 0;
            int runCountLastXSession = 0;
            for (int i = chapterStats.CurrentChapterLastGoldenRuns.Count - 1; i >= 0; i--) {
                RoomStats rStats = chapterStats.CurrentChapterLastGoldenRuns[i];

                RoomInfo rInfo = chapterPath.FindRoom(rStats);
                if (rInfo == null) continue;

                averageRunDistanceLastXSession += rInfo.RoomNumberInChapter;
                runCountLastXSession++;

                if (runCountsToFormatSession.ContainsKey(runCountLastXSession)) {
                    double avg = averageRunDistanceLastXSession / runCountLastXSession;
                    runCountsToFormatSession[runCountLastXSession] = $"{StatManager.FormatDouble(avg)}";
                }

                if (lastRunNumbersToFormat.ContainsKey(runCountLastXSession)) {
                    lastRunNumbersToFormat[runCountLastXSession] = $"{rInfo.RoomNumberInChapter}";
                }

                if (runCountLastXSession == 10) {
                    double avg = averageRunDistanceLastXSession / runCountLastXSession;
                    if (!chapterStats.CurrentChapterRollingAverages.ContainsKey(10)) {
                        chapterStats.CurrentChapterRollingAverages.Add(10, avg);
                    } else if(avg > chapterStats.CurrentChapterRollingAverages[10]) {
                        chapterStats.CurrentChapterRollingAverages[10] = avg;
                    }
                }
            }


            double leftOverAvg = averageRunDistanceLastXSession / runCountLastXSession;

            Dictionary<int, string> toSet = new Dictionary<int, string>();

            foreach (int nr in runCountsToFormatSession.Keys) {
                string formatted = runCountsToFormatSession[nr];
                if (formatted == null) {
                    if (runCountLastXSession == 0) {
                        toSet.Add(nr, $"{StatManager.ValueNotAvailable}");
                    } else {
                        toSet.Add(nr, $"{StatManager.FormatDouble(leftOverAvg)}");
                    }
                }
            }
            foreach (var kv in toSet) {
                runCountsToFormatSession[kv.Key] = kv.Value;
            }


            // ========================================

            List<int> matchList = ListRollingAverageRunDistances.GetMatchList(format);
            Dictionary<int, string> rollingAverageOutputs = new Dictionary<int, string>();

            foreach (int windowSize in matchList) {
                int count = chapterStats.CurrentChapterLastGoldenRuns.Count;
                if (count < windowSize || windowSize < 1) {
                    rollingAverageOutputs.Add(windowSize, StatManager.ValueNotAvailable);
                    continue;
                }

                List<double> graph = new List<double>();
                int lastIndex = count - windowSize;

                for (int i = 0; i <= lastIndex; i++) {
                    double avg = 0;
                    for (int j = 0; j < windowSize; j++) {
                        int index = i + j;
                        RoomStats rStats = chapterStats.CurrentChapterLastGoldenRuns[index];
                        RoomInfo rInfo = chapterPath.FindRoom(rStats);
                        //ConsistencyTrackerModule.Instance.Log($"Adding to avg: index '{index}', index.RoomNumber '{rInfo.RoomNumberInChapter}'");
                        avg += rInfo.RoomNumberInChapter;
                    }
                    //ConsistencyTrackerModule.Instance.Log($"Adding to graph: avg before '{avg}', windowSize '{windowSize}', avg '{avg / windowSize}'");
                    graph.Add(avg / windowSize);
                }


                string output = string.Join(", ", graph);
                switch (StatManager.ListOutputFormat) {
                    case ListFormat.Plain:
                        rollingAverageOutputs.Add(windowSize, output);
                        break;
                    case ListFormat.Json:
                        rollingAverageOutputs.Add(windowSize, $"[{output}]");
                        break;
                }
            }

            format = ListRollingAverageRunDistances.ReplaceMatchException(format);
            format = ListRollingAverageRunDistances.ValueReplaceAll(format, rollingAverageOutputs);

            // ========================================

            //Output requested runs
            //Average run distance
            foreach (int nr in runCountsToFormatSession.Keys) {
                string formatted = runCountsToFormatSession[nr];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{chapter:averageRunDistanceSession#{nr}}}", formatted);
            }
            foreach (int nr in lastRunNumbersToFormat.Keys) {
                string formatted = lastRunNumbersToFormat[nr];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{chapter:lastRunDistance#{nr}}}", formatted);
            }

            format = format.Replace(ChapterAverageRunDistance, $"{StatManager.FormatDouble(averageRunDistance)}");
            format = format.Replace(ChapterAverageRunDistanceSession, $"{StatManager.FormatDouble(averageRunDistanceSession)}");

            if (chapterStats.CurrentChapterRollingAverages.ContainsKey(10)) {
                format = format.Replace(ChapterHighestAverageOver10Runs, $"{StatManager.FormatDouble(chapterStats.CurrentChapterRollingAverages[10])}");
            } else {
                format = format.Replace(ChapterHighestAverageOver10Runs, $"{StatManager.ValueNotAvailable}");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        //success-rate;Room SR: {room:successRate} | CP: {checkpoint:successRate} | Total: {chapter:successRate} 
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(ChapterAverageRunDistance, "Average run distance over all runs ever"),
                new KeyValuePair<string, string>(ChapterAverageRunDistanceSession, "Average run distance over all runs of the current session"),
                new KeyValuePair<string, string>(ChapterHighestAverageOver10Runs, "The highest average distance over any 10 consecutive runs of the current session"),
                new KeyValuePair<string, string>("{chapter:averageRunDistanceSession#X}", "Average run distance over the last X runs of the current session"),
                new KeyValuePair<string, string>("{chapter:lastRunDistance#X}", "The room number of last run nr. X (e.g. {chapter:lastRunDistance#1} would give you the room number of the most recent golden run, #2 of the 2nd most recent, ...)"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat("basic-avg-distance", $"Avg. run distance: {ChapterAverageRunDistance}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat("basic-avg-distance-session", $"Avg. run distance (Session): {ChapterAverageRunDistanceSession}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat("basic-avg-distance-last-10", $"Avg. over last 10 runs: {{chapter:averageRunDistanceSession#10}}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat("basic-avg-distance-best-10", $"Best 10-run-average: {ChapterHighestAverageOver10Runs}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat("basic-last-5-runs", $"Last 5 runs: {{chapter:lastRunDistance#1}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#2}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#3}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#4}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#5}}/{LiveProgressStat.ChapterRoomCount}"),
                
            };
        }
    }
}
/*
 Avg. run distance: {chapter:averageRunDistance}/{chapter:roomCount}
 Avg. run distance (Session): {chapter:averageRunDistanceSession}/{chapter:roomCount}
 Avg. over last 10 runs: {chapter:averageRunDistanceSession#10}/{chapter:roomCount}
 
    Last runs:
    {chapter:lastRunDistance#1}/{chapter:roomCount}
    {chapter:lastRunDistance#2}/{chapter:roomCount}
    {chapter:lastRunDistance#3}/{chapter:roomCount}
    {chapter:lastRunDistance#4}/{chapter:roomCount}
    {chapter:lastRunDistance#5}/{chapter:roomCount}
     
     */
