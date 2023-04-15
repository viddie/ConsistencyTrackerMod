﻿using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart {
    public class LineChart : Chart {

        private List<LineSeries> AllSeries { get; set; } = new List<LineSeries>();

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

        public override void RenderDataPoints() {
            foreach (LineSeries series in AllSeries) {
                RenderSeries(series);
            }

            RenderXAxisTicksAndLabels();
        }

        public void RenderSeries(LineSeries series) {
            Vector2 prevPosition = Vector2.Zero;
            
            for (int i = 0; i < series.Data.Count; i++) {
                LineDataPoint point = series.Data[i];
                float value = point.Y;

                if (float.IsNaN(value)) {
                    prevPosition = Vector2.Zero;
                    continue;
                }
                
                float x = GetXValuePosition(i);
                float y = GetYValuePosition(value);
                Vector2 position = new Vector2(x, y);
                if(series.PointSize > 0)
                    Draw.Circle(position, series.PointSize * Settings.Scale, point.Color ?? series.PointColor, 10);

                //For all but the first point, draw a line to the previous point
                if (i > 0 && prevPosition != Vector2.Zero) {
                    Draw.Line(position, prevPosition, series.LineColor, series.LineThickness * Settings.Scale);
                }

                prevPosition = position;
            }
        }

        public void RenderXAxisTicksAndLabels() {
            for (int i = 0; i < AllSeries[0].Data.Count; i++) {
                LineDataPoint point = AllSeries[0].Data[i];
                float x = GetXValuePosition(i);
                float y = Position.Y + Settings.ChartHeight;
                Vector2 position = new Vector2(x, y);

                //Draw the tick
                Vector2 down = DrawHelper.MoveCopy(position, 0, Settings.AxisTickLength * Settings.Scale);
                Draw.Line(position, down, Settings.AxisColor, Math.Max(Settings.AxisTickThickness * Settings.Scale, 1));

                //Draw the label
                if (Settings.ShowXAxisLabels) {
                    string label = point.XAxisLabel;
                    if (!string.IsNullOrEmpty(label)) {
                        Vector2 labelPosition = DrawHelper.MoveCopy(down, 0, 5 * Settings.Scale);
                        ActiveFont.Draw(label, labelPosition, new Vector2(0.5f, 0f), Vector2.One * Settings.AxisLabelFontMult * Settings.Scale, Settings.AxisLabelColor);
                    }
                }
            }
        }



        public float GetYValuePosition(float value) {
            float yRange = Settings.YMax - Settings.YMin;
            float yValueRange = value - Settings.YMin;
            float yValuePercent = yValueRange / yRange;
            float yValuePosition = Settings.ChartHeight - (Settings.ChartHeight * yValuePercent);
            return yValuePosition + Position.Y;
        }

        public float GetXValuePosition(int pointIndex) {
            //have the points be placed evenly spaced across the chart
            float xValuePosition = XPositionFactor * pointIndex;
            return xValuePosition + Position.X;
        }
    }
}