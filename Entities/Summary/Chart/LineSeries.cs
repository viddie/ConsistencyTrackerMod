using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart {
    public class LineSeries {

        public List<LineDataPoint> Data { get; set; } = new List<LineDataPoint>();
        public Color PointColor { get; set; } = Color.White;
        public float PointSize { get; set; } = 0f;
        public Color LineColor { get; set; } = Color.White;
        public float LineThickness { get; set; } = 3f;

        public int Depth { get; set; } = 0;
    }
}
