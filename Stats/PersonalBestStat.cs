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
     {pb:bestRoomNumber}
     {pb:bestRoomNumberSession}

     {pb:best#<number>}
     {pb:bestSession#<number>}

     {pb:bestRoomNumber#<number>}
     {pb:bestRoomNumberSession#<number>}
         */

    public class PersonalBestStat : Stat {

        public static string PBBest = "{pb:best}";
        public static string PBBestSession = "{pb:bestSession}";
        public static string PBBestRoomNumber = "{pb:bestRoomNumber}";
        public static string PBBestRoomNumberSession = "{pb:bestRoomNumberSession}";
        public static List<string> IDs = new List<string>() { PBBest, PBBestSession, PBBestRoomNumber, PBBestRoomNumberSession };

        public static string BestPattern = @"\{pb:best#(.*?)\}";
        public static string BestPatternSession = @"\{pb:bestSession#(.*?)\}";

        public static string RoomNumberPattern = @"\{pb:bestRoomNumber#(.*?)\}";
        public static string RoomNumberPatternSession = @"\{pb:bestRoomNumberSession#(.*?)\}";

        public PersonalBestStat() : base(IDs) { }

        public override bool ContainsIdentificator(string format) {
            if (IDs.Find(pattern => format.Contains(pattern)) != null) return true;

            return Regex.IsMatch(format, BestPattern) || Regex.IsMatch(format, BestPatternSession) || Regex.IsMatch(format, RoomNumberPattern) || Regex.IsMatch(format, RoomNumberPatternSession);
        }

        public override string FormatStat(PathInfo path, ChapterStats stats, string format) {
            if (path == null) {
                format = StatManager.MissingPathFormat(format, PBBest);
                format = StatManager.MissingPathFormat(format, PBBestSession);
                format = Regex.Replace(format, BestPattern, StatManager.MissingPathOutput);
                format = Regex.Replace(format, BestPatternSession, StatManager.MissingPathOutput);

                format = StatManager.MissingPathFormat(format, PBBestRoomNumber);
                format = StatManager.MissingPathFormat(format, PBBestRoomNumberSession);
                format = Regex.Replace(format, RoomNumberPattern, StatManager.MissingPathOutput);
                format = Regex.Replace(format, RoomNumberPatternSession, StatManager.MissingPathOutput);
                return format;
            }

            Dictionary<string, string> invalidFormats = new Dictionary<string, string>();

            Dictionary<int, string> pbRoomsToFormat = new Dictionary<int, string>();
            Dictionary<int, string> pbRoomsToFormatSession = new Dictionary<int, string>();

            Dictionary<int, string> pbRoomNumbersToFormat = new Dictionary<int, string>();
            Dictionary<int, string> pbRoomNumbersToFormatSession = new Dictionary<int, string>();


            if (format.Contains(PBBest)) pbRoomsToFormat.Add(1, null);
            if (format.Contains(PBBestSession)) pbRoomsToFormatSession.Add(1, null);

            if (format.Contains(PBBestRoomNumber)) pbRoomNumbersToFormat.Add(1, null);
            if (format.Contains(PBBestRoomNumberSession)) pbRoomNumbersToFormatSession.Add(1, null);


            var matches = Regex.Matches(format, BestPattern);
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

            matches = Regex.Matches(format, BestPatternSession);
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

            matches = Regex.Matches(format, RoomNumberPattern);
            foreach (Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string pbNumberStr = match.Groups[i].Value;
                    int pbNumberInt;
                    try {
                        pbNumberInt = int.Parse(pbNumberStr);
                        if (pbNumberInt < 1) throw new ArgumentException();

                        if (!pbRoomNumbersToFormat.ContainsKey(pbNumberInt))
                            pbRoomNumbersToFormat.Add(pbNumberInt, null);
                    } catch (FormatException) {
                        if (invalidFormats.ContainsKey($"{{pb:bestRoomNumber#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestRoomNumber#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    } catch (ArgumentException) {
                        if (invalidFormats.ContainsKey($"{{pb:bestRoomNumber#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestRoomNumber#{match.Groups[i].Value}}}", $"<PB number must be 1 or greater: {match.Groups[i].Value}>");
                    } catch (Exception) {
                        if (invalidFormats.ContainsKey($"{{pb:bestRoomNumber#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestRoomNumber#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    }
                }
            }

            matches = Regex.Matches(format, RoomNumberPatternSession);
            foreach (Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string pbNumberStr = match.Groups[i].Value;
                    int pbNumberInt;
                    try {
                        pbNumberInt = int.Parse(pbNumberStr);
                        if (pbNumberInt < 1) throw new ArgumentException();

                        if (!pbRoomNumbersToFormatSession.ContainsKey(pbNumberInt))
                            pbRoomNumbersToFormatSession.Add(pbNumberInt, null);
                    } catch (FormatException) {
                        if (invalidFormats.ContainsKey($"{{pb:bestRoomNumberSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestRoomNumberSession#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    } catch (ArgumentException) {
                        if (invalidFormats.ContainsKey($"{{pb:bestRoomNumberSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestRoomNumberSession#{match.Groups[i].Value}}}", $"<PB number must be 1 or greater: {match.Groups[i].Value}>");
                    } catch (Exception) {
                        if (invalidFormats.ContainsKey($"{{pb:bestRoomNumberSession#{match.Groups[i].Value}}}")) continue;
                        invalidFormats.Add($"{{pb:bestRoomNumberSession#{match.Groups[i].Value}}}", $"<Invalid PB number value: {match.Groups[i].Value}>");
                    }
                }
            }




            int pbNumber = 0;
            int pbNumberSession = 0;
            RoomNameDisplayType nameFormat = StatManager.RoomNameType;
            int winRoomNumber = path.GetWinRoomNumber() - 1;

            //Check for wins first
            if (stats.GoldenCollectedCount > 0) {
                pbNumber++;
                
                if (pbRoomsToFormat.ContainsKey(pbNumber)) {
                    string roomName = StatManager.GetWinningRoomName(stats);
                    if (stats.GoldenCollectedCount > 1) roomName = $"{roomName} x{stats.GoldenCollectedCount}";
                    pbRoomsToFormat[pbNumber] = roomName;
                }
                if (pbRoomNumbersToFormat.ContainsKey(pbNumber)) {
                    pbRoomNumbersToFormat[pbNumber] = $"{winRoomNumber}";
                }
            }
            if (stats.GoldenCollectedCountSession > 0) {
                pbNumberSession++;
                
                if (pbRoomsToFormatSession.ContainsKey(pbNumberSession)) {
                    string roomName = StatManager.GetWinningRoomName(stats);
                    if (stats.GoldenCollectedCountSession > 1) roomName = $"{roomName} x{stats.GoldenCollectedCountSession}";
                    pbRoomsToFormatSession[pbNumberSession] = roomName;
                }
                if (pbRoomNumbersToFormatSession.ContainsKey(pbNumberSession)) {
                    pbRoomNumbersToFormatSession[pbNumberSession] = $"{winRoomNumber}";
                }
            }

            //Walk the path BACKWARDS (d1d7 reference???) 
            for (int cpIndex = path.Checkpoints.Count - 1; cpIndex >= 0; cpIndex--) {
                CheckpointInfo cpInfo = path.Checkpoints[cpIndex];
                List<RoomInfo> rooms = cpInfo.Rooms;

                for (int roomIndex = rooms.Count - 1; roomIndex >= 0; roomIndex--) {
                    RoomInfo rInfo = rooms[roomIndex];
                    RoomStats rStats = stats.GetRoom(rInfo);

                    int goldenDeaths = rStats.GoldenBerryDeaths;
                    int goldenDeathsSession = rStats.GoldenBerryDeathsSession;

                    if (goldenDeaths > 0) {
                        string roomName = rInfo.GetFormattedRoomName(nameFormat);
                        if (goldenDeaths > 1) roomName = $"{roomName} x{goldenDeaths}";

                        pbNumber++;
                        if (pbRoomsToFormat.ContainsKey(pbNumber)) {
                            pbRoomsToFormat[pbNumber] = roomName;
                        }
                        if (pbRoomNumbersToFormat.ContainsKey(pbNumber)) {
                            pbRoomNumbersToFormat[pbNumber] = $"{rInfo.RoomNumberInChapter}";
                        }
                    }

                    if (goldenDeathsSession > 0) {
                        string roomName = rInfo.GetFormattedRoomName(nameFormat);
                        if (goldenDeathsSession > 1) roomName = $"{roomName} x{goldenDeathsSession}";

                        pbNumberSession++;
                        if (pbRoomsToFormatSession.ContainsKey(pbNumberSession)) {
                            pbRoomsToFormatSession[pbNumberSession] = roomName;
                        }
                        if (pbRoomNumbersToFormatSession.ContainsKey(pbNumberSession)) {
                            pbRoomNumbersToFormatSession[pbNumberSession] = $"{rInfo.RoomNumberInChapter}";
                        }
                    }
                }
            }

            //Output requested best runs
            //Best runs
            foreach (int pb in pbRoomsToFormat.Keys) {
                string formatted = pbRoomsToFormat[pb];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{pb:best#{pb}}}", formatted);
                if (pb == 1) format = format.Replace($"{{pb:best}}", formatted);
            }
            //Best runs session
            foreach (int pb in pbRoomsToFormatSession.Keys) {
                string formatted = pbRoomsToFormatSession[pb];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{pb:bestSession#{pb}}}", formatted);
                if (pb == 1) format = format.Replace($"{{pb:bestSession}}", formatted);
            }

            //Room numbers of best runs
            foreach (int pb in pbRoomNumbersToFormat.Keys) {
                string formatted = pbRoomNumbersToFormat[pb];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{pb:bestRoomNumber#{pb}}}", formatted);
                if (pb == 1) format = format.Replace($"{{pb:bestRoomNumber}}", formatted);
            }
            //Room numbers of best runs session
            foreach (int pb in pbRoomNumbersToFormatSession.Keys) {
                string formatted = pbRoomNumbersToFormatSession[pb];
                if (formatted == null) formatted = "-";

                format = format.Replace($"{{pb:bestRoomNumberSession#{pb}}}", formatted);
                if (pb == 1) format = format.Replace($"{{pb:bestRoomNumberSession}}", formatted);
            }

            //Output formatting errors
            foreach (KeyValuePair<string, string> kvPair in invalidFormats) {
                format = format.Replace(kvPair.Key, kvPair.Value);
            }

            return format;
        }

        public static RoomInfo GetFurthestDeathRoom(PathInfo path, ChapterStats stats) {
            RoomInfo pbRoom = null;

            for (int i = path.Checkpoints.Count - 1; i >= 0; i--) {
                CheckpointInfo cpInfo = path.Checkpoints[i];

                for (int j = cpInfo.Rooms.Count - 1; j >= 0; j--) {
                    RoomInfo rInfo = cpInfo.Rooms[j];
                    RoomStats rStats = stats.GetRoom(rInfo);
                    
                    if (rStats.GoldenBerryDeaths > 0) {
                        pbRoom = rInfo;
                        break;
                    }
                }

                if (pbRoom != null) break;
            }

            return pbRoom;
        }
        public static Tuple<RoomInfo, int> GetSessionPBRoomFromLastRuns(PathInfo path, List<string> lastRuns) {
            bool isWin = false;
            RoomInfo pbRoom = null;
            int pbRoomNumber = 0;
            int deaths = 0;

            List<RoomInfo> rooms = path.GetRoomsForLastRuns(lastRuns);
            foreach (RoomInfo rInfo in rooms) {
                int roomNumber = path.GetRunRoomNumberInChapter(rInfo);
                if (roomNumber == 0) continue; //Not on path

                if (pbRoom == null || roomNumber > pbRoomNumber) {
                    pbRoom = rInfo;
                    pbRoomNumber = roomNumber;
                    deaths = 1;
                    
                    if (rInfo == null) { //rInfo being null means the run was a win
                        isWin = true;
                    }
                    
                } else if (roomNumber == pbRoomNumber) {
                    deaths++;
                }
            }

            if (isWin == false && pbRoom == null) return null;
            return Tuple.Create(pbRoom, deaths);
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(PBBest, Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST")),
                new KeyValuePair<string, string>(PBBestSession, $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_SESSION_1")} {PBBest}{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_SESSION_2")}"),
                new KeyValuePair<string, string>("{pb:best#<num>}", $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_NUM_1")} {PBBest}{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_NUM_2")} {{pb:best#1}} {Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_NUM_3")} {PBBest}"),
                new KeyValuePair<string, string>("{pb:bestSession#<num>}", $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_NUM_SESSION_1")} {PBBest} {Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_NUM_SESSION_2")} {{pb:bestSession#1}} {Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_NUM_SESSION_3")} {PBBestSession}"),
                new KeyValuePair<string, string>(PBBestRoomNumber, Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER")),
                new KeyValuePair<string, string>(PBBestRoomNumberSession, $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_SESSION_1")} {PBBestRoomNumber}{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_SESSION_2")}"),
                new KeyValuePair<string, string>("{pb:bestRoomNumber#<num>}", $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_NUM_1")} {PBBestRoomNumber}{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_NUM_2")} {{pb:bestRoomNumber#1}} {Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_NUM_3")} {PBBestRoomNumber}"),
                new KeyValuePair<string, string>("{pb:bestRoomNumberSession#<num>}", $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_NUM_SESSION_1")} {PBBestRoomNumber} {Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_NUM_SESSION_2")} {{pb:bestRoomNumberSession#1}} {Dialog.Clean("CCT_STAT_PERSONAL_BEST_EXPLANATIONS_PB_BEST_ROOM_NUMBER_NUM_SESSION_3")} {PBBestRoomNumberSession}"),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_TITLE_BASIC_PBS"), $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_CONTENT_BASIC_PBS")}: {PBBest} | {{pb:best#2}} | {{pb:best#3}} | {{pb:best#4}} | {{pb:best#5}}"),
                new StatFormat(Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_TITLE_BASIC_PBS_SESSION"), $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_CONTENT_BASIC_PBS_SESSION")}: {PBBestSession} | {{pb:bestSession#2}} | {{pb:bestSession#3}} | {{pb:bestSession#4}} | {{pb:bestSession#5}}"),
                new StatFormat(Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_TITLE_BASIC_PBS_COMBINED"), $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_CONTENT_BASIC_PBS_COMBINED_1")}: {PBBest} | {{pb:best#2}} | {{pb:best#3}} | {{pb:best#4}} | {{pb:best#5}}\\n" 
                                             + $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_CONTENT_BASIC_PBS_COMBINED_2")}: {PBBestSession} | {{pb:bestSession#2}} | {{pb:bestSession#3}} | {{pb:bestSession#4}} | {{pb:bestSession#5}}"),
                new StatFormat(Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_TITLE_BASIC_PB_ONLY"), $"{Dialog.Clean("CCT_STAT_PERSONAL_BEST_FORMAT_CONTENT_BASIC_PB_ONLY")}: {PBBest} ({PBBestRoomNumber}/{LiveProgressStat.ChapterRoomCount})"),
            };
        }
    }
}
