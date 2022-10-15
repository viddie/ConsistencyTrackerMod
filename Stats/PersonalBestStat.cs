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
     {pb:best}
     {pb:bestSession}

     {pb:best#<number>}
     {pb:bestSession#<number>}

     {run:currentPbStatus}          - Format e.g. "Current run: 75. best run", "Current run: 4. best run", "Current run: PB"
     {run:currentPbStatusPercent}   - Format e.g. "Current run better than 0% of all runs", "Current run better than 72.39% of all runs", "Current run better than 100% of all runs"

         */

    public class PersonalBestStat : Stat {

        public static string PBBest = "{pb:best}";
        public static string PBBestSession = "{pb:bestSession}";
        public static List<string> IDs = new List<string>() { PBBest, PBBestSession };

        public static string NumberPattern = @"\{pb:best#(.*?)\}";
        public static string NumberPatternSession = @"\{pb:bestSession#(.*?)\}";

        public PersonalBestStat() : base(IDs) { }

        public override bool ContainsIdentificator(string format) {
            if (format.Contains(PBBest) || format.Contains(PBBestSession)) return true;

            return Regex.IsMatch(format, NumberPattern) || Regex.IsMatch(format, NumberPatternSession);
        }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, PBBest);
                format = StatManager.MissingPathFormat(format, PBBestSession);
                format = Regex.Replace(format, NumberPattern, StatManager.MissingPathOutput);
                format = Regex.Replace(format, NumberPatternSession, StatManager.MissingPathOutput);
                return format;
            }

            Dictionary<string, string> invalidFormats = new Dictionary<string, string>();

            Dictionary<int, string> pbRoomsToFormat = new Dictionary<int, string>();
            Dictionary<int, string> pbRoomsToFormatSession = new Dictionary<int, string>();

            if (format.Contains(PBBest)) pbRoomsToFormat.Add(1, null);
            if (format.Contains(PBBestSession)) pbRoomsToFormatSession.Add(1, null);


            var matches = Regex.Matches(format, NumberPattern);
            foreach(Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string pbNumberStr = match.Groups[i].Value;
                    int pbNumberInt;
                    try {
                        pbNumberInt = int.Parse(pbNumberStr);
                        if (pbNumberInt < 1) throw new ArgumentException();

                        if (!pbRoomsToFormat.ContainsKey(pbNumberInt))
                            pbRoomsToFormat.Add(pbNumberInt, null);

                    } catch (FormatException) {
                        if (invalidFormats.ContainsKey($"{{pb:best#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:best#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    } catch (ArgumentException) {
                        if (invalidFormats.ContainsKey($"{{pb:best#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:best#{match.Groups[i].Value}}}", $"<PB number must be 1 or greater: {match.Groups[i].Value}>");
                    } catch (Exception) {
                        if (invalidFormats.ContainsKey($"{{pb:best#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:best#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    }
                }
            }

            matches = Regex.Matches(format, NumberPatternSession);
            foreach (Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string pbNumberStr = match.Groups[i].Value;
                    int pbNumberInt;
                    try {
                        pbNumberInt = int.Parse(pbNumberStr);
                        if (pbNumberInt < 1) throw new ArgumentException();

                        if (!pbRoomsToFormatSession.ContainsKey(pbNumberInt))
                            pbRoomsToFormatSession.Add(pbNumberInt, null);
                    } catch (FormatException) {
                        if (invalidFormats.ContainsKey($"{{pb:bestSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestSession#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    } catch (ArgumentException) {
                        if (invalidFormats.ContainsKey($"{{pb:bestSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestSession#{match.Groups[i].Value}}}", $"<PB number must be 1 or greater: {match.Groups[i].Value}>");
                    } catch (Exception) {
                        if (invalidFormats.ContainsKey($"{{pb:bestSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestSession#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    }
                }
            }




            int pbNumber = 0;
            int pbNumberSession = 0;
            RoomNameDisplayType nameFormat = ConsistencyTrackerModule.Instance.ModSettings.LiveDataPBDisplayNameType;

            //Walk the path BACKWARDS (d1d7 reference???)
            for (int cpIndex = chapterPath.Checkpoints.Count - 1; cpIndex >= 0; cpIndex--) {
                CheckpointInfo cpInfo = chapterPath.Checkpoints[cpIndex];

                for (int roomIndex = cpInfo.Rooms.Count - 1; roomIndex >= 0; roomIndex--) {
                    RoomInfo rInfo = cpInfo.Rooms[roomIndex];

                    int goldenDeaths = chapterStats.GetRoom(rInfo.DebugRoomName).GoldenBerryDeaths;
                    int goldenDeathsSession = chapterStats.GetRoom(rInfo.DebugRoomName).GoldenBerryDeathsSession;

                    if (goldenDeaths > 0) {
                        string roomName = rInfo.GetFormattedRoomName(nameFormat);
                        if (goldenDeaths > 1) roomName = $"{roomName} x{goldenDeaths}";

                        pbNumber++;
                        if (pbRoomsToFormat.ContainsKey(pbNumber)) {
                            pbRoomsToFormat[pbNumber] = roomName;
                        }
                    }

                    if (goldenDeathsSession > 0) {
                        string roomName = rInfo.GetFormattedRoomName(nameFormat);
                        if (goldenDeathsSession > 1) roomName = $"{roomName} x{goldenDeathsSession}";

                        pbNumberSession++;
                        if (pbRoomsToFormatSession.ContainsKey(pbNumberSession)) {
                            pbRoomsToFormatSession[pbNumberSession] = roomName;
                        }
                    }
                }
            }

            //Output requested best runs
            foreach (int pb in pbRoomsToFormat.Keys) {
                string formatted = pbRoomsToFormat[pb];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{pb:best#{pb}}}", formatted);
                if(pb == 1) format = format.Replace($"{{pb:best}}", formatted);
            }
            foreach (int pb in pbRoomsToFormatSession.Keys) {
                string formatted = pbRoomsToFormatSession[pb];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{pb:bestSession#{pb}}}", formatted);
                if (pb == 1) format = format.Replace($"{{pb:bestSession}}", formatted);
            }

            //Output formatting errors
            foreach (KeyValuePair<string, string> kvPair in invalidFormats) {
                format = format.Replace(kvPair.Key, kvPair.Value);
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        public string JoinMatchGroupValues(Match match, string sep = ", ") {
            List<string> values = new List<string>();

            for (int i = 0; i < match.Groups.Count; i++) {
                values.Add(match.Groups[0].Value);
            }

            return string.Join(sep, values);
        }
    }
}
