using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class LogUtils {
        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        private static readonly Dictionary<string, int> CallCounts = new Dictionary<string, int>();


        public static void LogOnce(string message, int frameBack = 1) {
            LogNTimes(message, 1, frameBack + 1);
        }
        public static void LogNTimes(string message, int n, int frameBack = 1) {
            string key = GetCallingKey(frameBack + 1);
            int callCount = CallCounts.ContainsKey(key) ? CallCounts[key] : 0;
            if (callCount < n) {
                int remaining = n - callCount;
                Mod.Log($"({remaining}) "+message, frameBack:frameBack + 1);
                CallCounts[key] = callCount + 1;
            }
        }

        public static void LogEveryN(string message, int n, int frameBack = 1){
            string key = GetCallingKey(frameBack + 1);
            int callCount = CallCounts.ContainsKey(key) ? CallCounts[key] : 0;
            if (callCount % n == 0) {
                Mod.Log(message, frameBack: frameBack + 1);
            }
            CallCounts[key] = callCount + 1;
        }

        public static string GetCallingKey(int frameBack) {
            StackFrame frame = new StackTrace().GetFrame(frameBack);
            string typeName = frame.GetMethod().DeclaringType.Name;
            string methodName = frame.GetMethod().Name;
            string key = $"{typeName}.{methodName}.O{frame.GetILOffset()}";
            return key;
        }

    }
}
