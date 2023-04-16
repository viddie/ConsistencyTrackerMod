using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart {
    public class ChartSettings {

        //General
        public float ChartWidth { get; set; }
        public float ChartHeight { get; set; }
        public float Scale { get; set; } = 1;
        public float FontMult { get; set; } = 0.5f;

        //Axis stuff
        public bool ShowXAxis { get; set; } = true;
        public bool ShowYAxis { get; set; } = true;
        public Color AxisColor { get; set; } = Color.Gray;
        public float AxisThickness { get; set; } = 5;
        public float AxisTickLength { get; set; } = 10;
        public float AxisTickThickness { get; set; } = 1;

        public bool ShowXAxisLabels { get; set; } = true;
        public bool ShowYAxisLabels { get; set; } = true;
        public Color AxisLabelColor { get; set; } = Color.White;
        public float AxisLabelFontMult { get; set; } = 1f;

        public float YMin { get; set; } = 0;
        public float YMax { get; set; } = 100;
        public int YAxisLabelCount { get; set; } = 5;
        public Func<float, string> YAxisLabelFormatter { get; set; } = (float value) => value.ToString();

        public float XMin { get; set; } = 0;
        public float XMax { get; set; } = 100;

        //Grid Lines
        public bool ShowGridLines { get; set; } = true;
        public Color GridLineColor { get; set; } = new Color(0.3f, 0.3f, 0.3f, 0.125f);
        public float GridLineThickness { get; set; } = 1;

        //Other Colors
        public Color ValueLabelColor { get; set; } = Color.White;
        public Color BackgroundColor { get; set; } = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        //Legend
        public bool ShowLegend { get; set; } = true;
        public Color? LegendBackgroundColor { get; set; }
        public float LegendFontMult { get; set; } = 1f;
    }
}
