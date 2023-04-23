using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts {
    public abstract class Chart : Entity {
        
        public ChartSettings Settings { get; set; }

        public Chart() {
            Settings = new ChartSettings();
        }
        public Chart(ChartSettings settings) {
            Settings = settings;
        }

        
        public override void Render() {
            base.Render();
            if (Settings.ChartWidth == 0 || Settings.ChartHeight == 0) return;

            Vector2 position = Position;
            Draw.Rect(position, Settings.ChartWidth, Settings.ChartHeight, Settings.BackgroundColor);

            if (Settings.ShowXAxis) {
                RenderXAxis();
            }
            if (Settings.ShowYAxis) {
                RenderYAxis();
            }

            if (Settings.ShowGridLines) {
                RenderYGridLines();
            }

            if (Settings.ShowLegend) {
                RenderLegend();
            }

            RenderDataPoints();
        }

        public void RenderYAxis() {
            Vector2 topLeft = Position;
            Vector2 bottomLeft = DrawHelper.MoveCopy(topLeft, 0, Settings.ChartHeight);
            Draw.Line(topLeft, bottomLeft, Settings.AxisColor, Settings.AxisThickness * Settings.Scale);

            float yRange = Settings.YMax - Settings.YMin;
            int tickCount = Settings.YAxisLabelCount;

            //If min = 0 and max = 100, draw ticks and labels for 0, 25, 50, 75, 100

            float tickSpacing = Settings.ChartHeight / (tickCount - 1);
            float tickValueSpacing = yRange / (tickCount - 1);

            for (int i = 0; i < tickCount; i++) {
                float tickValue = Settings.YMin + (tickValueSpacing * i);
                float tickY = bottomLeft.Y - (tickSpacing * i);
                Vector2 tickStart = new Vector2(bottomLeft.X, tickY);
                Vector2 tickEnd = DrawHelper.MoveCopy(tickStart, -Settings.AxisTickLength * Settings.Scale, 0);
                Draw.Line(tickStart, tickEnd, Settings.AxisColor, Settings.AxisTickThickness);

                if (Settings.ShowYAxisLabels) {
                    string tickLabel = Settings.YAxisLabelFormatter(tickValue);
                    Vector2 tickLabelPosition = DrawHelper.MoveCopy(tickEnd, -5 * Settings.Scale, 0);
                    ActiveFont.Draw(tickLabel, tickLabelPosition, new Vector2(1f, 0.5f), Vector2.One * Settings.FontMult * Settings.YAxisLabelFontMult * Settings.Scale, Settings.AxisLabelColor);
                }
            }
        }
        public void RenderYGridLines() {
            int tickCount = Settings.YAxisLabelCount;
            float tickSpacing = Settings.ChartHeight / (tickCount - 1);

            for (int i = 0; i < tickCount-1; i++) {
                float tickY = Position.Y + (tickSpacing * i);
                Vector2 tickStart = new Vector2(Position.X, tickY);
                Vector2 tickEnd = DrawHelper.MoveCopy(tickStart, Settings.ChartWidth, 0);
                Draw.Line(tickStart, tickEnd, Settings.GridLineColor, Settings.GridLineThickness);
            }
        }

        public void RenderLegend() {
            Vector2 legendPosition = DrawHelper.MoveCopy(Position, Settings.ChartWidth + 30 * Settings.Scale, 30 * Settings.Scale);
            float boxSideLength = 20 * Settings.Scale;

            List<Tuple<string, Color>> series = GetLegendEntries();
            foreach (Tuple<string, Color> entry in series) {
                string name = entry.Item1;
                Color color = entry.Item2;
                //Move into -y and -x direction to draw the box in the color
                Vector2 boxPosition = DrawHelper.MoveCopy(legendPosition, -boxSideLength / 2, -boxSideLength / 2);
                Draw.Rect(boxPosition, boxSideLength, boxSideLength, color);

                //Move into +x direction by the box size + 10 to draw the text
                Vector2 textPosition = DrawHelper.MoveCopy(legendPosition, boxSideLength / 2 + 10 * Settings.Scale, 0);
                Vector2 measure = DrawHelper.DrawText(name, textPosition, Settings.FontMult * Settings.LegendFontMult * Settings.Scale, Settings.AxisLabelColor, new Vector2(0f, 0.5f));

                //Move into +y direction by the text height + 10 to draw the next entry
                legendPosition = DrawHelper.MoveCopy(legendPosition, 0, (Math.Max(measure.Y, boxSideLength)) + 10 * Settings.Scale);
            }
        }

        public void RenderXAxis() {
            Vector2 bottomLeft = DrawHelper.MoveCopy(Position, 0, Settings.ChartHeight);
            Vector2 bottomRight = DrawHelper.MoveCopy(bottomLeft, Settings.ChartWidth, 0);
            Draw.Line(bottomLeft, bottomRight, Settings.AxisColor, Settings.AxisThickness * Settings.Scale);
        }

        
        public abstract List<Tuple<string, Color>> GetLegendEntries();
        public abstract void RenderDataPoints();
    }
}
