using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart {
    public class LineChart : Chart {

        public LineChart() {
            
        }
        public LineChart(ChartSettings settings) : base(settings) { 
            
        }



        public override void RenderDataPoints() {
            List<float> points = new List<float>() { 
                100,
                90,
                80,
                30,
                40,
                0,
                15,
                45.123f,
            };
            Color color = Color.Green;

            Vector2 prevPosition = Vector2.Zero;
            for (int i = 0; i < points.Count; i++){
                float value = points[i];
                float x = GetXValuePosition(i, points.Count);
                float y = GetYValuePosition(value);
                Vector2 position = new Vector2(x, y);
                Draw.Circle(position, 5, color, 10);

                //For all but the first point, draw a line to the previous point
                if (i > 0) {
                    Draw.Line(position, prevPosition, color, 5 * Settings.Scale);
                }

                prevPosition = position;
            }
        }

        public float GetYValuePosition(float value) {
            float yRange = Settings.YMax - Settings.YMin;
            float yValueRange = value - Settings.YMin;
            float yValuePercent = yValueRange / yRange;
            float yValuePosition = Settings.ChartHeight * yValuePercent;
            return yValuePosition + Position.Y;
        }

        public float GetXValuePosition(int pointIndex, int pointCount) {
            //have the points be placed evenly spaced across the chart
            float xValuePosition = (Settings.ChartWidth / (pointCount - 1)) * pointIndex;
            return xValuePosition + Position.X;
        }
    }
}
