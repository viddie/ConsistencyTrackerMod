using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Stats {
    public class ValuePlaceholder<T> {

        //Pattern e.g. string pattern = @"\{chapter:averageRunDistanceSession#(.*?)\}"
        public string Pattern;

        //Pattern e.g. string pattern = "{chapter:averageRunDistanceSession#(.*?)}"
        public string Placeholder;

        public ValuePlaceholder(string pattern, string placeholder) {
            Pattern = pattern;
            Placeholder = placeholder;
        }

        public bool HasMatch(string format) {
            return Regex.IsMatch(format, Pattern);
        }


        public List<T> GetMatchList(string format) {
            List<T> matchList = new List<T>();

            if (!HasMatch(format)) return matchList;

            //ConsistencyTrackerModule.Instance.Log($"[{nameof(ValuePlaceholder<T>)}] [{nameof(GetMatchList)}] Building match list...");

            MatchCollection matches = Regex.Matches(format, Pattern);
            foreach (Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string matchedValue = match.Groups[i].Value;
                    try {
                        T convertedValue = (T)Convert.ChangeType(matchedValue, typeof(T));

                        if (!matchList.Contains(convertedValue))
                            matchList.Add(convertedValue);

                    } catch (FormatException) {
                        //ConsistencyTrackerModule.Instance.Log($"[{nameof(ValuePlaceholder<T>)}] [{nameof(GetMatchList)}] Exception when converting string '{matchedValue}' to type '{typeof(T)}': Incorrect format of input");
                    } catch (Exception) {
                        //ConsistencyTrackerModule.Instance.Log($"[{nameof(ValuePlaceholder<T>)}] [{nameof(GetMatchList)}] Exception when converting string '{matchedValue}' to type '{typeof(T)}': {ex.Message}");
                    }
                }
            }

            //ConsistencyTrackerModule.Instance.Log($"[{nameof(ValuePlaceholder<T>)}] [{nameof(GetMatchList)}] Resulting match list: [{string.Join(", ", matchList)}]");

            return matchList;
        }
        public string ReplaceMatchException(string format) {
            //ConsistencyTrackerModule.Instance.Log($"Replacing errors in match...");

            MatchCollection matches = Regex.Matches(format, Pattern);
            foreach (Match match in matches) {
                for (int i = 1; i < match.Groups.Count; i++) {
                    string matchedValue = match.Groups[i].Value;
                    try {
                        T convertedValue = (T)Convert.ChangeType(matchedValue, typeof(T));
                    } catch (FormatException) {
                        //ConsistencyTrackerModule.Instance.Log($"Replacing '{GetPlaceholder(matchedValue)}' with '<Incorrect format>'");
                        format = format.Replace(GetPlaceholder(matchedValue), $"<Incorrect format>");
                    } catch (Exception ex) {
                        //ConsistencyTrackerModule.Instance.Log($"Replacing '{GetPlaceholder(matchedValue)}' with '<Error: {ex.Message}>'");
                        format = format.Replace(GetPlaceholder(matchedValue), $"<Error: {ex.Message}>");
                    }
                }
            }
            return format;
        }


        public string GetPatternWithValue(T value) {
            return Pattern.Replace($"(.*?)", value.ToString());
        }
        public string GetPatternWithValue(string text) {
            return Pattern.Replace($"(.*?)", text);
        }

        public string GetPlaceholder(T value) {
            return Placeholder.Replace($"(.*?)", value.ToString());
        }
        public string GetPlaceholder(string text) {
            return Placeholder.Replace($"(.*?)", text);
        }

        public string ReplaceAll(string format, string replacement) {
            return Regex.Replace(format, Pattern, replacement);
        }
        public string ValueReplace(string format, T value, string replacement) {
            return format.Replace(GetPlaceholder(value), replacement);
        }
        public string ValueReplaceAll(string format, Dictionary<T, string> values) {
            foreach (T value in values.Keys) {
                format = ValueReplace(format, value, values[value]);
            }
            return format;
        }
    }
}
