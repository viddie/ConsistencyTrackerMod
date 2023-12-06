using Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts;
using Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
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

        private static readonly int TableRowCount = 20;
        
        private LineChart TimeSpentChart { get; set; }
        private Table TimeSpentTable { get; set; }

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

            List<Tuple<string, long>> data = new List<Tuple<string, long>>();
            if (path == null) {
                foreach (var pair in stats.Rooms.OrderBy(p => p.Key)) {
                    RoomStats rStats = pair.Value;
                    long timeSpent = rStats.TimeSpentInRoom;
                    data.Add(Tuple.Create(rStats.DebugRoomName, timeSpent));
                }
            } else {
                foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                    foreach (RoomInfo rInfo in cpInfo.Rooms) {
                        if (!includeTransitionRooms && rInfo.IsNonGameplayRoom) continue;
                        string roomName = rInfo.GetFormattedRoomName(Mod.ModSettings.LiveDataRoomNameDisplayType);
                        long timeSpent = stats.GetRoom(rInfo).TimeSpentInRoom;
                        data.Add(Tuple.Create(roomName, timeSpent));
                    }
                }
            }

            StatCount = 1 + (int)(Math.Floor((float)data.Count / TableRowCount) + 1);
            if (SelectedStat == 0) {
                UpdateChart(data);
            } else {
                UpdateTable(data, SelectedStat - 1);
            }
        }

        public void UpdateChart(List<Tuple<string, long>> data) {
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

        public void UpdateTable(List<Tuple<string, long>> data, int page) {
            DataColumn roomColumn = new DataColumn("Room", typeof(string));
            DataColumn timeSpentColumn = new DataColumn("Time Spent", typeof(long));

            DataTable testData = new DataTable() {
                Columns = { roomColumn, timeSpentColumn }
            };

            int startIndex = page * TableRowCount;
            int index = 0;
            foreach (var item in data) {
                if (index >= startIndex && index < startIndex + TableRowCount) {
                    testData.Rows.Add(item.Item1, item.Item2);
                }
                index++;
            }

            TimeSpentTable.ColSettings = new Dictionary<DataColumn, ColumnSettings>() {
                [roomColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center, MinWidth = 120 },
                [timeSpentColumn] = new ColumnSettings() {
                    Alignment = ColumnSettings.TextAlign.Right,
                    ValueFormatter = (obj) => {
                        long val = (long)obj;
                        return ConsoleCommands.TicksToString(val);
                    }
                }
            };
            TimeSpentTable.Data = testData;
            TimeSpentTable.Settings.Title = $"Time Spent ({page + 1}/{StatCount - 1})";
            TimeSpentTable.Update();
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
            }
        }
    }
}
