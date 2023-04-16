using Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
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
                AxisLabelFontMult = 1.2f,
                BackgroundColor = new Color(0f, 0f, 0f, 0f),
                ShowXAxisLabels = false,
            });
        }

        public override void Update() {
            base.Update();

            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;

            if (path == null) {
                MissingPath = true;
                return;
            }
            
            
            TestChart.Settings.YMax = path.RoomCount;
            TestChartSmall.Settings.YMax = path.RoomCount;
            TestChartVerySmall.Settings.YMax = path.RoomCount;

            List<LineDataPoint> dataRollingAvg1 = new List<LineDataPoint>();
            List<LineDataPoint> dataRollingAvg3 = new List<LineDataPoint>();
            List<LineDataPoint> dataRollingAvg10 = new List<LineDataPoint>();

            List<double> rollingAvg1 = AverageLastRunsStat.GetRollingAverages(path, stats, 1, stats.CurrentChapterLastGoldenRuns);
            List<double> rollingAvg3 = AverageLastRunsStat.GetRollingAverages(path, stats, 3, stats.CurrentChapterLastGoldenRuns);
            List<double> rollingAvg10 = AverageLastRunsStat.GetRollingAverages(path, stats, 10, stats.CurrentChapterLastGoldenRuns);

            for (int i = 0; i < rollingAvg1.Count; i++) {
                dataRollingAvg1.Add(new LineDataPoint() {
                    XAxisLabel = $"{i + 1}",
                    Y = (float)rollingAvg1[i],
                    Label = rollingAvg1[i].ToString(),
                });

                float val3 = float.NaN;
                if (i > 0 && i < rollingAvg1.Count - 1 && rollingAvg1.Count >= 3) {
                    val3 = (float)rollingAvg3[i - 1];
                }
                dataRollingAvg3.Add(new LineDataPoint() {
                    XAxisLabel = $"{i + 1}",
                    Y = val3,
                });


                float val10 = float.NaN;
                if (i > 3 && i < rollingAvg1.Count - 5 && rollingAvg1.Count >= 10) {
                    val10 = (float)rollingAvg10[i - 4];
                }
                dataRollingAvg10.Add(new LineDataPoint() {
                    XAxisLabel = $"{i + 1}",
                    Y = val10,
                });
            }


            List<LineSeries> series = new List<LineSeries>() {
                new LineSeries(){ Data = dataRollingAvg1, LineColor = Color.LightBlue, Depth = 1, Name = "Run Distances", ShowLabels = true, LabelPosition = LabelPosition.Middle, LabelFontMult = 0.75f },
                new LineSeries(){ Data = dataRollingAvg3, LineColor = new Color(255, 165, 0, 100), Depth = -1, Name = "Avg. over 3" },
                //new LineSeries(){ Data = dataRollingAvg10, LineColor = new Color(255, 165, 0, 100), Depth = -1, Name = "Avg. over 10" },
            };
            TestChart.SetSeries(series);
            TestChartSmall.SetSeries(series);
            TestChartVerySmall.SetSeries(series);

            DataPointText = "Data Points:";
            foreach (LineDataPoint dataPoint in dataRollingAvg1) {
                DataPointText += $"\n    {dataPoint.XAxisLabel}: {dataPoint.Y}";
            }
        }

        public override void Render() {
            base.Render();

            TestChart.Position = MoveCopy(Position, 50, 0);
            TestChart.Render();

            TestChartSmall.Position = MoveCopy(Position, 50 + TestChart.Settings.ChartWidth + BasicMargin * 5, TestChartSmall.Settings.ChartHeight);
            TestChartSmall.Render();

            Vector2 pointer = MoveCopy(TestChartSmall.Position, 0, TestChartSmall.Settings.ChartHeight + BasicMargin * 5);
            TestChartVerySmall.Position = pointer;
            TestChartVerySmall.Render();

            pointer = MoveCopy(pointer, 0, TestChartVerySmall.Settings.ChartHeight + BasicMargin * 3);
            DrawText(DataPointText, pointer, FontMultSmall, Color.White);
        }
    }
}
