using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public static class GameData {
        private static Dictionary<string, ModUpdateInfo> modUpdateInfos;
        private static Hook modUpdaterHelperHook;
        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        
        public static void Load() {
            modUpdaterHelperHook = new Hook(typeof(ModUpdaterHelper).GetMethod("DownloadModUpdateList"), typeof(GameData).GetMethod("ModUpdaterHelperOnDownloadModUpdateList"));
            
            //typeof(ModUpdaterHelper).GetMethod("DownloadModUpdateList")?.OnHook(ModUpdaterHelperOnDownloadModUpdateList);
            modUpdateInfos = Engine.Instance.GetDynamicDataInstance().Get<Dictionary<string, ModUpdateInfo>>(nameof(modUpdateInfos));
        }

        public static void Unload() {
            Engine.Instance.GetDynamicDataInstance().Set(nameof(modUpdateInfos), modUpdateInfos);
            modUpdaterHelperHook?.Dispose();
        }
        
        public delegate Dictionary<string, ModUpdateInfo> orig_ModUpdaterHelper_DownloadModUpdateList();
        public static Dictionary<string, ModUpdateInfo> ModUpdaterHelperOnDownloadModUpdateList(orig_ModUpdaterHelper_DownloadModUpdateList orig) {
            return modUpdateInfos = orig();
        }


        private static uint getGamebananaId(string url) {
            uint gbid = 0;
            if (url.StartsWith("http://gamebanana.com/dl/") && uint.TryParse(url.Substring("http://gamebanana.com/dl/".Length), out gbid))
                return gbid;
            if (url.StartsWith("https://gamebanana.com/dl/") && uint.TryParse(url.Substring("https://gamebanana.com/dl/".Length), out gbid))
                return gbid;
            if (url.StartsWith("http://gamebanana.com/mmdl/") && uint.TryParse(url.Substring("http://gamebanana.com/mmdl/".Length), out gbid))
                return gbid;
            if (url.StartsWith("https://gamebanana.com/mmdl/") && uint.TryParse(url.Substring("https://gamebanana.com/mmdl/".Length), out gbid))
                return gbid;
            return gbid;
        }

        public static int GetModGamebananaId() {
            if (!(Engine.Scene is Level level)) {
                return 0;
            }

            AreaData areaData = AreaData.Get(level);
            string moduleName = string.Empty;
            EverestModule mapModule = null;
            if (Everest.Content.TryGet<AssetTypeMap>("Maps/" + areaData.SID, out ModAsset mapModAsset) && mapModAsset.Source != null) {
                moduleName = mapModAsset.Source.Name;
                mapModule = Everest.Modules.FirstOrDefault(module => module.Metadata?.Name == moduleName);
            }

            if (mapModule == null) {
                return 0;
            }

            if (modUpdateInfos?.TryGetValue(moduleName, out var modUpdateInfo) == true) {
                //In case that the player is using a particular version of Everest, which temporarily removed this field.
                PropertyInfo prop = typeof(ModUpdateInfo).GetProperty("GameBananaId");
                if (prop != null) {
                    return prop.GetValue(modUpdateInfo) as int? ?? 0;
                }
            }
            return 0;
        }
        public static string GetModUrl() {
            int gamebananaId = GetModGamebananaId();
            if (gamebananaId == 0) {
                return string.Empty;
            }
            return $"https://gamebanana.com/mods/{gamebananaId}\n\n";
        }
    }
}