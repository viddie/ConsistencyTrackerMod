using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public static class Util {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        
        public static T GetPrivateProperty<T>(object obj, string propertyName, bool isField = true, bool isPublic = false) {
            if (obj == null) {
                Mod.LogVerbose($"Object is null! Property: {propertyName}");
                return default(T);
            } else if (propertyName == null) {
                Mod.LogVerbose($"Property name is null! Object: {obj.GetType().Name}");
                return default(T);
            }

            Type type = obj.GetType();

            BindingFlags flags = BindingFlags.Instance;

            if (isPublic) {
                flags |= BindingFlags.Public;
            } else {
                flags |= BindingFlags.NonPublic;
            }

            if (isField) {
                FieldInfo field = type.GetField(propertyName, flags);
                if (field == null) {
                    Mod.LogVerbose($"Field '{propertyName}' not found in {type.Name}! Available fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)field.GetValue(obj);

            } else {
                PropertyInfo property = type.GetProperty(propertyName, flags);
                if (property == null) {
                    Mod.LogVerbose($"Property '{propertyName}' not found in {type.Name}! Available Fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)property.GetValue(obj, null);
            }
        }

        public static T GetPrivateStaticProperty<T>(object obj, string propertyName, bool isField = true) {
            if (obj == null) {
                Mod.LogVerbose($"Object is null! Property: {propertyName}");
                return default(T);
            } else if (propertyName == null) {
                Mod.LogVerbose($"Property name is null! Object: {obj.GetType().Name}");
                return default(T);
            }


            Type type = obj.GetType();
            BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;

            if (isField) {
                FieldInfo field = type.GetField(propertyName, flags);
                if (field == null) {
                    Mod.LogVerbose($"Field '{propertyName}' not found in {type.Name}! Available fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)field.GetValue(null);

            } else {
                PropertyInfo property = type.GetProperty(propertyName, flags);
                if (property == null) {
                    Mod.LogVerbose($"Property '{propertyName}' not found in {type.Name}! Available Fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Static).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Static).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Static).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)property.GetValue(null, null);
            }
        }
        
        public static T GetProtectedProperty<T>(object obj, string propertyName, bool isField = true, bool isPublic = false) {
            if (obj == null) {
                Mod.LogVerbose($"Object is null! Property: {propertyName}");
                return default(T);
            } else if (propertyName == null) {
                Mod.LogVerbose($"Property name is null! Object: {obj.GetType().Name}");
                return default(T);
            }

            Type type = obj.GetType();

            BindingFlags flags = BindingFlags.Instance;

            if (isPublic) {
                flags |= BindingFlags.Public;
            } else {
                flags |= BindingFlags.NonPublic;
            }

            if (isField) {
                FieldInfo field = type.GetField(propertyName, flags);
                if (field == null) {
                    Mod.LogVerbose($"Field '{propertyName}' not found in {type.Name}! Available fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)field.GetValue(obj);

            } else {
                PropertyInfo property = type.GetProperty(propertyName, flags);
                if (property == null) {
                    Mod.LogVerbose($"Property '{propertyName}' not found in {type.Name}! Available Fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)property.GetValue(obj, null);
            }
        }

        
        /// <summary>
        /// Converts from ticks (ns) to frames (17ms)
        /// </summary>
        public static long TicksToFrames(long ticks) {
            return ticks / 10000 / 17;
        }

        /// <summary>
        /// Compares versions in the style of "1.0.0" and returns true if the first version is newer than the second.
        /// </summary>
        public static bool IsUpdateAvailable(string version, string compareTo) {
            //If any of the two version is null, assume it's not updated
            //ConsistencyTrackerModule.Instance.Log($"Comparing versions: {version} and {compareTo}");
            if (version == null || compareTo == null) return true;

            string[] versionParts = version.Split('.');
            string[] compareToParts = compareTo.Split('.');

            for (int i = 0; i < Math.Min(versionParts.Length, compareToParts.Length); i++) {
                int versionPart = int.Parse(versionParts[i]);
                int compareToPart = int.Parse(compareToParts[i]);

                if (versionPart > compareToPart) {
                    return false;
                } else if (versionPart < compareToPart) {
                    return true;
                }
            }

            return false;
        }

        public static string DictionaryToString<K, V>(Dictionary<K, V> dict, Func<V, string> valueFormatter = null) {
            if (dict == null) return "null";
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<K, V> kvp in dict) {
                string valueString = valueFormatter?.Invoke(kvp.Value) ?? kvp.Value?.ToString() ?? "null";
                sb.Append($"[{kvp.Key}] -> '{valueString}'");
            }
            return sb.ToString();
        }
    }
}