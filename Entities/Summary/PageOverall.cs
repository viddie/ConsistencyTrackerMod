using Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts;
using Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageOverall : SummaryHudPage {

        public enum SubPage { 
            PersonalBest = 0,
            AverageRunDistances = 1,
            TotalSuccessRate = 2,
            ChokeRateTable = 3,
        }
        
        public string TextAttemptCount { get; set; }
        public string TextAvgSuccessRate { get; set; }
        public string TextRedRooms { get; set; }
        

        private readonly int countBestRuns = 5;
        private List<ProgressBar> BestRunsProgressBars { get; set; }
        private List<Tuple<string, int>> BestRunsData { get; set; } = new List<Tuple<string, int>>();
        private int ChapterRoomCount { get; set; }

        private Table ChokeRateTable { get; set; }
        private int ChokeRateTablePageSize { get; set; } = 20;
        private Table CheckpointStatsTable { get; set; }

        
        private float ChartWidth = 610;
        private float ChartHeight = 420;
        private LineChart PersonalBestChart { get; set; }
        private LineChart AvgRunDistancesChart { get; set; }
        private LineChart TotalSuccessRateChart { get; set; }


        public PageOverall(string name) : base(name) {
            int barWidth = 400;
            int barHeight = 13;
            BestRunsProgressBars = new List<ProgressBar>();
            for (int i = 0; i < countBestRuns; i++) {
                BestRunsProgressBars.Add(new ProgressBar(0, 100) { BarWidth = barWidth, BarHeight = barHeight });
            }

            ChokeRateTable = new Table() {
                Settings = new TableSettings() {
                    FontMultAll = 0.5f,
                    //BackgroundColorEven = new Color(0.1f, 0.1f, 0.1f, 0.1f),
                    //BackgroundColorOdd = new Color(0.2f, 0.2f, 0.2f, 0.25f),
                },
            };

            CheckpointStatsTable = new Table() {
                Settings = new TableSettings() { 
                    FontMultAll = 0.5f,
                },
            };

            PersonalBestChart = new LineChart(new ChartSettings() {
                ChartWidth = ChartWidth,
                ChartHeight = ChartHeight,
                YMin = 1,
                YAxisLabelFontMult = 0.5f,
                TitleFontMult = 0.75f,
                LegendFontMult = 0.5f,
                Title = "Personal Best over all sessions",
            });

            AvgRunDistancesChart = new LineChart(new ChartSettings() {
                ChartWidth = ChartWidth,
                ChartHeight = ChartHeight,
                YMin = 0,
                YAxisLabelFontMult = 0.5f,
                TitleFontMult = 0.75f,
                LegendFontMult = 0.5f,
                Title = "Average Run Distances over all sessions",
                YAxisLabelFormatter = (value) => Math.Round(value, 2).ToString(),
            });

            TotalSuccessRateChart = new LineChart(new ChartSettings() {
                ChartWidth = ChartWidth,
                ChartHeight = ChartHeight,
                YMin = 0,
                YMax = 1,
                YAxisLabelFontMult = 0.5f,
                TitleFontMult = 0.75f,
                LegendFontMult = 0.5f,
                Title = "Total Success Rate over all sessions",
                YAxisLabelFormatter = (value) => $"{Math.Round(value * 100, 2)}%",
            });

            StatCount = 3;
        }

        public override string GetName() {
            return $"{Name} ({SelectedStat + 1}/{StatCount})";
        }

        public override void Update() {
            base.Update();

            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;

            if (path == null) {
                MissingPath = true;
                return;
            }

            string format = "Golden Deaths Total: {chapter:goldenDeaths}";
            TextAttemptCount = Stats.FormatVariableFormat(format);

            format = "Average Success Rate: {chapter:successRate}" +
                "\nChapter Golden Chance: {chapter:goldenChance}";
            TextAvgSuccessRate = Stats.FormatVariableFormat(format);


            //Checkpoint Stats Table
            DataColumn checkpointName = new DataColumn("Checkpoint", typeof(string));
            DataColumn cpSuccessRate = new DataColumn("Success Rate", typeof(string));
            DataColumn cpGoldenChance = new DataColumn("Golden Chance", typeof(string));
            DataTable cpData = new DataTable();
            cpData.Columns.AddRange(new DataColumn[] { checkpointName, cpSuccessRate, cpGoldenChance });
            CheckpointStatsTable.ColSettings = new Dictionary<DataColumn, ColumnSettings>() {
                [checkpointName] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center },
                [cpSuccessRate] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Right },
                [cpGoldenChance] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Right },
            };
            
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                cpData.Rows.Add(cpInfo.Name, StatManager.FormatPercentage(cpInfo.Stats.SuccessRate), StatManager.FormatPercentage(cpInfo.Stats.GoldenChance));
            }

            CheckpointStatsTable.Data = cpData;
            CheckpointStatsTable.Update();


            //Best runs
            ChapterRoomCount = path.GameplayRoomCount;
            for (int i = 0; i < countBestRuns; i++) {
                string[] split = Stats.FormatVariableFormat($"{{pb:best#{i + 1}}};{{pb:bestRoomNumber#{i + 1}}}").Split(';');
                string bestRoom = split[0];
                string bestRoomNumberStr = split[1];

                if (!int.TryParse(bestRoomNumberStr, out int bestRoomNumber)) {
                    bestRoomNumber = 0;
                }

                BestRunsData.Add(Tuple.Create(bestRoom, bestRoomNumber));
            }


            format = "Red Rooms\n{chapter:listColor-red}";
            TextRedRooms = Stats.FormatVariableFormat(format);

            int chokeRateTablePages = (int)Math.Ceiling(((float)path.GameplayRoomCount) / 20);
            StatCount = 3 + chokeRateTablePages;
            UpdateRightColumn(path, stats);
        }

        public void UpdateRightColumn(PathInfo path, ChapterStats stats) {
            if (SelectedStat == (int)SubPage.PersonalBest) {
                UpdatePersonalBestChart(path, stats);
            } else if (SelectedStat == (int)SubPage.AverageRunDistances) {
                UpdateAverageRunDistanceChart(path, stats);
            } else if (SelectedStat == (int)SubPage.TotalSuccessRate) {
                UpdateTotalSuccessRateChart(path, stats);
            } else if (SelectedStat >= (int)SubPage.ChokeRateTable) {
                int tablePage = SelectedStat - (int)SubPage.ChokeRateTable;
                UpdateChokeRateTable(path, stats, tablePage);
            }
        }

        public void UpdatePersonalBestChart(PathInfo path, ChapterStats stats) {
            PersonalBestChart.Settings.YMax = path.GameplayRoomCount;

            List<LineDataPoint> dataOverallPB = new List<LineDataPoint>();
            List<LineDataPoint> dataSessionPB = new List<LineDataPoint>();

            // Add Old Sessions
            int sessionNumber = 1;
            for (int i = 0; i < stats.OldSessions.Count; i++) {
                OldSession session = stats.OldSessions[i];

                RoomInfo overallPBRoom = path.GetRoom(session.PBRoomName);
                RoomInfo sessionPBRoom = path.GetRoom(session.SessionPBRoomName);

                float value = float.NaN;
                string label = null;
                if (overallPBRoom != null) { 
                    value = overallPBRoom.RoomNumberInChapter;
                    int goldenDeaths = session.PBRoomDeaths;
                    string addition = goldenDeaths > 1 ? $" x{goldenDeaths}" : "";
                    label = $"{overallPBRoom.GetFormattedRoomName(StatManager.RoomNameType)}{addition}";
                }
                dataOverallPB.Add(new LineDataPoint() {
                    Y = value,
                    XAxisLabel = $"#{sessionNumber}",
                    Label = label,
                });


                value = float.NaN;
                label = null;
                if (sessionPBRoom != null) { 
                    value = sessionPBRoom.RoomNumberInChapter;
                    int goldenDeaths = session.SessionPBRoomDeaths;
                    string addition = goldenDeaths > 1 ? $" x{goldenDeaths}" : "";
                    label = $"{overallPBRoom.GetFormattedRoomName(StatManager.RoomNameType)}{addition}";
                }
                dataSessionPB.Add(new LineDataPoint() { 
                    Y = value,
                    XAxisLabel = $"#{sessionNumber}",
                    Label = label,
                });

                if (dataOverallPB[dataOverallPB.Count - 1].Label == dataSessionPB[dataSessionPB.Count - 1].Label) {
                    dataSessionPB[dataSessionPB.Count - 1].Label = ""; //Empty label to not have it be drawn
                }

                sessionNumber++;
            }

            // Add Current Session
            RoomInfo currentOverallPB = PersonalBestStat.GetPBRoom(path, stats);
            Tuple<RoomInfo, int> currentSessionPB = PersonalBestStat.GetSessionPBRoomFromLastRuns(path, stats.LastGoldenRuns);
            if (currentSessionPB != null) { //Don't add current session if there is no data yet
                float value = float.NaN;
                string label = null;
                if (currentOverallPB != null) {
                    value = currentOverallPB.RoomNumberInChapter;
                    int goldenDeaths = stats.GetRoom(currentOverallPB).GoldenBerryDeaths;
                    string addition = goldenDeaths > 1 ? $" x{goldenDeaths}" : "";
                    label = $"{currentOverallPB.GetFormattedRoomName(StatManager.RoomNameType)}{addition}";
                }
                dataOverallPB.Add(new LineDataPoint() {
                    Y = value,
                    XAxisLabel = $"Now",
                    Label = label,
                });


                value = float.NaN;
                label = null;
                if (currentSessionPB != null) {
                    value = currentSessionPB.Item1.RoomNumberInChapter;
                    int goldenDeaths = currentSessionPB.Item2;
                    string addition = goldenDeaths > 1 ? $" x{goldenDeaths}" : "";
                    label = $"{currentSessionPB.Item1.GetFormattedRoomName(StatManager.RoomNameType)}{addition}";
                }
                dataSessionPB.Add(new LineDataPoint() {
                    Y = value,
                    XAxisLabel = $"Now",
                    Label = label,
                });

                if (dataOverallPB[dataOverallPB.Count - 1].Label == dataSessionPB[dataSessionPB.Count - 1].Label) {
                    dataSessionPB[dataSessionPB.Count - 1].Label = ""; //Empty label to not have it be drawn
                }
            }
            


            //Make data chart-ready
            LineSeries seriesOverall = new LineSeries() { Data = dataOverallPB, LineColor = Color.LightBlue, Depth = 1, Name = "Overall PB", ShowLabels = true, LabelPosition = LabelPosition.Top, LabelFontMult = 0.75f };
            LineSeries seriesSession = new LineSeries() { Data = dataSessionPB, LineColor = new Color(255, 165, 0, 100), Depth = 2, Name = "Session PB", ShowLabels = true, LabelPosition = LabelPosition.Bottom, LabelFontMult = 0.75f };

            List<LineSeries> series = new List<LineSeries>() { seriesOverall, seriesSession };

            int pointCount = dataOverallPB.Count;
            PersonalBestChart.Settings.XAxisLabelFontMult = 1f;
            if (pointCount > 70) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                PersonalBestChart.Settings.XAxisLabelFontMult = 0.3f;
            } else if (pointCount > 40) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                PersonalBestChart.Settings.XAxisLabelFontMult = 0.5f;
            } else if (pointCount > 25) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                PersonalBestChart.Settings.XAxisLabelFontMult = 0.65f;
            }

            PersonalBestChart.SetSeries(series);
        }

        public void UpdateAverageRunDistanceChart(PathInfo path, ChapterStats stats) {
            AvgRunDistancesChart.Settings.YMax = path.GameplayRoomCount;

            List<LineDataPoint> dataOverallAvg = new List<LineDataPoint>();
            List<LineDataPoint> dataSessionAvg = new List<LineDataPoint>();

            // Add Old Sessions
            int sessionNumber = 1;
            for (int i = 0; i < stats.OldSessions.Count; i++) {
                OldSession session = stats.OldSessions[i];
                dataOverallAvg.Add(new LineDataPoint() {
                    Y = session.AverageRunDistance,
                    XAxisLabel = $"#{sessionNumber}",
                });
                dataSessionAvg.Add(new LineDataPoint() {
                    Y = session.AverageRunDistanceSession,
                    XAxisLabel = $"#{sessionNumber}",
                });

                if (dataOverallAvg[dataOverallAvg.Count - 1].Y == dataSessionAvg[dataSessionAvg.Count - 1].Y) {
                    dataOverallAvg[dataSessionAvg.Count - 1].Label = ""; //Empty label to not have it be drawn
                }

                sessionNumber++;
            }

            // Add Current Session
            Tuple<double, double> runDistanceAvgs = AverageLastRunsStat.GetAverageRunDistance(path, stats);
            if (runDistanceAvgs.Item2 > 0f) {
                dataOverallAvg.Add(new LineDataPoint() {
                    Y = (float)runDistanceAvgs.Item1,
                    XAxisLabel = $"Now",
                });
                dataSessionAvg.Add(new LineDataPoint() {
                    Y = (float)runDistanceAvgs.Item2,
                    XAxisLabel = $"Now",
                });

                if (dataOverallAvg[dataOverallAvg.Count - 1].Y == dataSessionAvg[dataSessionAvg.Count - 1].Y) {
                    dataOverallAvg[dataSessionAvg.Count - 1].Label = ""; //Empty label to not have it be drawn
                }
            }
            

            //Make data chart-ready
            LineSeries seriesOverall = new LineSeries() { Data = dataOverallAvg, LineColor = Color.LightBlue, Depth = 1, Name = "Overall Avg.", ShowLabels = true, LabelPosition = LabelPosition.Bottom, LabelFontMult = 0.75f };
            LineSeries seriesSession = new LineSeries() { Data = dataSessionAvg, LineColor = new Color(255, 165, 0, 100), Depth = 2, Name = "Session Avg.", ShowLabels = true, LabelPosition = LabelPosition.Top, LabelFontMult = 0.75f };

            List<LineSeries> series = new List<LineSeries>() { seriesOverall, seriesSession };

            int pointCount = dataOverallAvg.Count;
            AvgRunDistancesChart.Settings.XAxisLabelFontMult = 1f;
            if (pointCount > 70) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                AvgRunDistancesChart.Settings.XAxisLabelFontMult = 0.3f;
            } else if (pointCount > 40) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                AvgRunDistancesChart.Settings.XAxisLabelFontMult = 0.5f;
            } else if (pointCount > 25) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                AvgRunDistancesChart.Settings.XAxisLabelFontMult = 0.65f;
            }

            AvgRunDistancesChart.SetSeries(series);
        }

        public void UpdateTotalSuccessRateChart(PathInfo path, ChapterStats stats) {
            List<LineDataPoint> dataSuccessRate = new List<LineDataPoint>();

            // Add Old Sessions
            int sessionNumber = 1;
            for (int i = 0; i < stats.OldSessions.Count; i++) {
                OldSession session = stats.OldSessions[i];
                dataSuccessRate.Add(new LineDataPoint() {
                    Y = session.TotalSuccessRate,
                    XAxisLabel = $"#{sessionNumber}",
                });
                sessionNumber++;
            }

            float lastSessionSR = float.NaN;
            if (stats.OldSessions.Count > 0) {
                lastSessionSR = stats.OldSessions[stats.OldSessions.Count - 1].TotalSuccessRate;
            }

            if (float.IsNaN(lastSessionSR) || lastSessionSR != path.Stats.SuccessRate) {
                // Add Current Session
                dataSuccessRate.Add(new LineDataPoint() {
                    Y = path.Stats.SuccessRate,
                    XAxisLabel = $"Now",
                });
            }
            

            //Make data chart-ready
            LineSeries seriesOverall = new LineSeries() { Data = dataSuccessRate, LineColor = Color.Green, Depth = 1, Name = "Total Success Rate", ShowLabels = true, LabelPosition = LabelPosition.Top, LabelFontMult = 0.75f };
            List<LineSeries> series = new List<LineSeries>() { seriesOverall };

            int pointCount = dataSuccessRate.Count;
            TotalSuccessRateChart.Settings.XAxisLabelFontMult = 1f;
            if (pointCount > 70) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                TotalSuccessRateChart.Settings.XAxisLabelFontMult = 0.3f;
            } else if (pointCount > 40) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                TotalSuccessRateChart.Settings.XAxisLabelFontMult = 0.5f;
            } else if (pointCount > 25) {
                series[0].ShowLabels = false;
                series[1].ShowLabels = false;
                TotalSuccessRateChart.Settings.XAxisLabelFontMult = 0.65f;
            }

            TotalSuccessRateChart.SetSeries(series);
        }

        public void UpdateChokeRateTable(PathInfo path, ChapterStats stats, int tablePage) {
            Dictionary<RoomInfo, Tuple<int, float, int, float>> roomGoldenSuccessRateData = ChokeRateStat.GetRoomData(path, stats);

            //Sort dictionary into List based off the value.Item2 (choke rate) ascending
            //float.NaN should be at the end
            List<KeyValuePair<RoomInfo, Tuple<int, float, int, float>>> sortedRoomGoldenSuccessRateData = roomGoldenSuccessRateData.ToList();

            //Filter out non-gameplay rooms
            sortedRoomGoldenSuccessRateData = sortedRoomGoldenSuccessRateData.Where(pair => !pair.Key.IsNonGameplayRoom).ToList();

            sortedRoomGoldenSuccessRateData.Sort((pair1, pair2) => {
                if (float.IsNaN(pair1.Value.Item2) && float.IsNaN(pair2.Value.Item2) ||
                    pair1.Value.Item2 == pair2.Value.Item2) {
                    return pair1.Key.RoomNumberInChapter.CompareTo(pair2.Key.RoomNumberInChapter);
                }
                if (float.IsNaN(pair1.Value.Item2)) {
                    return 1;
                } else if (float.IsNaN(pair2.Value.Item2)) {
                    return -1;
                } else {
                    return pair1.Value.Item2.CompareTo(pair2.Value.Item2);
                }
            });

            DataColumn roomColumn = new DataColumn("Room", typeof(string));
            DataColumn successRateColumn = new DataColumn("Choke Rate", typeof(float));
            DataColumn deathsColumn = new DataColumn("Successes/Entries", typeof(string));
            DataColumn noteColumn = new DataColumn("Note", typeof(string));

            DataTable testData = new DataTable() {
                Columns = { roomColumn, successRateColumn, deathsColumn, noteColumn }
            };
            
            int earlyRoomBoundary = 0;
            int roomCount = path.GameplayRoomCount;
            if (roomCount > 150) {
                earlyRoomBoundary = 15;
            } else if (roomCount > 100) {
                earlyRoomBoundary = 10;
            } else if (roomCount > 50) {
                earlyRoomBoundary = 7;
            } else if (roomCount > 30) {
                earlyRoomBoundary = 5;
            } else if (roomCount > 20) {
                earlyRoomBoundary = 4;
            } else if (roomCount > 10) {
                earlyRoomBoundary = 3;
            } else if (roomCount > 7) {
                earlyRoomBoundary = 2;
            } else {
                earlyRoomBoundary = 1;
            }

            int startIndex = tablePage * ChokeRateTablePageSize;
            int index = 0;
            foreach (KeyValuePair<RoomInfo, Tuple<int, float, int, float>> kvPair in sortedRoomGoldenSuccessRateData) {
                RoomInfo rInfo = kvPair.Key;
                RoomStats rStats = stats.GetRoom(rInfo);
                Tuple<int, float, int, float> data = kvPair.Value;
                List<string> notes = new List<string>();
                if (rInfo.RoomNumberInChapter <= earlyRoomBoundary) {
                    notes.Add($"early room");
                }
                if (!float.IsNaN(kvPair.Value.Item2) && kvPair.Value.Item1 <= 5) {
                    notes.Add($"few runs");
                }
                string note = $"";
                if (notes.Count > 0) {
                    note = $"({string.Join(", ", notes)})";
                }

                if (index >= startIndex && index < startIndex + ChokeRateTablePageSize) {
                    testData.Rows.Add(rInfo.GetFormattedRoomName(StatManager.RoomNameType), float.IsNaN(data.Item2) ? float.NaN : 1 - data.Item2, $"{data.Item1 - rStats.GoldenBerryDeaths}/{data.Item1}", note);
                }
                index++;
            }


            ChokeRateTable.ColSettings = new Dictionary<DataColumn, ColumnSettings>() {
                [roomColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center, MinWidth = 120 },
                [successRateColumn] = new ColumnSettings() {
                    Alignment = ColumnSettings.TextAlign.Right,
                    ValueFormatter = (obj) => {
                        float val = (float)obj;
                        return float.IsNaN(val) ? "-%" : $"{StatManager.FormatPercentage(val)}";
                    }
                },
                [deathsColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center },
                [noteColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center, MinWidth = 100 }
            };
            ChokeRateTable.Data = testData;
            ChokeRateTable.Settings.Title = $"Table Page ({tablePage + 1}/{StatCount - (int)SubPage.ChokeRateTable})";
            ChokeRateTable.Update();
        }



        public override void Render() {
            base.Render();

            if (MissingPath) return;

            Vector2 pointer = Position + Vector2.Zero;
            Vector2 pointerCol2 = MoveCopy(pointer, PageWidth / 2 - 100, 0);

            //========== Left Column ==========

            //===== Basic Texts =====
            Vector2 measure = DrawText(TextAttemptCount, pointer, FontMultMediumSmall, Color.White);

            Move(ref pointer, 0, measure.Y + BasicMargin);
            measure = DrawText(TextAvgSuccessRate, pointer, FontMultSmall, Color.White);

            //===== Checkpoints Stats Table =====
            Move(ref pointer, 0, measure.Y + BasicMargin);
            CheckpointStatsTable.Position = MoveCopy(pointer, 0, 0);
            CheckpointStatsTable.Render();
            Move(ref pointer, 0, CheckpointStatsTable.TotalHeight + BasicMargin * 2);

            //===== Best Runs Bars =====
            measure = DrawText("Best Runs", pointer, FontMultSmall, Color.White);
            Move(ref pointer, 0, measure.Y + BasicMargin * 3);
            float maxLabelHeight = 0;
            float maxWidth = 0;
            //Determine highest label width
            for (int i = 0; i < countBestRuns; i++) {
                string bestRoom = BestRunsData[i].Item1;
                int bestRoomNumber = BestRunsData[i].Item2;

                measure = ActiveFont.Measure($"{bestRoom} ({bestRoomNumber}/{ChapterRoomCount})") * FontMultSmall;
                if (measure.Y > maxLabelHeight) maxLabelHeight = measure.Y;

                measure = ActiveFont.Measure($"#{i + 1}") * FontMultSmall;
                if (measure.X > maxWidth) maxWidth = measure.X;
            }
            for (int i = 0; i < countBestRuns; i++) {
                string bestRoom = BestRunsData[i].Item1;
                int bestRoomNumber = BestRunsData[i].Item2;

                measure = DrawText($"#{i + 1}", pointer, FontMultSmall, Color.White);

                ProgressBar bar = BestRunsProgressBars[i];
                bar.FontMult = FontMultSmall;
                bar.Value = bestRoomNumber;
                bar.MaxValue = ChapterRoomCount;
                bar.RightLabel = $"{ChapterRoomCount}";
                bar.ValueLabel = bestRoomNumber == 0 ? "" : $"{bestRoom} ({bestRoomNumber}/{ChapterRoomCount})";
                bar.Color = new Color(242, 182, 0);
                bar.Position = MoveCopy(pointer, maxWidth + 10, measure.Y / 2 - bar.BarHeight / 2);
                bar.Render();

                Move(ref pointer, 0, Math.Max(bar.BarHeight, measure.Y) + maxLabelHeight + BasicMargin);
            }
            
            Move(ref pointer, 0, measure.Y - BasicMargin);

            //===== Separator =====
            Vector2 separator = MoveCopy(pointerCol2, -50, 0);
            Draw.Line(separator, MoveCopy(separator, 0, PageHeight), Color.Gray, 2);

            //========== Right Column ==========
            Move(ref pointerCol2, 10, 0);
            RenderRightColumn(pointerCol2);
        }

        public void RenderRightColumn(Vector2 pos) {
            if (SelectedStat == (int)SubPage.PersonalBest) {
                PersonalBestChart.Position = pos;
                PersonalBestChart.Render();

            } else if (SelectedStat == (int)SubPage.AverageRunDistances) {
                AvgRunDistancesChart.Position = pos;
                AvgRunDistancesChart.Render();

            } else if (SelectedStat == (int)SubPage.TotalSuccessRate) {
                TotalSuccessRateChart.Position = pos;
                TotalSuccessRateChart.Render();

            } else if (SelectedStat >= (int)SubPage.ChokeRateTable) {
                ChokeRateTable.Position = pos;
                ChokeRateTable.Render();
            }
        }
    }
}
