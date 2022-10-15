using Celeste.Mod.ConsistencyTracker.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Stats {
    public class StatManager {

        public static List<Stat> AllStats = new List<Stat>() {
            new SuccessRateStat(),
            new ChokeRateStat(),
            new SuccessRateColorsStat(),
            new LiveProgressStat(),
            new PersonalBestStat(),
            new CurrentRunPbStat(),
        };

        public static string BaseFolder = "live-data";
        public static string MissingPathOutput = "<Missing Path>";

        public Dictionary<StatFormat, List<Stat>> Formats;

        public StatManager() {
            LoadFormats();

            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFolder("live-data"));
        }

        public void LoadFormats() {
            string formatFilePath = ConsistencyTrackerModule.GetPathToFile("live-data/format.txt");
            if (File.Exists(formatFilePath)) {
                string content = File.ReadAllText(formatFilePath);
                Formats = ParseFormatsFile(content);

            } else {
                Formats = CreateDefaultFormatFile(formatFilePath);
            }

            FindStatsForFormats();
        }

        public void FindStatsForFormats() {
            foreach (StatFormat format in Formats.Keys) {
                List<Stat> statList = Formats[format];

                foreach (Stat stat in AllStats) {
                    if (stat.ContainsIdentificator(format.Format))
                        statList.Add(stat);
                }
            }
        }

        public void OutputFormats(PathInfo pathInfo, ChapterStats chapterStats) {
            //To summarize some data that many stats need
            AggregateStatsPass(pathInfo, chapterStats);

            foreach (StatFormat format in Formats.Keys) {
                List<Stat> statList = Formats[format];
                string outFileName = $"{format.Name}.txt";
                string outFilePath = ConsistencyTrackerModule.GetPathToFile($"{BaseFolder}/{outFileName}");

                string formattedData = format.Format;

                foreach (Stat stat in statList) {
                    formattedData = stat.FormatStat(pathInfo, chapterStats, formattedData);
                }

                File.WriteAllText(outFilePath, formattedData);
            }
        }

        /// <summary>To summarize some data that many stats need.</summary>
        public void AggregateStatsPass(PathInfo pathInfo, ChapterStats chapterStats) {
            if (pathInfo == null) return;

            int attemptCount = ConsistencyTrackerModule.Instance.ModSettings.LiveDataSelectedAttemptCount;

            if (pathInfo.Stats == null) AggregateStatsPassOnce(pathInfo, chapterStats);
            pathInfo.Stats = new AggregateStats();

            pathInfo.CurrentRoom = null;

            //Walk the path
            foreach (CheckpointInfo cpInfo in pathInfo.Checkpoints) {
                cpInfo.Stats = new AggregateStats();

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = chapterStats.GetRoom(rInfo.DebugRoomName);
                    cpInfo.Stats.CountAttempts += rStats.AttemptsOverN(attemptCount);
                    cpInfo.Stats.CountSuccesses += rStats.SuccessesOverN(attemptCount);
                    cpInfo.Stats.CountGoldenBerryDeaths += rStats.GoldenBerryDeaths;
                    cpInfo.Stats.CountGoldenBerryDeathsSession += rStats.GoldenBerryDeathsSession;

                    cpInfo.Stats.GoldenChance *= rStats.AverageSuccessOverN(attemptCount);

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        pathInfo.CurrentRoom = rInfo;
                    }
                }


                pathInfo.Stats.CountAttempts += cpInfo.Stats.CountAttempts;
                pathInfo.Stats.CountSuccesses += cpInfo.Stats.CountSuccesses;
                pathInfo.Stats.CountGoldenBerryDeaths += cpInfo.Stats.CountGoldenBerryDeaths;
                pathInfo.Stats.CountGoldenBerryDeathsSession += cpInfo.Stats.CountGoldenBerryDeathsSession;

                pathInfo.Stats.GoldenChance *= cpInfo.Stats.GoldenChance;
            }
        }

        /// <summary>For data that shouldn't be done on every update but rather once when chapter is changed.</summary>
        public void AggregateStatsPassOnce(PathInfo pathInfo, ChapterStats chapterStats) {
            //Walk the path
            int cpNumber = 0;
            int roomNumber = 0;
            foreach (CheckpointInfo cpInfo in pathInfo.Checkpoints) {
                cpNumber++;
                cpInfo.CPNumberInChapter = cpNumber;

                int roomNumberInCP = 0;

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    roomNumber++;
                    roomNumberInCP++;
                    rInfo.RoomNumberInChapter = roomNumber;
                    rInfo.RoomNumberInCP = roomNumberInCP;
                }
            }
        }


        public static string MissingPathFormat(string format, string id) {
            return format.Replace(id, MissingPathOutput);
        }


        public Dictionary<StatFormat, List<Stat>> CreateDefaultFormatFile(string path) {
            Dictionary <StatFormat, List <Stat>> formats = new Dictionary<StatFormat, List<Stat>>() {
                [new StatFormat("success-rate", $"Room SR: {SuccessRateStat.RoomSuccessRate} | CP: {SuccessRateStat.CheckpointSuccessRate} | Total: {SuccessRateStat.ChapterSuccessRate}")] = new List<Stat>() { },
                [new StatFormat("pb", $"Best runs: {{pb:best}} | {{pb:best#2}} | {{pb:best#3}} | {{pb:best#4}} | {{pb:best#5}}")] = new List<Stat>() { },
                [new StatFormat("pb-session", $"Best runs: {{pb:bestSession}} | {{pb:bestSession#2}} | {{pb:bestSession#3}} | {{pb:bestSession#4}} | {{pb:bestSession#5}}")] = new List<Stat>() { },
                [new StatFormat("color-tracker", $"Reds: {SuccessRateColorsStat.ColorRed}, Yellows: {SuccessRateColorsStat.ColorYellow}, Greens: {SuccessRateColorsStat.ColorGreen}, Light-Greens: {SuccessRateColorsStat.ColorLightGreen}")] = new List<Stat>() { },
                [new StatFormat("choke-rate", $"Room Choke Rate: {ChokeRateStat.RoomChokeRate} (CP: {ChokeRateStat.CheckpointChokeRate})")] = new List<Stat>() { },
                [new StatFormat("live-progress", $"Room {LiveProgressStat.RunChapterProgressNumber}/{LiveProgressStat.ChapterRoomCount} ({LiveProgressStat.RunChapterProgressPercent})" +
                    $" | Room in CP: {LiveProgressStat.RunCheckpointProgressNumber}/{LiveProgressStat.CheckpointRoomCount} ({LiveProgressStat.RunCheckpointProgressPercent})")] = new List<Stat>() { },
            };

            string prelude = $"# Lines starting with a # are ignored\n" +
                $"# \n" +
                $"# Each line in this file corresponds to one output file following this scheme:\n" +
                $"# <filename>;<format>\n" +
                $"# where the format can be any text or placeholders mixed together\n" +
                $"# \n" +
                $"# Example:\n" +
                $"# successRate;Room SR: {SuccessRateStat.RoomSuccessRate} | CP: {SuccessRateStat.CheckpointSuccessRate} | Total: {SuccessRateStat.ChapterSuccessRate}\n" +
                $"# would generate a 'successRate.txt' file containing the text \"Room SR: <data> | CP: <data> | Total: <data>\"\n" +
                $"# \n" +
                $"# To add new-lines to a format use '\\n'";

            string content = FormatsToFile(formats, prelude);
            File.WriteAllText(path, content);

            return formats;
        }

        public static string FormatsToFile(Dictionary<StatFormat, List<Stat>> formats, string prelude) {
            string toRet = $"{prelude}\n\n";

            foreach (StatFormat format in formats.Keys) {
                string formatText = format.Format;
                formatText = formatText.Replace("\n", "\\n");
                toRet += $"{format.Name};{formatText}\n";
            }

            return toRet;
        }
        public static Dictionary<StatFormat, List<Stat>> ParseFormatsFile(string content) {
            Dictionary<StatFormat, List<Stat>> toRet = new Dictionary<StatFormat, List<Stat>>();

            string[] lines = content.Split(new char[] { '\n' });

            foreach (string line in lines) {
                if (line.Trim().StartsWith("#")) continue;

                string[] formatSplit = line.Trim().Split(new char[] { ';' });
                if (formatSplit.Length <= 1) {
                    //Ill-formed format detected
                    continue;
                }

                string name = formatSplit[0];
                string formatText = string.Join(";", formatSplit.Skip(1));

                formatText = formatText.Replace("\\n", "\n");

                toRet.Add(new StatFormat(name, formatText), new List<Stat>());
            }

            return toRet;
        }


        public static string PercentageFormatting(int a, int b, int decimals=int.MaxValue) {
            if (decimals == int.MaxValue) {
                decimals = ConsistencyTrackerModule.Instance.ModSettings.LiveDataDecimalPlaces;
            }

            double res = Math.Round(((double)a / b) * 100, decimals);

            return $"{res}%";
        }
    }
}
