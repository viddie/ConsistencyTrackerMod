using Celeste.Mod.ConsistencyTracker.Entities.Summary.Charts;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageChartTest : SummaryHudPage {

        private LineChart ChokeRateChart { get; set; }

        public PageChartTest(string name) : base(name) {
            StatCount = 2;
            ChokeRateChart = new LineChart(new ChartSettings() {
                ChartWidth = 1140,
                ChartHeight = 595,
                YMin = 1,
                TitleFontMult = 0.8f,
                TitleMargin = 15f,
                LegendFontMult = 0.75f,
            });
        }

        public override string GetName() {
            return $"{Name} ({SelectedStat+1}/{StatCount})";
        }

        public override void Update() {
            base.Update();

            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;

            if (path == null) {
                MissingPath = true;
                return;
            }

            StatCount = stats.OldSessions.Count + 2; //+1 for overall and current session
            bool isOverall = SelectedStat == 0;
            bool isCurrentSession = SelectedStat == 1;
            OldSession oldSession = isOverall ? null : isCurrentSession ? null : stats.OldSessions[stats.OldSessions.Count - (SelectedStat - 1)];

            List<string> lastRuns = oldSession?.LastGoldenRuns ?? stats.LastGoldenRuns;
            List<RoomInfo> lastRunsRooms = path.GetRoomsForLastRuns(lastRuns);

            string language = Dialog.Language.Id;
            CultureInfo cultureInfo = new CultureInfo(language == "schinese" ? "zh_cn" : "en_us");

            if (isOverall) {
                ChokeRateChart.Settings.Title = Dialog.Clean("CCT_SUMMARY_CHOKE_RATE_CHART_TITLE_OVERALL");
            } else if (isCurrentSession) {
                ChokeRateChart.Settings.Title = Dialog.Clean("CCT_SUMMARY_CHOKE_RATE_CHART_TITLE_TODAY")
                .Replace("sessionCount", (stats.OldSessions.Count + 1).ToString());
            } else {
                string date;
                if (DateTime.Now.Year != oldSession.SessionStarted.Year) {
                    date = oldSession.SessionStarted.ToString("D", cultureInfo);
                } else {
                    date = oldSession.SessionStarted.ToString("M", cultureInfo);
                }
                ChokeRateChart.Settings.Title = Dialog.Clean("CCT_SUMMARY_CHOKE_RATE_CHART_TITLE")
                .Replace("sessionCount", (stats.OldSessions.Count - (SelectedStat - 2)).ToString())
                .Replace("date", date);
            }

            UpdateChart(path, stats, lastRunsRooms, isOverall);
        }

        public void UpdateChart(PathInfo path, ChapterStats stats, List<RoomInfo> lastRunsRooms, bool isOverall) {
            List<LineDataPoint> dataChokeRates = new List<LineDataPoint>();
            List<LineDataPoint> dataRoomEntries = new List<LineDataPoint>();

            CustomNameBehavior customRoomNameBehavior = Mod.ModSettings.LiveDataCustomNameBehavior;
            if (customRoomNameBehavior != CustomNameBehavior.Ignore && customRoomNameBehavior != CustomNameBehavior.Override) {
                customRoomNameBehavior = CustomNameBehavior.Ignore;
            }

            Dictionary<RoomInfo, Tuple<int, float>> roomData = null;
            Dictionary<RoomInfo, Tuple<int, float, int, float>> roomDataOverall = null;
            if (isOverall) {
                roomDataOverall = ChokeRateStat.GetRoomData(path, stats);
            } else {
                roomData = ChokeRateStat.GetRoomDataSession(path, lastRunsRooms);
            }

            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.GameplayRooms) {
                    int roomEntries = 0;
                    float successRate = 0f;

                    if (isOverall) {
                        Tuple<int, float, int, float> data = roomDataOverall[rInfo];
                        roomEntries = data.Item1;
                        successRate = data.Item2;
                    } else {
                        Tuple<int, float> data = roomData[rInfo];
                        roomEntries = data.Item1;
                        successRate = data.Item2;
                    }

                    
                    if (float.IsNaN(successRate)) {
                        successRate = 1;
                    }
                    float chokeRate = 1 - successRate;

                    dataChokeRates.Add(new LineDataPoint() {
                        XAxisLabel = $"{rInfo.GetFormattedRoomName(StatManager.RoomNameType, customRoomNameBehavior)}",
                        Y = chokeRate * 100,
                        Label = $"{Math.Round(chokeRate * 100, 2)}%",
                    });

                    dataRoomEntries.Add(new LineDataPoint() {
                        XAxisLabel = $"{rInfo.GetFormattedRoomName(StatManager.RoomNameType, customRoomNameBehavior)}",
                        Y = roomEntries,
                        Label = $"{roomEntries}",
                    });
                }
            }

            string name1 = Dialog.Clean("CCT_SUMMARY_CHOKE_RATE");
            string name2 = Dialog.Clean("CCT_SUMMARY_ROOM_ENTRIES");

            LineSeries data1 = new LineSeries() { Data = dataChokeRates, LineColor = Color.LightBlue, Depth = 1, Name = name1, ShowLabels = true, LabelPosition = LabelPosition.Top, LabelFontMult = 0.6f };
            LineSeries data2 = new LineSeries() { Data = dataRoomEntries, LineColor = Color.Orange, Depth = 0, Name = name2, ShowLabels = true, LabelPosition = LabelPosition.Middle, LabelFontMult = 0.6f, IndepedentOfYAxis = true, IndependentYMin = 0f };

            List<LineSeries> series = new List<LineSeries>() { data1, data2 };
            ChokeRateChart.Settings.YMax = 100;
            ChokeRateChart.Settings.YMin = 0;
            ChokeRateChart.Settings.XAxisLabelFontMult = 1f;
            ChokeRateChart.Settings.YAxisLabelFormatter = (float value) => $"{value}%";

            if (dataChokeRates.Count > 30) {
                ChokeRateChart.Settings.XAxisLabelFontMult = 0.4f;
            } else if (dataChokeRates.Count > 20) {
                ChokeRateChart.Settings.XAxisLabelFontMult = 0.6f;
            } else if (dataChokeRates.Count > 10) {
                ChokeRateChart.Settings.XAxisLabelFontMult = 0.8f;
            }

            ChokeRateChart.SetSeries(series);
        }

        public override void Render() {
            base.Render();

            if (MissingPath) return;

            Vector2 pointer = Position;

            Move(ref pointer, 80, 0);
            ChokeRateChart.Position = pointer;
            ChokeRateChart.Render();
        }
    }
}
