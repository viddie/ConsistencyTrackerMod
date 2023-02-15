using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Exceptions;
using Celeste.Mod.ConsistencyTracker.Models;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Stats {
    public class StatManager {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        public static List<Stat> AllStats = new List<Stat>() {
            new BasicPathlessInfo(),
            new SuccessRateStat(),
            new LiveProgressStat(),
            new ListCheckpointDeathsStat(),
            new CurrentRunPbStat(),
            new PersonalBestStat(),
            new ChokeRateStat(),
            new RunGoldenChanceStat(),
            new StreakStat(),
            new AverageLastRunsStat(),
            new SuccessRateColorsStat(),
            new BasicInfoStat(),
            new ListRoomNamesStat(),
            new ListSuccessRatesStat(),
            new ListChokeRatesStat(),
        };

        public static string BaseFolder = "live-data";
        public static string FormatFileName = "format.txt";
        public static string FormatSeparator = ";";
        public static string MissingPathOutput = "<path>";
        public static string NotOnPathOutput = "-";
        public static string ValueNotAvailable = "-";

        private static string FormatFilePath => ConsistencyTrackerModule.GetPathToFile($"{BaseFolder}/{FormatFileName}");

        public static bool HideFormatsWithoutPath { get => Mod.ModSettings.LiveDataHideFormatsWithoutPath; }
        public static RoomNameDisplayType RoomNameType { get => Mod.ModSettings.LiveDataRoomNameDisplayType; }
        public static int AttemptCount { get => Mod.ModSettings.LiveDataSelectedAttemptCount; }
        public static int DecimalPlaces { get => Mod.ModSettings.LiveDataDecimalPlaces; }
        public static bool IgnoreUnplayedRooms { get => Mod.ModSettings.LiveDataIgnoreUnplayedRooms; }
        public static ListFormat ListOutputFormat { get => Mod.ModSettings.LiveDataListOutputFormat; }


        public Dictionary<StatFormat, List<Stat>> Formats { get; set; }
        public Dictionary<StatFormat, string> LastResults { get; set; } = new Dictionary<StatFormat, string>();

        public bool HadPass => LastPassChapterStats != null;
        public ChapterStats LastPassChapterStats = null;
        public PathInfo LastPassPathInfo = null;

        public StatManager() {
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFolder($"{BaseFolder}"));
            LoadFormats();
        }

        public void LoadFormats() {
            Mod.Log($"Loading live-data formats...");
            string formatFilePath = FormatFilePath;
            
            if (File.Exists(formatFilePath)) {
                Mod.Log($"Found {FormatFileName}...", true);
                string content = File.ReadAllText(formatFilePath);
                Mod.Log($"Parsing {FormatFileName}", true);
                Formats = ParseFormatsFile(content);
                Mod.Log($"Read '{Formats.Count}' formats from {FormatFileName}", true);

            } else {
                Mod.Log($"Did not find {FormatFileName}, creating new...", true);
                Formats = CreateDefaultFormatsObject();
                SaveFormatFile();
                Mod.Log($"Read '{Formats.Count}' formats from default format file", true);
            }

            FindStatsForFormats();
        }

        public void ResetFormats() {
            Mod.Log($"Requested resetting of live-data formats, will perform backup first...");
            
            string formatFilePath = ConsistencyTrackerModule.GetPathToFile($"{BaseFolder}/{FormatFileName}");
            string fileName = Path.GetFileNameWithoutExtension(formatFilePath);
            string fileExt = Path.GetExtension(formatFilePath);
            string backupPath = ConsistencyTrackerModule.GetPathToFile($"{BaseFolder}/{fileName}_backup_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}{fileExt}");

            if (File.Exists(formatFilePath)) {
                File.Copy(formatFilePath, backupPath);
                Mod.Log($"Backed up {FormatFileName} to {backupPath}", true);
                File.Delete(formatFilePath);
                Mod.Log($"Deleted {FormatFileName}", true);

            } else {
                Mod.Log($"Did not find {FormatFileName} to delete, skipping backup", true);
            }

            LoadFormats();
            Mod.SaveChapterStats();
        }

        public void FindStatsForFormats() {
            List<StatFormat> keys = Formats.Keys.ToList();
            foreach (StatFormat format in keys) {
                List<Stat> statList = new List<Stat>();

                foreach (Stat stat in AllStats) {
                    if (stat.ContainsIdentificator(format.Format))
                        statList.Add(stat);
                }

                Formats[format] = statList;
            }
        }

        public void OutputFormats(PathInfo pathInfo, ChapterStats chapterStats) {
            Mod.Log($"Starting output");
            DateTime startTime = DateTime.Now;

            try {
                //To summarize some data that many stats need
                AggregateStatsPass(pathInfo, chapterStats);
                LastPassChapterStats = chapterStats;
                LastPassPathInfo = pathInfo;
                LastResults.Clear();


                foreach (StatFormat format in Formats.Keys) {
                    List<Stat> statList = Formats[format];
                    string outFileName = $"{format.Name}.txt";
                    string outFilePath = ConsistencyTrackerModule.GetPathToFile($"{BaseFolder}/{outFileName}");

                    string formattedData = format.Format;

                    foreach (Stat stat in statList) {
                        formattedData = stat.FormatStat(pathInfo, chapterStats, formattedData);
                    }

                    UpdateOverlayTexts(format.Name, formattedData);

                    LastResults.Add(format, formattedData);

                    if (Mod.ModSettings.LiveDataFileOutputEnabled) { 
                        File.WriteAllText(outFilePath, formattedData);
                    }
                }
            } catch (Exception ex) {
                Mod.Log($"Exception during aggregate pass or stat calculation: {ex}");
            }

            DateTime endTime = DateTime.Now;
            Mod.Log($"Outputting formats done! (Time taken: {(endTime - startTime).TotalSeconds}s)");
        }
        public string FormatVariableFormat(string format) {
            if (LastPassChapterStats == null || LastPassPathInfo == null) throw new NoStatPassException();

            foreach (Stat stat in AllStats) {
                if (stat.ContainsIdentificator(format)) {
                    try {
                        format = stat.FormatStat(LastPassPathInfo, LastPassChapterStats, format);
                    } catch (Exception ex) {
                        Mod.Log($"Exception during stat calculation: Stat '{stat.GetType().Name}' with format '{format}' caused exception -> {ex}");
                    }
                }
            }

            return format;
        }

        #region Stats Passes
        /// <summary>To summarize some data that many stats need.</summary>
        public void AggregateStatsPass(PathInfo pathInfo, ChapterStats chapterStats) {
            if (pathInfo == null) return;

            int attemptCount = Mod.ModSettings.LiveDataSelectedAttemptCount;

            if (pathInfo.Stats == null) AggregateStatsPassOnce(pathInfo);
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

                    int currentStreak = rStats.SuccessStreak;
                    if (currentStreak > rStats.SuccessStreakBest) {
                        rStats.SuccessStreakBest = currentStreak;
                    }

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        pathInfo.CurrentRoom = rInfo;
                    }
                    if (rInfo.DebugRoomName == Mod.SpeedrunToolSaveStateRoomName) {
                        pathInfo.SpeedrunToolSaveStateRoom = rInfo;
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
        public void AggregateStatsPassOnce(PathInfo pathInfo) {
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
        #endregion

        #region Overlay Hooks
        public void UpdateOverlayTexts(string formatName, string formatText) {
            bool holdingGolden = Mod.CurrentChapterStats.ModState.PlayerIsHoldingGolden;
            string noneFormat = "<same>";

            if (holdingGolden) {
                string compareFormat1 = Mod.ModSettings.IngameOverlayText1FormatGolden == noneFormat ? Mod.ModSettings.IngameOverlayText1Format : Mod.ModSettings.IngameOverlayText1FormatGolden;
                string compareFormat2 = Mod.ModSettings.IngameOverlayText2FormatGolden == noneFormat ? Mod.ModSettings.IngameOverlayText2Format : Mod.ModSettings.IngameOverlayText2FormatGolden;
                string compareFormat3 = Mod.ModSettings.IngameOverlayText3FormatGolden == noneFormat ? Mod.ModSettings.IngameOverlayText3Format : Mod.ModSettings.IngameOverlayText3FormatGolden;
                string compareFormat4 = Mod.ModSettings.IngameOverlayText4FormatGolden == noneFormat ? Mod.ModSettings.IngameOverlayText4Format : Mod.ModSettings.IngameOverlayText4FormatGolden;

                if (formatName == compareFormat1) {
                    Mod.IngameOverlay.SetText(1, formatText);
                }
                if (formatName == compareFormat2) {
                    Mod.IngameOverlay.SetText(2, formatText);
                }
                if (formatName == compareFormat3) {
                    Mod.IngameOverlay.SetText(3, formatText);
                }
                if (formatName == compareFormat4) {
                    Mod.IngameOverlay.SetText(4, formatText);
                }
            } else {
                if (formatName == Mod.ModSettings.IngameOverlayText1Format) {
                    Mod.IngameOverlay.SetText(1, formatText);
                }
                if (formatName == Mod.ModSettings.IngameOverlayText2Format) {
                    Mod.IngameOverlay.SetText(2, formatText);
                }
                if (formatName == Mod.ModSettings.IngameOverlayText3Format) {
                    Mod.IngameOverlay.SetText(3, formatText);
                }
                if (formatName == Mod.ModSettings.IngameOverlayText4Format) {
                    Mod.IngameOverlay.SetText(4, formatText);
                }
            }
        }
        #endregion

        #region Util
        /// <summary>
        /// Gets a format's text from the most recent stats pass
        /// </summary>
        /// <param name="format">The format to get</param>
        /// <returns>The text if a successful stats pass has been done before, null if not or the format name wasn't found</returns>
        public string GetLastPassFormatText(string format) {
            if (!HadPass) return null;

            KeyValuePair<StatFormat, string> stat = LastResults.FirstOrDefault((kv) => kv.Key.Name == format);
            if (stat.Key == null) {
                return null;
            }

            return stat.Value;
        }

        public List<KeyValuePair<string, string>> GetPlaceholderExplanationList() {
            List<KeyValuePair<string, string>> explanations = new List<KeyValuePair<string, string>>();
            foreach (Stat stat in AllStats) {
                foreach (KeyValuePair<string, string> explanation in stat.GetPlaceholderExplanations()) {
                    explanations.Add(explanation);
                }
            }

            return explanations;
        }

        public List<StatFormat> GetFormatListSorted() {
            List<StatFormat> formats = new List<StatFormat>();

            List<StatFormat> customFormats = GetCustomFormatList();
            List<StatFormat> defaultFormats = GetAvailableDefaultFormatList();

            foreach (StatFormat format in customFormats) {
                formats.Add(format);
            }
            foreach (StatFormat format in defaultFormats) {
                formats.Add(format);
            }

            return formats;
        }
        public List<StatFormat> GetCustomFormatList() {
            List<StatFormat> formats = new List<StatFormat>();

            List<StatFormat> defaultFormats = GetDefaultFormatList();
            
            foreach (StatFormat format in Formats.Keys) {
                if (defaultFormats.Any((f) => f.Name == format.Name)) {
                    
                } else {
                    formats.Add(format);
                }
            }

            return formats;
        }

        public List<StatFormat> GetDefaultFormatList() {
            List<StatFormat> formats = new List<StatFormat>();
            foreach (Stat stat in AllStats) {
                foreach (StatFormat statFormat in stat.GetDefaultFormats()) {
                    formats.Add(statFormat);
                }
            }
            return formats;
        }
        public List<StatFormat> GetAvailableDefaultFormatList() {
            List<StatFormat> formats = new List<StatFormat>();

            List<StatFormat> defaultFormats = GetDefaultFormatList();

            foreach (StatFormat format in Formats.Keys) {
                if (defaultFormats.Any((f) => f.Name == format.Name)) {
                    formats.Add(format);
                }
            }

            return formats;
        }

        public bool HasFormat(string formatName) {
            return Formats.Keys.Any((f) => f.Name == formatName);
        }

        public void CreateFormat(string formatName, string formatText) {
            StatFormat stat = new StatFormat(formatName, formatText);
            Formats.Add(stat, null);
            FindStatsForFormats();
            SaveFormatFile();
            Mod.SaveChapterStats();
        }
        public bool UpdateFormat(string formatName, string formatText) {
            StatFormat stat = Formats.Keys.FirstOrDefault((f) => f.Name == formatName);
            if (stat == null) {
                return false;
            }
            stat.Format = formatText;
            FindStatsForFormats();
            SaveFormatFile();
            Mod.SaveChapterStats();
            return true;
        }
        public bool DeleteFormat(string formatName) {
            StatFormat stat = Formats.Keys.FirstOrDefault((f) => f.Name == formatName);
            if (stat == null) {
                return false;
            }
            Formats.Remove(stat);
            SaveFormatFile();
            Mod.SaveChapterStats();
            return true;
        }
        #endregion

        #region Format file IO
        
        public void SaveFormatFile(bool resetDefaultFormats = false) {
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
            }


            string afterExplanationHeader = $"# \n" +
                $"# Predefined Formats\n" +
                $"# ";

            string content;
            if (resetDefaultFormats) {
                content = FormatsToString(GetDefaultFormatList());
            } else {
                content = FormatsToString(GetAvailableDefaultFormatList());
            }

            string afterCustomFormatHeader = $"# \n" +
                $"# Custom Formats\n" +
                $"# ";

            string customFormats = FormatsToString(GetCustomFormatList());

            string combined = $"{prelude}\n{afterExplanationHeader}\n{content}\n{afterCustomFormatHeader}\n{customFormats}";

            string path = FormatFilePath;
            Mod.Log($"Saving format file to '{path}' (resetDefaultFormats: {resetDefaultFormats})");
            File.WriteAllText(path, combined);
        }

        private Dictionary<StatFormat, List<Stat>> CreateDefaultFormatsObject() {
            Dictionary<StatFormat, List<Stat>> formats = new Dictionary<StatFormat, List<Stat>>();
            foreach (Stat stat in AllStats) {
                foreach (StatFormat statFormat in stat.GetDefaultFormats()) {
                    formats.Add(statFormat, new List<Stat>());
                }
            }

            return formats;
        }

        public static string FormatsToString(List<StatFormat> formats) {
            string toRet = $"";

            foreach (StatFormat format in formats) {
                string formatText = format.Format;
                formatText = formatText.Replace("\n", "\\n");
                toRet += $"{format.Name}{FormatSeparator}{formatText}\n\n";
            }

            return toRet;
        }
        public static Dictionary<StatFormat, List<Stat>> ParseFormatsFile(string content) {
            Dictionary<StatFormat, List<Stat>> toRet = new Dictionary<StatFormat, List<Stat>>();

            string[] lines = content.Split(new string[] { "\n" }, StringSplitOptions.None);

            Mod.Log($"All loaded formats:");

            foreach (string line in lines) {
                if (line.Trim() == "" || line.Trim().StartsWith("#")) continue; //Empty line or comment

                string[] formatSplit = line.Trim().Split(new string[] { FormatSeparator }, StringSplitOptions.None);
                if (formatSplit.Length <= 1) {
                    //Ill-formed format detected
                    continue;
                }

                string name = formatSplit[0];
                string formatText = string.Join(FormatSeparator, formatSplit.Skip(1));

                Mod.Log($"'{name}' -> '{formatText}'", true);

                formatText = formatText.Replace("\\n", "\n");

                toRet.Add(new StatFormat(name, formatText), new List<Stat>());
            }

            return toRet;
        }
        #endregion

        #region Formatting
        public static string MissingPathFormat(string format, string id) {
            if (HideFormatsWithoutPath) {
                return "";
            }
            return format.Replace(id, MissingPathOutput);
        }
        public static string NotOnPathFormat(string format, string id, string addition = "") {
            return format.Replace(id, $"{NotOnPathOutput}{addition}");
        }
        public static string NotOnPathFormatPercent(string format, string id) {
            return NotOnPathFormat(format, id, "%");
        }
        public static string MissingValueFormat(string format, string id, string addition = "") {
            return format.Replace(id, $"{ValueNotAvailable}{addition}");
        }
        public static string MissingValueFormatPercent(string format, string id) {
            return MissingValueFormat(format, id, "%");
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
            return b ? $"true" : $"false";
        }

        public static string FormatFloat(float f) {
            return FormatDouble(f);
        }
        public static string FormatDouble(double d) {
            double res = Math.Round(d, DecimalPlaces);
            return $"{res}";
        }

        public static string GetFormattedRoomName(RoomInfo rInfo) {
            return rInfo.GetFormattedRoomName(RoomNameType);
        }
        #endregion
    }
}
