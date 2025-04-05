using System;
using System.Collections.Generic;
using MonoMod.ModInterop;

namespace Celeste.Mod.ConsistencyTracker.ThirdParty {
    public static class SpeedrunToolSupport {
        
        private static bool SpeedrunToolInstalled;
        private static object Action;

        public static void Load() {
            typeof(SpeedrunToolImport).ModInterop();
            SpeedrunToolInstalled = SpeedrunToolImport.RegisterSaveLoadAction != null;

            if (!SpeedrunToolInstalled) {
                ConsistencyTrackerModule.Instance.Log("SpeedunTool is not installed.");
                return;
            }
            ConsistencyTrackerModule.Instance.Log("SpeedunTool was loaded!");
            
            Action = SpeedrunToolImport.RegisterSaveLoadAction(SaveState, LoadState, ClearState, null, null, null);
        }

        public static void Unload() {
            if (!SpeedrunToolInstalled) return; 
            SpeedrunToolImport.Unregister(Action);
        }

        private static void SaveState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            //Logger.Log(nameof(ConsistencyTrackerModule), "saveState called!");
            ConsistencyTrackerModule.Instance.SpeedrunToolSaveState(savedvalues, level);
        }

        private static void LoadState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            //Logger.Log(nameof(ConsistencyTrackerModule), "loadState called!");
            ConsistencyTrackerModule.Instance.SpeedrunToolLoadState(savedvalues, level);
        }

        private static void ClearState() {
            //Logger.Log(nameof(ConsistencyTrackerModule), "clearState called!");
            ConsistencyTrackerModule.Instance.SpeedrunToolClearState();
        }
    }
    
    [ModImportName("SpeedrunTool.SaveLoad")]
    internal static class SpeedrunToolImport {
    
        public static Func<Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action<Dictionary<Type, Dictionary<string, object>>, Level>, Action, Action<Level>, Action<Level>, Action, object> RegisterSaveLoadAction;
    
        public static Func<Type, string[], object> RegisterStaticTypes;
    
        public static Action<object> Unregister;
    
        public static Func<object, object> DeepClone;
    }
}
