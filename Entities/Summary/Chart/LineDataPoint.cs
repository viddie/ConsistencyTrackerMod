using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart {
    public class LineDataPoint {
        public float Y { get; set; }
        public string Label { get; set; }
        public string XAxisLabel { get; set; }
        public Color? Color { get; set; } = null;
    }
}
