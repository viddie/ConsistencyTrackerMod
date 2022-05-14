using System;
using System.Collections.Generic;

namespace Celeste.Mod.ConsistencyTracker.ThirdParty {
    public static class SpeedrunToolSupport {
        private static bool Loaded;

        public static void Load() {
            // don't load twice
            if (Loaded) return;
            Loaded = true;

            var action = new SpeedrunTool.SaveLoad.SaveLoadAction(SaveState, LoadState, ClearState);
            SpeedrunTool.SaveLoad.SaveLoadAction.Add(action);
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
}
