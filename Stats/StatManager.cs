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
                Formats = new Dictionary<StatFormat, List<Stat>>() {
                    [new StatFormat("successRate", "Room SR: {room:successRate} | CP: {checkpoint:successRate} | Total: {chapter:successRate}")] = new List<Stat>() { },
                    [new StatFormat("pb", "PB: {pb:best} | Session: {pb:bestSession}")] = new List<Stat>() { },
                    [new StatFormat("color-tracker", "Red: {chapter:color-red}, Yellow: {chapter:color-yellow}, Green: {chapter:color-green}, Light-Green: {chapter:color-lightGreen}")] = new List<Stat>() { },
                    [new StatFormat("chokeRateRoom", "Room Choke Rate: {room:chokeRate} (CP: {checkpoint:chokeRate})")] = new List<Stat>() { },
                };

                string content = FormatsToFile(Formats);
                File.WriteAllText(formatFilePath, content);
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
            pathInfo.Stats = new AggregateStats();

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
                }


                pathInfo.Stats.CountAttempts += cpInfo.Stats.CountAttempts;
                pathInfo.Stats.CountSuccesses += cpInfo.Stats.CountSuccesses;
                pathInfo.Stats.CountGoldenBerryDeaths += cpInfo.Stats.CountGoldenBerryDeaths;
                pathInfo.Stats.CountGoldenBerryDeathsSession += cpInfo.Stats.CountGoldenBerryDeathsSession;

                pathInfo.Stats.GoldenChance *= cpInfo.Stats.GoldenChance;
            }
        }

        public static string MissingPathFormat(string format, string id) {
            return format.Replace(id, MissingPathOutput);
        }


        public static string FormatsToFile(Dictionary<StatFormat, List<Stat>> formats) {
            string toRet = "";

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
                string[] formatSplit = line.Split(new char[] { ';' });
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
    }
}
