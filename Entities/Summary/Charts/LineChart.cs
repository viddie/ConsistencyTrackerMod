using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts {
    public class LineChart : Chart {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        private List<LineSeries> AllSeries { get; set; } = new List<LineSeries>();

        private Vector2 DataPointer { get; set; }
        private float XPositionFactor { get; set; }

        public LineChart() {
            
        }
        public LineChart(ChartSettings settings) : base(settings) { 
            
        }

        public void SetSeries(List<LineSeries> allSeries) {
            if (allSeries.Count == 0) {
                throw new Exception("Must have at least one series");
            }
            
            int seriesLength = -1;
            foreach (LineSeries series in allSeries) {
                if (seriesLength == -1) {
                    seriesLength = series.Data.Count;
                } else if (seriesLength != series.Data.Count) {
                    throw new Exception("All series must have the same number of data points");
                }
            }

            allSeries.Sort((seriesA, seriesB) => seriesA.Depth.CompareTo(seriesB.Depth));
            XPositionFactor = Settings.ChartWidth / (allSeries[0].Data.Count - 1);

            AllSeries = allSeries;
        }
        public List<LineSeries> GetSeries() {
            return AllSeries;
        }

        public override void RenderDataPoints(Vector2 pointer) {
            DataPointer = pointer;
            
            foreach (LineSeries series in AllSeries) {
                RenderSeries(series);
            }

            RenderXAxisTicksAndLabels();
        }

        public void RenderSeries(LineSeries series) {
            Vector2 prevPosition = Vector2.Zero;
            LineDataPoint prevPoint = null;

            float seriesMin = series.MinYValue;
            float seriesMax = series.MaxYValue;

            for (int i = 0; i < series.Data.Count; i++) {
                LineDataPoint point = series.Data[i];
                float value = point.Y;
                Vector2 position = Vector2.Zero;

                if (!float.IsNaN(value)) {
                    float x, y;

                    if (series.IndepedentOfYAxis) {
                        x = GetXValuePosition(i);
                        y = GetYValuePosition(value, seriesMin, seriesMax);
                    } else {
                        x = GetXValuePosition(i);
                        y = GetYValuePosition(value);
                    }

                    position = new Vector2(x, y);

                    if (series.PointSize > 0)
                        Draw.Circle(position, series.PointSize * Settings.Scale, point.Color ?? series.PointColor ?? series.LineColor, 10);

                    //For all but the first point, draw a line to the previous point
                    if (i > 0 && prevPosition != Vector2.Zero) {
                        Draw.Line(position, prevPosition, series.LineColor, series.LineThickness * Settings.Scale);
                    }
                }

                //Draw the label for the previous point
                if (i > 1 && series.ShowLabels && prevPosition != Vector2.Zero) {
                    float strokeThickness = Math.Max(1, 2 * Settings.Scale);
                    Color strokeColor = Color.Black;
                    string label = prevPoint.Label ?? Settings.YAxisLabelFormatter(prevPoint.Y);
                    
                    if (label != "") { //If label is an empty string, DONT draw this particular label
                        if (series.LabelPosition == LabelPosition.Middle) {
                            ActiveFont.DrawOutline(label, prevPosition, new Vector2(0.5f, 0.5f), Vector2.One * Settings.FontMult * series.LabelFontMult * Settings.Scale, Settings.AxisLabelColor, strokeThickness, strokeColor);
                        } else {
                            Vector2 labelPosition = DrawHelper.MoveCopy(prevPosition, 0, 10 * Settings.Scale * (series.LabelPosition == LabelPosition.Top ? -1 : 1));
                            Vector2 justify = new Vector2(0.5f, series.LabelPosition == LabelPosition.Top ? 1f : 0f);

                            //Draw line to label position
                            Draw.Line(prevPosition, labelPosition, Settings.GridLineColor, Settings.GridLineThickness * Settings.Scale);

                            //Draw label
                            ActiveFont.DrawOutline(label, labelPosition, justify, Vector2.One * Settings.FontMult * series.LabelFontMult * Settings.Scale, Settings.AxisLabelColor, strokeThickness, strokeColor);
                        }
                    }
                }

                prevPosition = position;
                prevPoint = point;
            }
        }

        public void RenderXAxisTicksAndLabels() {
            if (AllSeries.Count == 0) return;

            for (int i = 0; i < AllSeries[0].Data.Count; i++) {
                LineDataPoint point = AllSeries[0].Data[i];
                float x = GetXValuePosition(i);
                float y = DataPointer.Y + Settings.ChartHeight;
                Vector2 position = new Vector2(x, y);

                float oddHeightMult = AllSeries[0].Data.Count > 20 ? 2.5f : 1f;
                float heightMult = i % 2 == 1 ? oddHeightMult : 1f;

                //Draw the tick
                Vector2 down = DrawHelper.MoveCopy(position, 0, Settings.AxisTickLength * Settings.Scale * heightMult);
                Draw.Line(position, down, Settings.AxisColor, Math.Max(Settings.AxisTickThickness * Settings.Scale, 1));

                //Draw the label
                if (Settings.ShowXAxisLabels) {
                    string label = point.XAxisLabel;
                    if (!string.IsNullOrEmpty(label)) {
                        Vector2 labelPosition = DrawHelper.MoveCopy(down, 0, 5 * Settings.Scale);
                        ActiveFont.Draw(label, labelPosition, new Vector2(0.5f, 0f), Vector2.One * Settings.FontMult * Settings.XAxisLabelFontMult * Settings.Scale, Settings.AxisLabelColor);
                    }
                }
            }
        }


        public float GetYValuePosition(float value, float min = float.NaN, float max = float.NaN) {
            if (float.IsNaN(min))
                min = Settings.YMin;
            if (float.IsNaN(max))
                max = Settings.YMax;

            float yRange = max - min;
            float yValueRange = value - min;
            float yValuePercent = yValueRange / yRange;
            float yValuePosition = Settings.ChartHeight - (Settings.ChartHeight * yValuePercent);
            return yValuePosition + DataPointer.Y;
        }

        public float GetXValuePosition(int pointIndex) {
            //have the points be placed evenly spaced across the chart
            float xValuePosition = XPositionFactor * pointIndex;
            return xValuePosition + DataPointer.X;
        }

        public override List<Tuple<string, Color>> GetLegendEntries() {
            return AllSeries.Select(series => new Tuple<string, Color>(series.Name, series.LineColor)).ToList();
        }
    }
}
