using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Enums {
    public enum ListFormat {
        Plain, //Entry1, Entry2, Entry3, Entry4, Entry5     OR     85%, 90%, 75%, 95%, 100%, 95%, 95%, 50%
        Json,  //'Entry1', 'Entry2', 'Entry', 'Entry4'      OR     0.85, 0.9, 0.75, 0.95, 1, 0.95, 0.95, 0.5
    }
}
