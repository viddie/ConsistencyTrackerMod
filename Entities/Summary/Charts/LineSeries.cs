using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts {
    public class LineSeries {

        public string Name { get; set; }
        public List<LineDataPoint> Data { get; set; } = new List<LineDataPoint>();
        public float MaxYValue { get => Data.Count == 0 ? float.NaN : Data.Max(point => point.Y); }
        public float MinYValue { get => Data.Count == 0 ? float.NaN : Data.Min(point => point.Y); }

        public float PointSize { get; set; } = 0f;
        public Color LineColor { get; set; } = Color.White;
        public Color? PointColor { get; set; } = null;
        public float LineThickness { get; set; } = 3f;

        //Labels
        public bool ShowLabels { get; set; } = false;
        public float LabelFontMult { get; set; } = 1f;
        public LabelPosition LabelPosition { get; set; } = LabelPosition.Top;

        public bool IndepedentOfYAxis { get; set; } = false;
        public float? IndependentYMin { get; set; } = null;
        public float? IndependentYMax { get; set; } = null;

        public int Depth { get; set; } = 0;
    }
}
