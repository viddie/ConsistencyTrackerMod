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
            RoomNameDisplayType nameFormat = StatManager.RoomNameType;

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

        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(PBBest, "Name of the PB room + the death count if it's greater than 1"),
                new KeyValuePair<string, string>(PBBestSession, $"Same as {PBBest}, but only runs in the current session"),
                new KeyValuePair<string, string>("{pb:best#<num>}", $"Same as {PBBest}, but for the <num>'s best run. {{pb:best#1}} is equivalent to {PBBest}"),
                new KeyValuePair<string, string>("{pb:bestSession#<num>}", $"Same as {PBBest} for current session, but for the <num>'s best run. {{pb:bestSession#1}} is equivalent to {PBBestSession}"),
            };
        }
        public override List<StatFormat> GetStatExamples() {
            return new List<StatFormat>() {
                new StatFormat("pb", $"Best runs: {{pb:best}} | {{pb:best#2}} | {{pb:best#3}} | {{pb:best#4}} | {{pb:best#5}}"),
                new StatFormat("pb-session", $"Best runs: {{pb:bestSession}} | {{pb:bestSession#2}} | {{pb:bestSession#3}} | {{pb:bestSession#4}} | {{pb:bestSession#5}}"),
            };
        }
    }
}
