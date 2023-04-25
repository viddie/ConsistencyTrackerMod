using Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageChartTest : SummaryHudPage {

        private LineChart TestChart { get; set; }

        public PageChartTest(string name) : base(name) {
            StatCount = 2;

            TestChart = new LineChart(new ChartSettings() {
                ChartWidth = 1000,
                ChartHeight = 610,
                YMin = 1,
                TitleFontMult = 0.8f,
            });
        }

        public override string GetName() {
            return $"{Name} ({SelectedStat+1}/{StatCount})";
        }

        public override void Update() {
            base.Update();

            if (Stats.LastPassPathInfo == null) {
                MissingPath = true;
                return;
            }

            if (SelectedStat == 0) {
                UpdateChart2And3(false);
            } else if (SelectedStat == 1) {
                UpdateChart2And3(true);
            }
        }

        public void UpdateChart1() {
            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;
            //ChartTitle = "Run Distances (+Average)";

            TestChart.Settings.YMax = path.RoomCount;
            TestChart.Settings.YMin = 1;

            //List<List<LineDataPoint>> checkpointsData = new List<List<LineDataPoint>>();
            //List<int> checkpointRoomCounts = new List<int>();
            //List<string> checkpointNames = new List<string>();
            //if (path.Checkpoints.Count > 1) {
            //    int totalRoomCount = 0;
            //    for (int i = 0; i < path.Checkpoints.Count; i++) {
            //        if (i > 0) { 
            //            checkpointsData.Add(new List<LineDataPoint>());
            //            checkpointRoomCounts.Add(totalRoomCount+1);
            //            checkpointNames.Add(path.Checkpoints[i].Name);
            //        }
            //        totalRoomCount += path.Checkpoints[i].RoomCount;
            //    }
            //}

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


                //for (int j = 0; j < checkpointsData.Count; j++) {
                //    List<LineDataPoint> checkpointData = checkpointsData[j];
                //    checkpointData.Add(new LineDataPoint() {
                //        XAxisLabel = $"{i + 1}",
                //        Y = checkpointRoomCounts[j],
                //    });
                //}
            }

            int pointCount = dataRollingAvg1.Count;

            LineSeries data1 = new LineSeries() { Data = dataRollingAvg1, LineColor = Color.LightBlue, Depth = 1, Name = "Run Distances", ShowLabels = true, LabelPosition = LabelPosition.Middle, LabelFontMult = 0.75f };
            LineSeries data3 = new LineSeries() { Data = dataRollingAvg3, LineColor = new Color(255, 165, 0, 100), Depth = -1, Name = "Avg. over 3", LabelPosition = LabelPosition.Top, LabelFontMult = 0.75f };
            LineSeries data10 = new LineSeries() { Data = dataRollingAvg10, LineColor = new Color(255, 165, 0, 100), Depth = -1, Name = "Avg. over 10" };

            List<LineSeries> series = new List<LineSeries>() { data1 };

            if (pointCount <= 13) {
                series.Add(data3);
            } else {
                series.Add(data10);
            }

            //Additional Y = C lines
            //for (int i = 0; i < checkpointsData.Count; i++) {
            //    List<LineDataPoint> checkpointData = checkpointsData[i];
            //    series.Add(new LineSeries() {
            //        Data = checkpointData,
            //        LineColor = Color.DarkRed,
            //        Depth = -3,
            //        Name = $"{checkpointNames[i]} Entry",
            //    });
            //}

            TestChart.Settings.XAxisLabelFontMult = 1f;

            if (pointCount > 70) {
                series[0].ShowLabels = false;
                TestChart.Settings.XAxisLabelFontMult = 0.3f;
            } else if (pointCount > 40) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = true;
                TestChart.Settings.XAxisLabelFontMult = 0.5f;
            } else if (pointCount > 25) {
                TestChart.Settings.XAxisLabelFontMult = 0.65f;
            }

            TestChart.SetSeries(series);
        }

        public void UpdateChart2And3(bool isSession) {
            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;

            if (isSession) {
                TestChart.Settings.Title = "Room Entries & Choke Rates (Session)";
            } else {
                TestChart.Settings.Title = "Room Entries & Choke Rates";
            }


            List<LineDataPoint> dataChokeRates = new List<LineDataPoint>();
            List<LineDataPoint> dataRoomEntries = new List<LineDataPoint>();

            Dictionary<RoomInfo, Tuple<int, float, int, float>> roomData = ChokeRateStat.GetRoomData(path, stats);

            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.GameplayRooms) {
                    Tuple<int, float, int, float> data = roomData[rInfo];

                    int roomEntries = !isSession ? data.Item1 : data.Item3;
                    float successRate = !isSession ? data.Item2 : data.Item4;
                    if (float.IsNaN(successRate)) {
                        successRate = 1;
                    }
                    float chokeRate = 1 - successRate;

                    dataChokeRates.Add(new LineDataPoint() {
                        XAxisLabel = $"{rInfo.GetFormattedRoomName(StatManager.RoomNameType)}",
                        Y = chokeRate * 100,
                        Label = $"{Math.Round(chokeRate * 100, 2)}%",
                    });

                    dataRoomEntries.Add(new LineDataPoint() {
                        XAxisLabel = $"{rInfo.GetFormattedRoomName(StatManager.RoomNameType)}",
                        Y = roomEntries,
                    });
                }
            }

            string name1 = isSession ? "Session Choke Rates" : "Choke Rates";
            string name2 = isSession ? "Session Room Entries" : "Room Entries";

            LineSeries data1 = new LineSeries() { Data = dataChokeRates, LineColor = Color.LightBlue, Depth = 1, Name = name1, ShowLabels = true, LabelPosition = LabelPosition.Top, LabelFontMult = 0.6f };
            LineSeries data2 = new LineSeries() { Data = dataRoomEntries, LineColor = Color.Orange, Depth = 0, Name = name2, ShowLabels = true, LabelPosition = LabelPosition.Middle, LabelFontMult = 0.6f, IndepedentOfYAxis = true };

            List<LineSeries> series;
            if (!isSession) {
                series = new List<LineSeries>() { data1, data2 };
            } else {
                series = new List<LineSeries>() { data1, data2 };
            }
            TestChart.Settings.YMax = 100;
            TestChart.Settings.YMin = 0;
            TestChart.Settings.XAxisLabelFontMult = 1f;

            if (dataChokeRates.Count > 30) {
                TestChart.Settings.XAxisLabelFontMult = 0.4f;
            } else if (dataChokeRates.Count > 20) {
                TestChart.Settings.XAxisLabelFontMult = 0.6f;
            } else if (dataChokeRates.Count > 10) {
                TestChart.Settings.XAxisLabelFontMult = 0.8f;
            }

            TestChart.SetSeries(series);
        }

        public override void Render() {
            base.Render();

            Vector2 pointer = Position;

            Move(ref pointer, 50, 0);
            TestChart.Position = pointer;
            TestChart.Render();
        }
    }
}
