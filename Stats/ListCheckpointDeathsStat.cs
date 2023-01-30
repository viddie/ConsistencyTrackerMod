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
        

        public static List<string> IDs = new List<string>() {
            ListCheckpointDeaths, ListCheckpointDeathsIndicator,
            ListCheckpointGoldenDeaths, ListCheckpointGoldenDeathsSession, ListCheckpointGoldenDeathsAndSession,
        };

        public ListCheckpointDeathsStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ListCheckpointDeaths);
                format = StatManager.MissingPathFormat(format, ListCheckpointDeathsIndicator);
                format = StatManager.MissingPathFormat(format, ListCheckpointGoldenDeaths);
                format = StatManager.MissingPathFormat(format, ListCheckpointGoldenDeathsSession);
                format = StatManager.MissingPathFormat(format, ListCheckpointGoldenDeathsAndSession);
                return format;
            }



            List<int> checkpointDeaths = new List<int>();
            List<int> checkpointGoldenDeaths = new List<int>();
            List<int> checkpointGoldenDeathsSession = new List<int>();
            string indicatorList = "";
            string goldenDeathsAndSessionList = "";
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                int cpDeaths = 0;
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    cpDeaths += chapterStats.GetRoom(rInfo.DebugRoomName).DeathsInCurrentRun;
                }

                checkpointDeaths.Add(cpDeaths);
                checkpointGoldenDeaths.Add(cpInfo.Stats.GoldenBerryDeaths);
                checkpointGoldenDeathsSession.Add(cpInfo.Stats.GoldenBerryDeathsSession);

                goldenDeathsAndSessionList += $"{cpInfo.Stats.GoldenBerryDeaths} ({cpInfo.Stats.GoldenBerryDeathsSession}) | ";

                string indicator = $"{cpDeaths}";
                if (cpInfo == chapterPath.CurrentRoom.Checkpoint) {
                    indicator = $">{indicator}<";
                }
                
                indicatorList += $"{indicator}/";
            }

            indicatorList = indicatorList.TrimEnd('/');
            goldenDeathsAndSessionList = goldenDeathsAndSessionList.TrimEnd(' ', '|');

            format = format.Replace(ListCheckpointDeaths, string.Join("/", checkpointDeaths));
            format = format.Replace(ListCheckpointDeathsIndicator, indicatorList);        
            
            format = format.Replace(ListCheckpointGoldenDeaths, string.Join(" | ", checkpointGoldenDeaths));
            format = format.Replace(ListCheckpointGoldenDeathsSession, string.Join(" | ", checkpointGoldenDeathsSession));
            format = format.Replace(ListCheckpointGoldenDeathsAndSession, goldenDeathsAndSessionList);

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(ListCheckpointDeaths, "Lists death count for each checkpoint in the current run (for low deaths, like '4/0/2/3/0/0')"),
                new KeyValuePair<string, string>(ListCheckpointDeathsIndicator, "Same as the above, but adds an indicator for the current checkpoint (like '4/0/2/>3</0/0')"),
                new KeyValuePair<string, string>(ListCheckpointGoldenDeaths, "Lists your total golden deathcount per checkpoint in a similar format as above"),
                new KeyValuePair<string, string>(ListCheckpointGoldenDeathsSession, "Lists your total golden deathcount per checkpoint in the current session in a similar format as above"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat("basic-low-death", $"Low Death: {ListCheckpointDeaths}"),
                new StatFormat("basic-low-death-indicator", $"Low Death: {ListCheckpointDeathsIndicator}"),
                new StatFormat("basic-golden-deaths", $"Golden Deaths: {ListCheckpointGoldenDeaths}"),
                new StatFormat("basic-golden-deaths-session", $"Golden Deaths (Session): {ListCheckpointGoldenDeathsSession}"),
                new StatFormat("basic-golden-deaths-and-session", $"Golden Deaths (Session): {ListCheckpointGoldenDeathsAndSession}"),
            };
        }
    }
}
