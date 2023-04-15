using Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageChartTest : SummaryHudPage {

        private LineChart TestChart { get; set; }
        private LineChart TestChartSmall { get; set; }
        private LineChart TestChartVerySmall { get; set; }
        private string DataPointText { get; set; }
        
        public PageChartTest(string name) : base(name) {
            TestChart = new LineChart(new ChartSettings() {
                ChartWidth = 1000,
                ChartHeight = 650,
            });

            TestChartSmall = new LineChart(new ChartSettings() {
                ChartWidth = 200,
                ChartHeight = 150,
                Scale = 0.5f,
                BackgroundColor = new Color(0f, 0f, 0f, 0f),
                YMax = 150,
                YAxisLabelCount = 6,
            });

            TestChartVerySmall = new LineChart(new ChartSettings() {
                ChartWidth = 100,
                ChartHeight = 75,
                Scale = 0.25f,
                AxisLabelFontMult = 0.6f,
                BackgroundColor = new Color(0f, 0f, 0f, 0f),
                ShowXAxisLabels = false,
            });
        }

        public override void Update() {
            base.Update();

            
            List<LineDataPoint> data = new List<LineDataPoint>();
            List<LineDataPoint> data2 = new List<LineDataPoint>();

            Random random = new Random();
            for (int i = 0; i < 10; i++) {
                int randomValue = random.Next(0, 100);
                data.Add(new LineDataPoint() {
                    XAxisLabel = $"R-{i + 1}",
                    Y = randomValue,
                    Label = randomValue.ToString(),
                });

                randomValue = random.Next(0, 100);
                data2.Add(new LineDataPoint() {
                    XAxisLabel = $"R-{i + 1}",
                    Y = randomValue,
                    Label = randomValue.ToString(),
                });
            }

            data[4].Y = float.NaN;

            List<LineSeries> series = new List<LineSeries>() { 
                new LineSeries(){ Data = data, LineColor = Color.Green, PointColor = Color.Green, Depth = -1 },
                new LineSeries(){ Data = data2, LineColor = Color.Red, PointColor = Color.Green, Depth = 0, LineThickness = 12 },
            };
            TestChart.SetSeries(series);

            List<LineSeries> series2 = new List<LineSeries>()  {
                new LineSeries(){ Data = data, LineColor = Color.Blue, PointColor = Color.Green, Depth = 1, LineThickness = 8 },
                new LineSeries(){ Data = data2, LineColor = Color.Red, PointColor = Color.Green, Depth = 0 },
            };
            TestChartSmall.SetSeries(series2);
            TestChartVerySmall.SetSeries(series);

            DataPointText = "Data Points:";
            foreach (LineDataPoint dataPoint in data) {
                DataPointText += $"\n    {dataPoint.XAxisLabel}: {dataPoint.Y}";
            }
        }

        public override void Render() {
            base.Render();

            TestChart.Position = MoveCopy(Position, 50, 0);
            TestChart.Render();

            TestChartSmall.Position = MoveCopy(Position, 50 + TestChart.Settings.ChartWidth + BasicMargin * 5, 0);
            TestChartSmall.Render();

            Vector2 pointer = MoveCopy(TestChartSmall.Position, 0, TestChartSmall.Settings.ChartHeight + BasicMargin * 5);
            TestChartVerySmall.Position = pointer;
            TestChartVerySmall.Render();

            pointer = MoveCopy(pointer, 0, TestChartVerySmall.Settings.ChartHeight + BasicMargin * 3);
            DrawText(DataPointText, pointer, FontMultSmall, Color.White);
        }
    }
}
