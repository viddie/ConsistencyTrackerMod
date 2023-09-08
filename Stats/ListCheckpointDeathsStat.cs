using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     
     {path:names-json}

     'ST-1', 'ST-2', 'ST-3', 'CR-1', 'CR-2'
         */

    public class ListCheckpointDeathsStat : Stat {

        public static string ListCheckpointDeaths = "{list:checkpointDeaths}";
        public static string ListCheckpointDeathsIndicator = "{list:checkpointDeathsIndicator}";
        public static string ListCheckpointGoldenDeaths = "{list:checkpointGoldenDeaths}";
        public static string ListCheckpointGoldenDeathsSession = "{list:checkpointGoldenDeathsSession}";
        public static string ListCheckpointGoldenDeathsAndSession = "{list:checkpointGoldenDeathsAndSession}";

        public static string RunLowDeathDisplay = "{run:lowDeathDisplay}";
        public static string RunLowDeathDisplayIndicator = "{run:lowDeathDisplayIndicator}";
        public static string RunLowDeathTotal = "{run:lowDeathTotal}";


        public static List<string> IDs = new List<string>() {
            ListCheckpointDeaths, ListCheckpointDeathsIndicator,
            ListCheckpointGoldenDeaths, ListCheckpointGoldenDeathsSession, ListCheckpointGoldenDeathsAndSession,

            RunLowDeathDisplay, RunLowDeathDisplayIndicator, RunLowDeathTotal
        };

        public ListCheckpointDeathsStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ListCheckpointDeaths);
                format = StatManager.MissingPathFormat(format, ListCheckpointDeathsIndicator);
                format = StatManager.MissingPathFormat(format, ListCheckpointGoldenDeaths);
                format = StatManager.MissingPathFormat(format, ListCheckpointGoldenDeathsSession);
                format = StatManager.MissingPathFormat(format, ListCheckpointGoldenDeathsAndSession);

                format = StatManager.MissingPathFormat(format, RunLowDeathDisplay);
                format = StatManager.MissingPathFormat(format, RunLowDeathDisplayIndicator);
                format = StatManager.MissingPathFormat(format, RunLowDeathTotal);
                return format;
            }

            LowDeathBehavior lowDeathBehavior = ConsistencyTrackerModule.Instance.ModSettings.LiveDataStatLowDeathBehavior;
            if (lowDeathBehavior == LowDeathBehavior.Adaptive) {
                if (chapterPath.GameplayRoomCount < 13) {
                    lowDeathBehavior = LowDeathBehavior.AlwaysRooms;
                } else {
                    lowDeathBehavior = LowDeathBehavior.AlwaysCheckpoints;
                }
            }

            
            List<int> checkpointGoldenDeaths = new List<int>();
            List<int> checkpointGoldenDeathsSession = new List<int>();
            string goldenDeathsAndSessionList = "";
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                checkpointGoldenDeaths.Add(cpInfo.Stats.GoldenBerryDeaths);
                checkpointGoldenDeathsSession.Add(cpInfo.Stats.GoldenBerryDeathsSession);

                goldenDeathsAndSessionList += $"{cpInfo.Stats.GoldenBerryDeaths} ({cpInfo.Stats.GoldenBerryDeathsSession}) | ";
            }
            
            //Low Death
            List<List<int>> checkpointDeaths = new List<List<int>>();
            int currentCpIndex = -1;
            int currentRoomIndex = -1;
            int totalCurrentLowDeath = 0;
            for (int i = 0; i < chapterPath.Checkpoints.Count; i++) {
                CheckpointInfo cpInfo = chapterPath.Checkpoints[i];
                    
                List<int> cpDeathCounts = new List<int>();
                int cpDeaths = 0;
                for (int y = 0; y < cpInfo.GameplayRoomCount; y++) {
                    RoomInfo rInfo = cpInfo.GameplayRooms[y];

                    int deaths = chapterStats.GetRoom(rInfo.DebugRoomName).DeathsInCurrentRun;
                    cpDeaths += deaths;
                    totalCurrentLowDeath += deaths;

                    if (lowDeathBehavior == LowDeathBehavior.AlwaysCheckpoints) {
                        if (cpDeathCounts.Count == 0) {
                            cpDeathCounts.Add(deaths);
                        } else {
                            cpDeathCounts[0] += deaths;
                        }
                    } else {
                        cpDeathCounts.Add(deaths);
                    }

                    if (chapterPath.CurrentRoom != null && rInfo == chapterPath.CurrentRoom) {
                        currentRoomIndex = y;
                    }
                }

                if (currentCpIndex == -1 && chapterPath.CurrentRoom != null && cpInfo == chapterPath.CurrentRoom.Checkpoint) {
                    currentCpIndex = i;
                }

                checkpointDeaths.Add(cpDeathCounts);
            }

            List<string> cpDeathStrings = new List<string>();
            List<string> cpDeathStringsIndicator = new List<string>();
            for (int i = 0; i < checkpointDeaths.Count; i++) {
                List<int> cpDeathCounts = checkpointDeaths[i];
                string cpDeathString = "";
                string cpDeathStringIndicator = "";
                for (int y = 0; y < cpDeathCounts.Count; y++) {
                    int deaths = cpDeathCounts[y];
                    if (lowDeathBehavior == LowDeathBehavior.AlwaysRooms && i == currentCpIndex && y == currentRoomIndex) {
                        cpDeathString += $"{deaths},";
                        cpDeathStringIndicator += $">{deaths}<,";
                    } else { 
                        cpDeathString += $"{deaths},";
                        cpDeathStringIndicator += $"{deaths},";
                    }
                }
                cpDeathString = cpDeathString.TrimEnd(',');
                cpDeathStringIndicator = cpDeathStringIndicator.TrimEnd(',');

                if (lowDeathBehavior == LowDeathBehavior.AlwaysCheckpoints && i == currentCpIndex) {
                    cpDeathStrings.Add(cpDeathString);
                    cpDeathStringsIndicator.Add($">{cpDeathStringIndicator}<");
                } else {
                    cpDeathStrings.Add(cpDeathString);
                    cpDeathStringsIndicator.Add(cpDeathStringIndicator);
                }
            }
            string cpDeathList = string.Join("/", cpDeathStrings);
            string cpDeathListIndicator = string.Join("/", cpDeathStringsIndicator);
            
            format = format.Replace(ListCheckpointDeaths, cpDeathList);
            format = format.Replace(RunLowDeathDisplay, cpDeathList);
            format = format.Replace(ListCheckpointDeathsIndicator, cpDeathListIndicator);
            format = format.Replace(RunLowDeathDisplayIndicator, cpDeathListIndicator);
            format = format.Replace(RunLowDeathTotal, totalCurrentLowDeath.ToString());

            format = format.Replace(ListCheckpointGoldenDeaths, string.Join(" | ", checkpointGoldenDeaths));
            format = format.Replace(ListCheckpointGoldenDeathsSession, string.Join(" | ", checkpointGoldenDeathsSession));
            goldenDeathsAndSessionList = goldenDeathsAndSessionList.TrimEnd(' ', '|');
            format = format.Replace(ListCheckpointGoldenDeathsAndSession, goldenDeathsAndSessionList);

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(ListCheckpointDeaths, $"{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_LIST_CHECKPOINT_DEATHS_1")} {RunLowDeathDisplay}{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_LIST_CHECKPOINT_DEATHS_2")}"),
                new KeyValuePair<string, string>(ListCheckpointDeathsIndicator, $"{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_LIST_CHECKPOINT_DEATHS_INDICATOR_1")} {RunLowDeathDisplayIndicator}{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_LIST_CHECKPOINT_DEATHS_INDICATOR_2")}"),
                new KeyValuePair<string, string>(ListCheckpointGoldenDeaths, Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_LIST_CHECKPOINT_GOLDEN_DEATHS")),
                new KeyValuePair<string, string>(ListCheckpointGoldenDeathsSession, Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_LIST_CHECKPOINT_GOLDEN_DEATHS_SESSION")),

                new KeyValuePair<string, string>(RunLowDeathDisplay, Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_RUN_LOW_DEATH_DISPLAY")),
                new KeyValuePair<string, string>(RunLowDeathDisplayIndicator, Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_RUN_LOW_DEATH_DISPLAY_INDICATOR")),
                new KeyValuePair<string, string>(RunLowDeathTotal, Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_EXPLANATIONS_RUN_LOW_DEATH_TOTAL")),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_TITLE_BASIC_LOW_DEATH"), $"{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_CONTENT_BASIC_LOW_DEATH")}: {ListCheckpointDeaths}"),
                new StatFormat(Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_TITLE_BASIC_LOW_DEATH_INDICATOR"), $"{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_CONTENT_BASIC_LOW_DEATH_INDICATOR")}: {ListCheckpointDeathsIndicator}"),
                new StatFormat(Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_TITLE_BASIC_GOLDEN_DEATHS"), $"{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_CONTENT_BASIC_GOLDEN_DEATHS")}: {ListCheckpointGoldenDeaths}"),
                new StatFormat(Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_TITLE_BASIC_GOLDEN_DEATHS_SESSION"), $"{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_CONTENT_BASIC_GOLDEN_DEATHS_SESSION")}: {ListCheckpointGoldenDeathsSession}"),
                new StatFormat(Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_TITLE_BASIC_GOLDEN_DEATHS_AND_SESSION"), $"{Dialog.Clean("CCT_STAT_LIST_CHECKPOINT_DEATH_FORMAT_CONTENT_BASIC_GOLDEN_DEATHS_AND_SESSION")}: {ListCheckpointGoldenDeathsAndSession}"),
            };
        }
    }
}
