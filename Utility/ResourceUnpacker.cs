using System.Collections.Generic;
using System.IO;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class ResourceUnpacker {
        private static ConsistencyTrackerModule Mod = ConsistencyTrackerModule.Instance;
        
        public static void CheckPrepackagedPaths(bool reset=false) {
            string assetPath = "Assets/DefaultPaths";
            List<string> sideNames = new List<string>() { "Normal", "BSide", "CSide" };
            List<string> levelNames = new List<string>() {
                "1-ForsakenCity",
                "2-OldSite",
                "3-CelestialResort",
                "4-GoldenRidge",
                "5-MirrorTemple",
                "6-Reflection",
                "7-Summit",
                "9-Core",
            };
            string farewellLevelName = "Celeste_LostLevels_Normal";

            foreach (string level in levelNames) {
                foreach (string side in sideNames) {
                    string levelName = $"Celeste_{level}_{side}";
                    Mod.LogVerbose($"Checking path file '{levelName}'...");
                    CheckDefaultPathFile(levelName, $"{assetPath}/{levelName}.json", reset);
                }
            }

            CheckDefaultPathFile(farewellLevelName, $"{assetPath}/{farewellLevelName}.json", reset);
        }
        private static void CheckDefaultPathFile(string levelName, string assetPath, bool reset=false) {
            string nameTXT = $"{levelName}.txt";
            string nameJSON = $"{levelName}.json";
            string pathTXT = GetPathToFile(PathsFolder, nameTXT);
            string pathJSON = GetPathToFile(PathsFolder, nameJSON);

            if (File.Exists(pathTXT) && !File.Exists(pathJSON)) {
                File.Move(pathTXT, pathJSON);
            }
            if (File.Exists(pathTXT) && File.Exists(pathJSON)) {
                File.Delete(pathTXT);
            }

            if (!File.Exists(pathJSON) || reset) {
                CreatePathFileFromStream(nameJSON, assetPath);
            } else {
                Mod.LogVerbose($"Path file '{nameJSON}' already exists, skipping");
            }
        }

        public static void UpdateExternalTools() {
            Mod.Log($"Checking for external tool updates...");

            string basePath = "Assets";

            string commonJsName = "common.js";
            List<string> externalOverlayFiles = new List<string>() {
                    "CCTOverlay.html",
                    "CCTOverlay.js",
                    "CCTOverlay.css",
                    "img/goldberry.gif"
            };
            string externalOverlayFolder = $"ExternalOverlay";

            List<string> livedataEditorFiles = new List<string>() {
                    "LiveDataEditTool.html",
                    "LiveDataEditTool.js",
                    "LiveDataEditTool.css",
            };
            string livedataEditorFolder = $"LiveDataEditor";

            List<string> physicsInspectorFiles = new List<string>() {
                    "PhysicsInspector.html",
                    "PhysicsInspector.js",
                    "PhysicsInspector.css",
                    "PhysicsInspectorCanvas.js",
                    "PhysicsInspectorData.js",
                    "PhysicsInspectorSettings.js",
                    "konva.min.js",
            };
            string physicsInspectorFolder = $"PhysicsInspector";
            
            
            //Delete the old files, that are NOT yet sorted into the new folders
            foreach (string file in externalOverlayFiles) {
                if (File.Exists(GetPathToFile(ExternalToolsFolder, file))) {
                    File.Delete(GetPathToFile(ExternalToolsFolder, file));
                }
            }
            if (Directory.Exists(GetPathToFile(ExternalToolsFolder, "img"))) {
                Directory.Delete(GetPathToFile(ExternalToolsFolder, "img"));
            }
            
            foreach (string file in livedataEditorFiles) {
                if (File.Exists(GetPathToFile(ExternalToolsFolder, file))) {
                    File.Delete(GetPathToFile(ExternalToolsFolder, file));
                }
            }
            foreach (string file in physicsInspectorFiles) {
                if (File.Exists(GetPathToFile(ExternalToolsFolder, file))) {
                    File.Delete(GetPathToFile(ExternalToolsFolder, file));
                }
            }
            

            // common.js
            string alreadyGeneratedPath = GetPathToFile(ExternalToolsFolder, commonJsName);
            if (!File.Exists(alreadyGeneratedPath)) {
                CreateExternalToolFileFromStream(commonJsName, $"{basePath}/{commonJsName}");
            }
            
            //Overlay files
            CheckFolderExists(GetPathToFile(ExternalToolsFolder, externalOverlayFolder));
            alreadyGeneratedPath = GetPathToFile(ExternalToolsFolder, externalOverlayFolder, "CCTOverlay.html");
            if (Util.IsUpdateAvailable(ConsistencyTrackerModule.VersionsCurrent.Overlay, ConsistencyTrackerModule.VersionsNewest.Overlay) || !File.Exists(alreadyGeneratedPath)) {
                Mod.Log($"Updating External Overlay from version {ConsistencyTrackerModule.VersionsCurrent.Overlay ?? "null"} to version {ConsistencyTrackerModule.VersionsNewest.Overlay}");
                ConsistencyTrackerModule.VersionsCurrent.Overlay = ConsistencyTrackerModule.VersionsNewest.Overlay;

                CheckFolderExists(GetPathToFile(ExternalToolsFolder, externalOverlayFolder, "img"));

                foreach (string file in externalOverlayFiles) {
                    CreateExternalToolFileFromStream(file, $"{basePath}/{externalOverlayFolder}/{file}", externalOverlayFolder);
                }
            } else {
                Mod.Log($"External Overlay is up to date at version {ConsistencyTrackerModule.VersionsCurrent.Overlay}");
            }

            //Path Edit Tool files

            //Format Edit Tool files
            CheckFolderExists(GetPathToFile(ExternalToolsFolder, livedataEditorFolder));
            alreadyGeneratedPath = GetPathToFile(ExternalToolsFolder, livedataEditorFolder, "LiveDataEditTool.html");
            if (Util.IsUpdateAvailable(ConsistencyTrackerModule.VersionsCurrent.LiveDataEditor, ConsistencyTrackerModule.VersionsNewest.LiveDataEditor) || !File.Exists(alreadyGeneratedPath)) {
                Mod.Log($"Updating LiveData Editor from version {ConsistencyTrackerModule.VersionsCurrent.LiveDataEditor ?? "null"} to version {ConsistencyTrackerModule.VersionsNewest.LiveDataEditor}");
                ConsistencyTrackerModule.VersionsCurrent.LiveDataEditor = ConsistencyTrackerModule.VersionsNewest.LiveDataEditor;

                foreach (string file in livedataEditorFiles) {
                    CreateExternalToolFileFromStream(file, $"{basePath}/{livedataEditorFolder}/{file}", livedataEditorFolder);
                }
            } else {
                Mod.Log($"LiveData Editor is up to date at version {ConsistencyTrackerModule.VersionsCurrent.LiveDataEditor}");
            }

            //Physics Inspector Tool files
            CheckFolderExists(GetPathToFile(ExternalToolsFolder, physicsInspectorFolder));
            alreadyGeneratedPath = GetPathToFile(ExternalToolsFolder, physicsInspectorFolder, "PhysicsInspector.html");
            if (Util.IsUpdateAvailable(ConsistencyTrackerModule.VersionsCurrent.PhysicsInspector, ConsistencyTrackerModule.VersionsNewest.PhysicsInspector) || !File.Exists(alreadyGeneratedPath)) {
                Mod.Log($"Updating Physics Inspector from version {ConsistencyTrackerModule.VersionsCurrent.PhysicsInspector ?? "null"} to version {ConsistencyTrackerModule.VersionsNewest.PhysicsInspector}");
                ConsistencyTrackerModule.VersionsCurrent.PhysicsInspector = ConsistencyTrackerModule.VersionsNewest.PhysicsInspector;

                foreach (string file in physicsInspectorFiles) {
                    CreateExternalToolFileFromStream(file, $"{basePath}/{physicsInspectorFolder}/{file}", physicsInspectorFolder);
                }
            } else {
                Mod.Log($"Physics Inspector is up to date at version {ConsistencyTrackerModule.VersionsCurrent.PhysicsInspector}");
            }
        }

        private static void CreateExternalToolFileFromStream(string fileName, string assetPath, string subFolder = null) {
            string path = subFolder != null ? Path.Combine(ExternalToolsFolder, subFolder) : ExternalToolsFolder;

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            
            CreateFileFromStream(path, fileName, assetPath);
        }
        private static void CreatePathFileFromStream(string fileName, string assetPath) {
            CreateFileFromStream(PathsFolder, fileName, assetPath);
        }
        private static void CreateFileFromStream(string folder, string fileName, string assetPath) {
            string path = GetPathToFile(folder, fileName);

            Mod.LogVerbose($"Trying to access asset at '{assetPath}'");
            if (Everest.Content.TryGet(assetPath, out ModAsset value, true)) {
                using (var fileStream = File.Create(path)) {
                    value.Stream.Seek(0, SeekOrigin.Begin);
                    value.Stream.CopyTo(fileStream);
                    Mod.LogVerbose($"Wrote file '{fileName}' to path '{path}'");
                }
            } else {
                Mod.Log($"No asset found with content path '{assetPath}'");
            }
        }
        
        private static string PathsFolder => ConsistencyTrackerModule.PathsFolder;
        private static string ExternalToolsFolder => ConsistencyTrackerModule.ExternalToolsFolder;
        
        private static string GetPathToFile(string folder, string fileName) {
            return ConsistencyTrackerModule.GetPathToFile(folder, fileName);
        }
        
        private static string GetPathToFile(string folder, string subFolder, string fileName) {
            return ConsistencyTrackerModule.GetPathToFile(folder, subFolder, fileName);
        }
        
        private static void CheckFolderExists(string folderPath) {
            ConsistencyTrackerModule.CheckFolderExists(folderPath);
        }
    }
}