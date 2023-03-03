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
                    Mod.Log($"Field '{propertyName}' not found in {type.Name}! Available fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)field.GetValue(obj);

            } else {
                PropertyInfo property = type.GetProperty(propertyName, flags);
                if (property == null) {
                    Mod.Log($"Property '{propertyName}' not found in {type.Name}! Available Fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]");
                    return default(T);
                }
                return (T)property.GetValue(obj, null);
            }
        }
    }
}
