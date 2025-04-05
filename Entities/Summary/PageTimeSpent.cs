using Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts;
using Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.Mod.ConsistencyTracker.Entities.Summary.PageOverall;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageTimeSpent : SummaryHudPage {

        private static readonly int TableRowCount = 24;

        private LineChart TimeSpentChart { get; set; }
        private Table TimeSpentTable { get; set; }
        private Table TimeSpentAggregateTable { get; set; }

        public PageTimeSpent(string name) : base(name) {
            TimeSpentChart = new LineChart(new ChartSettings() {
                ChartWidth = 1140,
                ChartHeight = 595,
                YMin = 1,
                TitleFontMult = 0.8f,
                TitleMargin = 15f,
                LegendFontMult = 0.75f,
            });

            TimeSpentTable = new Table() {
                Settings = new TableSettings() {
                    FontMultAll = 0.5f,
                }
            };
            TimeSpentAggregateTable = new Table() {
                Settings = new TableSettings() {
                    FontMultAll = 0.5f,
                }
            };
        }

        public override string GetName() {
            return $"{Name} ({SelectedStat + 1}/{StatCount})";
        }

        public override void Update() {
            base.Update();

            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;
            bool includeTransitionRooms = false;

            if (stats == null) {
                return;
            }

            List<Tuple<string, long, long, long>> data = new List<Tuple<string, long, long, long>>();
            long totalTime = 0;
            long totalTimeInRuns = 0;
            long totalTimeCasual = 0;
            if (path == null || path.GameplayRoomCount == 0) {
                foreach (var pair in stats.Rooms.OrderBy(p => p.Key)) {
                    RoomStats rStats = pair.Value;
                    long timeSpent = rStats.TimeSpentInRoom;
                    long timeSpentRuns = rStats.TimeSpentInRoomInRuns;
                    long timeSpentCasual = rStats.TimeSpentInRoomFirstPlaythrough;
                    data.Add(Tuple.Create(rStats.DebugRoomName, timeSpent, timeSpentRuns, timeSpentCasual));

                    totalTime += timeSpent;
                    totalTimeInRuns += timeSpentRuns;
                    totalTimeCasual += timeSpentCasual;
                }
            } else {
                foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                    foreach (RoomInfo rInfo in cpInfo.Rooms) {
                        if (!includeTransitionRooms && rInfo.IsNonGameplayRoom) continue;
                        string roomName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType);
                        RoomStats rStats = stats.GetRoom(rInfo);
                        long timeSpent = rStats.TimeSpentInRoom;
                        long timeSpentRuns = rStats.TimeSpentInRoomInRuns;
                        long timeSpentCasual = rStats.TimeSpentInRoomFirstPlaythrough;
                        data.Add(Tuple.Create(roomName, timeSpent, timeSpentRuns, timeSpentCasual));

                        totalTime += timeSpent;
                        totalTimeInRuns += timeSpentRuns;
                        totalTimeCasual += timeSpentCasual;
                    }
                }
            }


            List<Tuple<string, long, long, long>> aggregatedData = new List<Tuple<string, long, long, long>>();
            aggregatedData.Add(Tuple.Create("Total", totalTime, totalTimeInRuns, totalTimeCasual));
            //Sum up all room times for each checkpoint
            if (path != null) {
                foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                    long timeSpent = 0;
                    long timeSpentRuns = 0;
                    long timeSpentCasual = 0;
                    foreach (RoomInfo rInfo in cpInfo.Rooms) {
                        if (!includeTransitionRooms && rInfo.IsNonGameplayRoom) continue;
                        string roomName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType);
                        RoomStats rStats = stats.GetRoom(rInfo);
                        timeSpent += rStats.TimeSpentInRoom;
                        timeSpentRuns += rStats.TimeSpentInRoomInRuns;
                        timeSpentCasual += rStats.TimeSpentInRoomFirstPlaythrough;
                    }
                    aggregatedData.Add(Tuple.Create($"{cpInfo.Name} ({cpInfo.Abbreviation})", timeSpent, timeSpentRuns, timeSpentCasual));
                }
            }


            StatCount = 1 + (int)(Math.Ceiling((float)data.Count / TableRowCount));
            if (SelectedStat == 0) {
                UpdateChart(data, aggregatedData);
            } else {
                UpdateTable(data, aggregatedData, SelectedStat - 1);
            }
        }

        public void UpdateChart(List<Tuple<string, long, long, long>> data, List<Tuple<string, long, long, long>> aggregatedData) {
            List<LineDataPoint> dataChokeRates = new List<LineDataPoint>();

            foreach (var item in data) {
                dataChokeRates.Add(new LineDataPoint() {
                    XAxisLabel = $"{item.Item1}",
                    Y = item.Item2 / 1000 / 10000,
                    Label = $"{ConsoleCommands.TicksToString(item.Item2)}",
                });
            }

            long maxTime = data.Max(p => p.Item2);
            float timeInSeconds = maxTime / 1000 / 10000;
            //Round to next 1 hour
            maxTime = (long)Math.Ceiling(timeInSeconds / 3600) * 3600 * 1000 * 10000;
                

            string name1 = "Time Spent";
            LineSeries data1 = new LineSeries() { Data = dataChokeRates, LineColor = Color.LightBlue, Depth = 1, Name = name1, ShowLabels = true, LabelPosition = LabelPosition.Top, LabelFontMult = 0.6f };

            List<LineSeries> series = new List<LineSeries>() { data1 };
            TimeSpentChart.Settings.YMax = maxTime / 1000 / 10000;
            TimeSpentChart.Settings.YMin = 0;
            TimeSpentChart.Settings.XAxisLabelFontMult = 1f;
            TimeSpentChart.Settings.YAxisLabelFormatter = (float value) => $"{ConsoleCommands.TicksToString((long)(value * 1000 * 10000))}";

            if (dataChokeRates.Count > 30) {
                TimeSpentChart.Settings.XAxisLabelFontMult = 0.4f;
            } else if (dataChokeRates.Count > 20) {
                TimeSpentChart.Settings.XAxisLabelFontMult = 0.6f;
            } else if (dataChokeRates.Count > 10) {
                TimeSpentChart.Settings.XAxisLabelFontMult = 0.8f;
            }

            TimeSpentChart.SetSeries(series);
        }

        public void UpdateTable(List<Tuple<string, long, long, long>> data, List<Tuple<string, long, long, long>> aggregatedData, int page) {
            DataColumn roomColumn = new DataColumn("Room", typeof(string));
            DataColumn timeSpentCasualColumn = new DataColumn("First Playthrough", typeof(long));
            DataColumn timeSpentPracticeColumn = new DataColumn("Practice", typeof(long));
            DataColumn timeSpentRunsColumn = new DataColumn("Runs", typeof(long));
            DataColumn timeSpentTotalColumn = new DataColumn("Total", typeof(long));
            
            DataColumn totalLabelColumn = new DataColumn("Room", typeof(string));
            DataColumn totalTimeSpentCasualColumn = new DataColumn("First Playthrough", typeof(long));
            DataColumn totalTimeSpentPracticeColumn = new DataColumn("Practice", typeof(long));
            DataColumn totalTimeSpentRunsColumn = new DataColumn("Runs", typeof(long));
            DataColumn totalTimeSpentTotalColumn = new DataColumn("Total", typeof(long));

            DataTable timeDataTable = new DataTable() {
                Columns = { roomColumn, timeSpentCasualColumn, timeSpentPracticeColumn, timeSpentRunsColumn, timeSpentTotalColumn }
            };
            DataTable aggregateDataTable = new DataTable() {
                Columns = { totalLabelColumn, totalTimeSpentCasualColumn, totalTimeSpentPracticeColumn, totalTimeSpentRunsColumn, totalTimeSpentTotalColumn }
            };

            int startIndex = page * TableRowCount;
            int index = 0;
            foreach (var item in data) {
                if (index >= startIndex && index < startIndex + TableRowCount) {
                    timeDataTable.Rows.Add(item.Item1, item.Item4, item.Item2 - item.Item3 - item.Item4, item.Item3, item.Item2);
                }
                index++;
            }

            foreach (var item in aggregatedData) {
                aggregateDataTable.Rows.Add(item.Item1, item.Item4, item.Item2 - item.Item3 - item.Item4, item.Item3, item.Item2);
            }

            ColumnSettings colSettings = new ColumnSettings() {
                Alignment = ColumnSettings.TextAlign.Right,
                ValueFormatter = (obj) => {
                    long val = (long)obj;
                    return ConsoleCommands.TicksToString(val);
                }
            };

            TimeSpentTable.ColSettings = new Dictionary<DataColumn, ColumnSettings>() {
                [roomColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center, MinWidth = 120 },
                [timeSpentCasualColumn] = colSettings,
                [timeSpentPracticeColumn] = colSettings,
                [timeSpentRunsColumn] = colSettings,
                [timeSpentTotalColumn] = colSettings,
            };
            TimeSpentTable.Data = timeDataTable;
            TimeSpentTable.Settings.Title = $"Time Per Room ({page + 1}/{StatCount - 1})";
            TimeSpentTable.Update();


            TimeSpentAggregateTable.ColSettings = new Dictionary<DataColumn, ColumnSettings>() {
                [totalLabelColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center, MinWidth = 120 },
                [totalTimeSpentCasualColumn] = colSettings,
                [totalTimeSpentPracticeColumn] = colSettings,
                [totalTimeSpentRunsColumn] = colSettings,
                [totalTimeSpentTotalColumn] = colSettings,
            };
            TimeSpentAggregateTable.Data = aggregateDataTable;
            TimeSpentAggregateTable.Settings.Title = $"Total Stats";
            TimeSpentAggregateTable.Update();
        }


        public override void Render() {
            base.Render();

            Vector2 pointer = Position;

            if (SelectedStat == 0) {
                Move(ref pointer, 100, 0);
                TimeSpentChart.Position = pointer;
                TimeSpentChart.Render();
            } else {
                Move(ref pointer, 0, 0);
                TimeSpentTable.Position = pointer;
                TimeSpentTable.Render();


                //===== Separator =====
                Vector2 separator = MoveCopy(pointer, PageWidth / 2, 0);
                Draw.Line(separator, MoveCopy(separator, 0, PageHeight), Color.Gray, 2);

                pointer = MoveCopy(separator, 20, 0);
                TimeSpentAggregateTable.Position = pointer;
                TimeSpentAggregateTable.Render();
            }
        }
    }
}
