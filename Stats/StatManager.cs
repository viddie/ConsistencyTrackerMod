using Celeste.Mod.ConsistencyTracker.Enums;
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
            new LiveProgressStat(),
            new CurrentRunPbStat(),
            new PersonalBestStat(),
            new ChokeRateStat(),
            new BasicInfoStat(),
            new BasicPathlessInfo(),
            new RunGoldenChanceStat(),
            new SuccessRateColorsStat(),
            new ListRoomNamesStat(),
            new ListSuccessRatesStat(),
            new ListChokeRatesStat(),
        };

        public static string BaseFolder = "live-data";
        public static string FormatFileName = "format.txt";
        public static string FormatSeparator = ";";
        public static string MissingPathOutput = "<path>";
        public static string NotOnPathOutput = "-";

        public static bool HideFormatsWithoutPath { get => ConsistencyTrackerModule.Instance.ModSettings.LiveDataHideFormatsWithoutPath; }
        public static RoomNameDisplayType RoomNameType { get => ConsistencyTrackerModule.Instance.ModSettings.LiveDataRoomNameDisplayType; }
        public static int AttemptCount { get => ConsistencyTrackerModule.Instance.ModSettings.LiveDataSelectedAttemptCount; }
        public static int DecimalPlaces { get => ConsistencyTrackerModule.Instance.ModSettings.LiveDataDecimalPlaces; }
        public static bool IgnoreUnplayedRooms { get => ConsistencyTrackerModule.Instance.ModSettings.LiveDataIgnoreUnplayedRooms; }
        public static ListFormat ListOutputFormat { get => ConsistencyTrackerModule.Instance.ModSettings.LiveDataListOutputFormat; }


        public Dictionary<StatFormat, List<Stat>> Formats;

        public StatManager() {
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFolder($"{BaseFolder}"));
            LoadFormats();
        }

        public void LoadFormats() {
            ConsistencyTrackerModule.Instance.Log($"[LoadFormats] Loading live-data formats...");
            string formatFilePath = ConsistencyTrackerModule.GetPathToFile($"{BaseFolder}/{FormatFileName}");
            if (File.Exists(formatFilePath)) {
                ConsistencyTrackerModule.Instance.Log($"[LoadFormats] Found {FormatFileName}...");
                string content = File.ReadAllText(formatFilePath);
                ConsistencyTrackerModule.Instance.Log($"[LoadFormats] Parsing {FormatFileName}");
                Formats = ParseFormatsFile(content);
                ConsistencyTrackerModule.Instance.Log($"[LoadFormats] Read '{Formats.Count}' formats from {FormatFileName}");

            } else {
                ConsistencyTrackerModule.Instance.Log($"[LoadFormats] Did not find {FormatFileName}, creating new...");
                Formats = CreateDefaultFormatFile(formatFilePath);
                ConsistencyTrackerModule.Instance.Log($"[LoadFormats] Read '{Formats.Count}' formats from default format file");
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
            ConsistencyTrackerModule.Instance.Log($"[OutputFormats] Starting output");

            try {
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
            } catch (Exception ex) {
                ConsistencyTrackerModule.Instance.Log($"[OutputFormats] Exception during aggregate pass, stat calculation or format outputting: {ex}");
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
                    cpInfo.Stats.GoldenBerryDeaths += rStats.GoldenBerryDeaths;
                    cpInfo.Stats.GoldenBerryDeathsSession += rStats.GoldenBerryDeathsSession;

                    float successRate = rStats.AverageSuccessOverN(attemptCount);
                    if (IgnoreUnplayedRooms && rStats.IsUnplayed)
                        successRate = 1;

                    cpInfo.Stats.GoldenChance *= successRate;

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        pathInfo.CurrentRoom = rInfo;
                    }
                }


                pathInfo.Stats.CountAttempts += cpInfo.Stats.CountAttempts;
                pathInfo.Stats.CountSuccesses += cpInfo.Stats.CountSuccesses;
                pathInfo.Stats.GoldenBerryDeaths += cpInfo.Stats.GoldenBerryDeaths;
                pathInfo.Stats.GoldenBerryDeathsSession += cpInfo.Stats.GoldenBerryDeathsSession;

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
            if (HideFormatsWithoutPath) {
                return "";
            }
            return format.Replace(id, MissingPathOutput);
        }
        public static string NotOnPathFormat(string format, string id, string addition="") {
            return format.Replace(id, $"{NotOnPathOutput}{addition}");
        }
        public static string NotOnPathFormatPercent(string format, string id) {
            return NotOnPathFormat(format, id, "%");
        }

        //basic-info;--- Chapter ---\nName: {chapter:debugName}\nGolden Deaths: {chapter:goldenDeaths} ({chapter:goldenDeathsSession})\nGolden Chance: {chapter:goldenChance}\n\n--- Checkpoint ---\nName: {checkpoint:name} ({checkpoint:abbreviation})\nGolden Deaths: {checkpoint:goldenDeaths} ({checkpoint:goldenDeathsSession})\nGolden Chance: {checkpoint:goldenChance}\n\n--- Room ---\nName: {room:name} ({room:debugName})\nGolden Deaths: {room:goldenDeaths} ({room:goldenDeathsSession})

        public Dictionary<StatFormat, List<Stat>> CreateDefaultFormatFile(string path) {
            Dictionary <StatFormat, List <Stat>> formats = new Dictionary<StatFormat, List<Stat>>();

            string prelude = $"# Lines starting with a # are ignored\n" +
                $"# \n" +
                $"# Each line in this file corresponds to one output file following this scheme:\n" +
                $"# <filename>{FormatSeparator}<format>\n" +
                $"# where the format can be any text or placeholders mixed together\n" +
                $"# \n" +
                $"# Example:\n" +
                $"# successRate{FormatSeparator}Room SR: {SuccessRateStat.RoomSuccessRate} | CP: {SuccessRateStat.CheckpointSuccessRate} | Total: {SuccessRateStat.ChapterSuccessRate}\n" +
                $"# would generate a 'successRate.txt' file containing the text \"Room SR: <data> | CP: <data> | Total: <data>\"\n" +
                $"# \n" +
                $"# To add new-lines to a format use '\\n'\n" +
                $"# \n" +
                $"# \n" +
                $"# List of all available placeholders:\n";

            //Add all stat explanations here
            foreach (Stat stat in AllStats) {
                foreach (KeyValuePair<string, string> explanation in stat.GetPlaceholderExplanations()) {
                    prelude += $"# {explanation.Key} - {explanation.Value}\n";
                }

                if (stat.GetPlaceholderExplanations().Count > 0)
                    prelude += $"# \n";

                foreach (StatFormat statFormat in stat.GetStatExamples()) {
                    formats.Add(statFormat, new List<Stat>());
                }
            }


            string afterExplanationHeader = $"# \n" +
                $"# Predefined Formats\n" +
                $"# ";

            string content = FormatsToFile(formats);

            string afterCustomFormatHeader = $"# \n" +
                $"# Custom Formats\n" +
                $"# \n";

            string combined = $"{prelude}\n{afterExplanationHeader}\n{content}\n{afterCustomFormatHeader}";

            File.WriteAllText(path, combined);

            return formats;
        }

        public static string FormatsToFile(Dictionary<StatFormat, List<Stat>> formats) {
            string toRet = $"";

            foreach (StatFormat format in formats.Keys) {
                string formatText = format.Format;
                formatText = formatText.Replace("\n", "\\n");
                toRet += $"{format.Name}{FormatSeparator}{formatText}\n\n";
            }

            return toRet;
        }
        public static Dictionary<StatFormat, List<Stat>> ParseFormatsFile(string content) {
            Dictionary<StatFormat, List<Stat>> toRet = new Dictionary<StatFormat, List<Stat>>();

            string[] lines = content.Split(new string[] { "\n" }, StringSplitOptions.None);

            foreach (string line in lines) {
                if (line.Trim() == "" || line.Trim().StartsWith("#")) continue; //Empty line or comment

                string[] formatSplit = line.Trim().Split(new string[] { FormatSeparator }, StringSplitOptions.None);
                if (formatSplit.Length <= 1) {
                    //Ill-formed format detected
                    continue;
                }

                string name = formatSplit[0];
                string formatText = string.Join(FormatSeparator, formatSplit.Skip(1));

                formatText = formatText.Replace("\\n", "\n");

                toRet.Add(new StatFormat(name, formatText), new List<Stat>());
            }

            return toRet;
        }


        public static string FormatPercentage(int a, int b, int decimals=int.MaxValue) {
            if (decimals == int.MaxValue) {
                decimals = DecimalPlaces;
            }

            double res = Math.Round(((double)a / b) * 100, decimals);

            return $"{res}%";
        }
        public static string FormatPercentage(double d, int decimals = int.MaxValue) {
            if (decimals == int.MaxValue) {
                decimals = DecimalPlaces;
            }

            double res = Math.Round(d * 100, decimals);

            return $"{res}%";
        }
        public static string FormatBool(bool b) {
            return b ? $"True" : $"False";
        }

        public static string GetFormattedRoomName(RoomInfo rInfo) {
            return rInfo.GetFormattedRoomName(RoomNameType);
        }
    }
}
