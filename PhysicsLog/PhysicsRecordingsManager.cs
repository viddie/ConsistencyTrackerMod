using Monocle;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.ConsistencyTracker.PhysicsLog.PhysicsLogger;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog {
    public class PhysicsRecordingsManager {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        private int MaxLogFiles => 10;

        private static readonly string FolderName = "physics-recordings";
        private static readonly string SavedRecordingsSubFolderName = "saved-recordings";
        private static readonly string RecentRecordingsSubFolderName = "recent-recordings";
        private static readonly string LogFileName = "position-log.txt";
        private static readonly string LayoutFileName = "room-layout.json";
        private static readonly string SavedStateFileName = "saved-recordings.json";

        private PhysicsRecordingsState State { get; set; }
        public static int MostRecentRecording => PhysicsLogger.Settings.IsRecording && Engine.Scene is Level ? 1 : 0;

        public StreamWriter LogWriter = null;

        public PhysicsRecordingsManager() {
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFile(FolderName));
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFile(FolderName, SavedRecordingsSubFolderName));
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFile(FolderName, RecentRecordingsSubFolderName));

            LoadStateFile();
        }

        #region Recent Recording IO

        public void StartRecording() {
            string path = GetPathToFile(RecordingType.Recent, FileType.PhysicsLog, 0);
            Mod.Log($"Started recording of physics to path {path}");
            ShiftFiles(FileType.PhysicsLog);
            ShiftFiles(FileType.Layout);

            LogWriter = new StreamWriter(path);
        }

        public void StopRecording() {
            Mod.Log("Stopping recording.");
            LogWriter.Close();
            LogWriter.Dispose();
            LogWriter = null;
        }

        public void SaveRoomLayoutsToFile(List<PhysicsLogRoomLayout> rooms, string chapterName, string sideName, DateTime recordingStarted, int frameCount) {
            Mod.Log($"Saving room layouts (count: '{rooms.Count}') to file.");

            string pathJson = GetPathToFile(RecordingType.Recent, FileType.Layout, 0);

            PhysicsLogLayoutsFile file = new PhysicsLogLayoutsFile() {
                Name = null,
                ChapterName = chapterName,
                SideName = sideName,
                RecordingStarted = recordingStarted,
                FrameCount = frameCount,
                Rooms = rooms,
            };

            File.WriteAllText(pathJson, JsonConvert.SerializeObject(file));
        }

        public void ShiftFiles(FileType fileType) {
            Mod.Log($"Shifting '{fileType}' files up by 1");
            //logs are stored in numbered files from 0 to MaxLogFiles
            //shift all files up by 1, deleting the last one

            for (int i = MaxLogFiles - 1; i >= 0; i--) {
                string pathFrom = GetPathToFile(RecordingType.Recent, fileType, i);
                string pathTo = GetPathToFile(RecordingType.Recent, fileType, i + 1);

                if (File.Exists(pathFrom)) {
                    File.Move(pathFrom, pathTo);
                }
            }

            //delete the last file
            string pathDelete = GetPathToFile(RecordingType.Recent, fileType, MaxLogFiles);
            if (File.Exists(pathDelete)) {
                File.Delete(pathDelete);
            }
        }

        #endregion

        #region Saved Recordings
        /// <summary>
        /// Load the saved recordings state file.
        /// </summary>
        /// <param name="layoutFile">The layout file</param>
        /// <param name="physicsLog">The physics log</param>
        /// <param name="name">User provided name</param>
        /// <returns>ID of the saved recording</returns>
        public int SaveRecording(PhysicsLogLayoutsFile layoutFile, List<string> physicsLog, string name) {
            int id = State.IDCounter++;
            
            string pathJson = GetPathToFile(RecordingType.Saved, FileType.Layout, id);
            string pathLog = GetPathToFile(RecordingType.Saved, FileType.PhysicsLog, id);

            File.WriteAllText(pathJson, JsonConvert.SerializeObject(layoutFile));
            File.WriteAllLines(pathLog, physicsLog);

            State.SavedRecordings.Add(new PhysicsRecordingsState.PhysicsRecording() {
                ID = id,
                Name = name,
                ChapterName = layoutFile.ChapterName,
                SideName = layoutFile.SideName,
                RecordingStarted = layoutFile.RecordingStarted,
                FrameCount = layoutFile.FrameCount,
            });
            
            SaveStateFile();

            return id;
        }

        /// <summary>
        /// Rename a saved recording.
        /// </summary>
        /// <param name="id">ID of the saved recording</param>
        /// <param name="name">New name of the recording</param>
        /// <returns>True if the recording was renamed, false if it was not found.</returns>
        public bool RenameRecording(int id, string name) {
            PhysicsRecordingsState.PhysicsRecording recording = State.SavedRecordings.Find(r => r.ID == id);
            if (recording == null) return false;
            
            recording.Name = name;
            SaveStateFile();
            return true;
        }


        /// <summary>
        /// Delete a saved recording.
        /// </summary>
        /// <param name="id">ID of the saved recording</param>
        /// <returns>True if the recording was deleted, false if it was not found.</returns>
        public bool DeleteRecording(int id) {
            PhysicsRecordingsState.PhysicsRecording recording = State.SavedRecordings.Find(r => r.ID == id);
            if (recording == null) return false;

            State.SavedRecordings.Remove(recording);
            SaveStateFile();

            string pathJson = GetPathToFile(RecordingType.Saved, FileType.Layout, id);
            string pathLog = GetPathToFile(RecordingType.Saved, FileType.PhysicsLog, id);

            if (File.Exists(pathJson)) File.Delete(pathJson);
            if (File.Exists(pathLog)) File.Delete(pathLog);

            return true;
        }

        /// <summary>
        /// Get a list of all saved recordings.
        /// </summary>
        public List<PhysicsRecordingsState.PhysicsRecording> GetSavedRecordings() {
            return State.SavedRecordings;
        }


        #endregion

        #region State IO
        private void LoadStateFile() {
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FolderName, SavedStateFileName);
            if (File.Exists(stateFilePath)) {
                string stateFileContents = File.ReadAllText(stateFilePath);
                State = JsonConvert.DeserializeObject<PhysicsRecordingsState>(stateFileContents);
            } else {
                State = new PhysicsRecordingsState();
                SaveStateFile();
            }
        }

        private void SaveStateFile() {
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FolderName, SavedStateFileName);
            string stateFileContents = JsonConvert.SerializeObject(State);
            File.WriteAllText(stateFilePath, stateFileContents);
        }
        #endregion
        
        #region Util
        public string GetPathToFile(RecordingType recordingType, FileType fileType, string id) {
            //return a path like "physics-recordings\recent-recordings\{id}_position-log.txt"

            string subFolderName = recordingType == RecordingType.Recent ? RecentRecordingsSubFolderName : SavedRecordingsSubFolderName;
            string fileName = fileType == FileType.PhysicsLog ? LogFileName : LayoutFileName;
            string folderPath = ConsistencyTrackerModule.GetPathToFile(FolderName, subFolderName, $"{id}_{fileName}");
            return folderPath;
        }

        public string GetPathToFile(RecordingType recordingType, FileType fileType, int id) {
            return GetPathToFile(recordingType, fileType, id.ToString());
        }
        #endregion

        #region API Access
        public List<PhysicsLogLayoutsFile> GetRecentRecordingsLayoutFiles() {
            List<PhysicsLogLayoutsFile> files = new List<PhysicsLogLayoutsFile>();
            int startIndex = MostRecentRecording;
            for (int i = startIndex; i < MaxLogFiles; i++) {
                string path = GetPathToFile(RecordingType.Recent, FileType.Layout, i);
                if (File.Exists(path)) {
                    string content = File.ReadAllText(path);
                    files.Add(JsonConvert.DeserializeObject<PhysicsLogLayoutsFile>(content));
                } else {
                    break;
                }
            }
            return files;
        }
        #endregion
    }
}
