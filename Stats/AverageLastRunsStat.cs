using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Newtonsoft.Json;

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


        public static Dictionary<string, Dictionary<int, double>> HighestRollingAverages { get; set; } = new Dictionary<string, Dictionary<int, double>>();
        

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
                RoomInfo rInfo = chapterPath.GetRoom(rStats);
                if (rInfo == null || rInfo.IsNonGameplayRoom) //rStats room is not on the path or is transition room
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
            Dictionary<int, double> rollingAvgs = GetCurrentChapterRollingAverages(chapterStats);

            double averageRunDistanceLastXSession = 0;
            int runCountLastXSession = 0;
            for (int i = chapterStats.LastGoldenRuns.Count - 1; i >= 0; i--) {
                
                int roomNumber = chapterPath.GetRunRoomNumberInChapter(chapterStats.LastGoldenRuns[i]);

                averageRunDistanceLastXSession += roomNumber;
                runCountLastXSession++;

                if (runCountsToFormatSession.ContainsKey(runCountLastXSession)) {
                    double avg = averageRunDistanceLastXSession / runCountLastXSession;
                    runCountsToFormatSession[runCountLastXSession] = $"{StatManager.FormatDouble(avg)}";
                }

                if (lastRunNumbersToFormat.ContainsKey(runCountLastXSession)) {
                    lastRunNumbersToFormat[runCountLastXSession] = $"{roomNumber}";
                }

                if (runCountLastXSession == 10) {
                    double avg = averageRunDistanceLastXSession / runCountLastXSession;
                    if (!rollingAvgs.ContainsKey(10)) {
                        rollingAvgs.Add(10, avg);
                    } else if(avg > rollingAvgs[10]) {
                        rollingAvgs[10] = avg;
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
                int count = chapterStats.LastGoldenRuns.Count;
                if (count < windowSize || windowSize < 1) {
                    rollingAverageOutputs.Add(windowSize, StatManager.ValueNotAvailable);
                    continue;
                }

                List<double> graph = GetRollingAverages(chapterPath, chapterStats, windowSize, chapterStats.LastGoldenRuns);

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

            if (rollingAvgs.ContainsKey(10)) {
                format = format.Replace(ChapterHighestAverageOver10Runs, $"{StatManager.FormatDouble(rollingAvgs[10])}");
            } else {
                format = format.Replace(ChapterHighestAverageOver10Runs, $"{StatManager.ValueNotAvailable}");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        public Dictionary<int, double> GetCurrentChapterRollingAverages(ChapterStats stats) {
            if (!HighestRollingAverages.ContainsKey(stats.ChapterUID)) {
                HighestRollingAverages.Add(stats.ChapterUID, new Dictionary<int, double>());
            }
            return HighestRollingAverages[stats.ChapterUID];
        }

        /// <summary>
        /// Returns averageRunDistance and averageRunDistanceSession as Tuple
        /// </summary>
        public static Tuple<double, double> GetAverageRunDistance(PathInfo chapterPath, ChapterStats chapterStats) {
            double averageRunDistance = 0;
            int countRunsTotal = 0;

            double averageRunDistanceSession = 0;
            int countRunsTotalSession = 0;

            foreach (RoomStats rStats in chapterStats.Rooms.Values) {
                RoomInfo rInfo = chapterPath.GetRoom(rStats);
                if (rInfo == null || rInfo.IsNonGameplayRoom) //rStats room is not on the path or is transition room
                    continue;

                countRunsTotal += rStats.GoldenBerryDeaths;
                averageRunDistance += rInfo.RoomNumberInChapter * rStats.GoldenBerryDeaths;

                countRunsTotalSession += rStats.GoldenBerryDeathsSession;
                averageRunDistanceSession += rInfo.RoomNumberInChapter * rStats.GoldenBerryDeathsSession;
            }

            averageRunDistance += (chapterPath.GameplayRoomCount + 1) * chapterStats.GoldenCollectedCount;
            countRunsTotal += chapterStats.GoldenCollectedCount;

            averageRunDistanceSession += (chapterPath.GameplayRoomCount + 1) * chapterStats.GoldenCollectedCountSession;
            countRunsTotalSession += chapterStats.GoldenCollectedCountSession;

            if (countRunsTotal > 0)
                averageRunDistance /= countRunsTotal;
            if (countRunsTotalSession > 0)
                averageRunDistanceSession /= countRunsTotalSession;

            return Tuple.Create(averageRunDistance, averageRunDistanceSession);
        }
        

        public static List<double> GetRollingAverages(PathInfo chapterPath, ChapterStats chapterStats, int windowSize, List<string> pastRuns) {
            int count = pastRuns.Count;
            List<double> graph = new List<double>();

            if (count < windowSize || windowSize < 1) {
                return graph;
            }

            int lastIndex = count - windowSize;

            for (int i = 0; i <= lastIndex; i++) {
                double avg = 0;
                for (int j = 0; j < windowSize; j++) {
                    int index = i + j;

                    int roomNumber = chapterPath.GetRunRoomNumberInChapter(pastRuns[index]);
                    if (roomNumber == 0) continue; //Not on path
                    
                    avg += roomNumber;
                }
                graph.Add(avg / windowSize);
            }

            return graph;
        }


        //success-rate;Room SR: {room:successRate} | CP: {checkpoint:successRate} | Total: {chapter:successRate} 
        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(ChapterAverageRunDistance, Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_EXPLANATIONS_CHAPTER_AVERAGE_RUN_DISTANCE")),
                new KeyValuePair<string, string>(ChapterAverageRunDistanceSession, Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_EXPLANATIONS_CHAPTER_AVERAGE_RUN_DISTANCE_SESSION")),
                new KeyValuePair<string, string>(ChapterHighestAverageOver10Runs, Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_EXPLANATIONS_CHAPTER_HIGHEST_AVERAGE_OVER_10_RUNS")),
                new KeyValuePair<string, string>("{chapter:averageRunDistanceSession#X}", Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_EXPLANATIONS_CHAPTER_AVERAGE_RUN_DISTANCE_SESSION_X")),
                new KeyValuePair<string, string>("{chapter:lastRunDistance#X}", $"{Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_EXPLANATIONS_CHAPTER_LAST_RUN_DISTANCE_X_1")} {{chapter:lastRunDistance#1}} {Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_EXPLANATIONS_CHAPTER_LAST_RUN_DISTANCE_X_2")}"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_TITLE_BASIC_AVG_DISTANCE"), $"{Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_CONTENT_BASIC_AVG_DISTANCE")} {ChapterAverageRunDistance}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat(Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_TITLE_BASIC_AVG_DISTANCE_SESSION"), $"{Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_CONTENT_BASIC_AVG_DISTANCE_SESSION")} {ChapterAverageRunDistanceSession}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat(Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_TITLE_BASIC_AVG_DISTANCE_LAST_10"), $"{Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_CONTENT_BASIC_AVG_DISTANCE_LAST_10")} {{chapter:averageRunDistanceSession#10}}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat(Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_TITLE_BASIC_AVG_DISTANCE_BEST_10"), $"{Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_CONTENT_BASIC_AVG_DISTANCE_BEST_10")} {ChapterHighestAverageOver10Runs}/{LiveProgressStat.ChapterRoomCount}"),
                new StatFormat(Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_TITLE_BASIC_LAST_5_RUNS"), $"{Dialog.Clean("CCT_STAT_AVERAGE_LAST_RUNS_FORMAT_CONTENT_BASIC_LAST_5_RUNS")} {{chapter:lastRunDistance#1}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#2}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#3}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#4}}/{LiveProgressStat.ChapterRoomCount}, {{chapter:lastRunDistance#5}}/{LiveProgressStat.ChapterRoomCount}"),
                
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
