using Celeste.Mod.ConsistencyTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models {
    public class StateResponse : Response {
        public RoomStats currentRoom { get; set; }
        public string chapterName { get; set; }
        public ModState modState { get; set; }
    }
}
