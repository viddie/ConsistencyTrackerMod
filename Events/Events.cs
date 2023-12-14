using Celeste.Mod.ConsistencyTracker.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Events {
    public class Events {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;


        public delegate void GoldenPickup(GoldenType type);
        public static event GoldenPickup OnGoldenPickup;
        public static void InvokeGoldenPickup(GoldenType type) {
            OnGoldenPickup?.Invoke(type);
        }

        public delegate void GoldenDeath();
        public static event GoldenDeath OnGoldenDeath;
        public static void InvokeGoldenDeath() {
            OnGoldenDeath?.Invoke();
        }

        public delegate void GoldenCollect(GoldenType type);
        public static event GoldenCollect OnGoldenCollect;
        public static void InvokeGoldenCollect(GoldenType type) {
            OnGoldenCollect?.Invoke(type);
        }


        public delegate void ChangedRoom();
        public static event ChangedRoom OnChangedRoom;
        public static void InvokeChangedRoom() {
            OnChangedRoom?.Invoke();
        }
        public delegate void ResetSession(bool sameSession);
        /// <summary>
        /// Same session means that the player used the debug map to teleport to a different room. It keeps the session alive, while resetting many other things.
        /// </summary>
        public static event ResetSession OnResetSession;
        public static void InvokeResetSession(bool sameSession) {
            OnResetSession?.Invoke(sameSession);
        }
        public delegate void BeforeSavingStats();
        public static event BeforeSavingStats OnBeforeSavingStats;
        public static void InvokeBeforeSavingStats() {
            OnBeforeSavingStats?.Invoke();
        }
        public delegate void AfterSavingStats();
        public static event AfterSavingStats OnAfterSavingStats;
        public static void InvokeAfterSavingStats() {
            OnAfterSavingStats?.Invoke();
        }


        public delegate void EnteredPbRoomWithGolden();
        public static event EnteredPbRoomWithGolden OnEnteredPbRoomWithGolden;
        public static void InvokeEnteredPbRoomWithGolden() {
            OnEnteredPbRoomWithGolden?.Invoke();
        }
        public delegate void ExitedPbRoomWithGolden();
        public static event ExitedPbRoomWithGolden OnExitedPbRoomWithGolden;
        public static void InvokeExitedPbRoomWithGolden() {
            OnExitedPbRoomWithGolden?.Invoke();
        }
    }
}
