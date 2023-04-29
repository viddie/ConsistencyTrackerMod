using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables {
    public class Table : Entity {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        public DataTable Data { get; set; } = new DataTable();
        public Dictionary<DataColumn, ColumnSettings> ColSettings { get; set; } = new Dictionary<DataColumn, ColumnSettings>();
        private ColumnSettings DefaultColSettings { get; set; } = new ColumnSettings();
        public TableSettings Settings { get; set; } = new TableSettings();

        private Dictionary<DataColumn, float> ColumnContentWidths { get; set; } = new Dictionary<DataColumn, float>();
        public float TotalWidth { get; set; } = -1;
        public float TotalHeight { get; set; } = 0;
        private float TitleHeight { get; set; } = 0;

        private ColumnSettings GetColumnSettings(DataColumn col) {
            if (ColSettings.ContainsKey(col)) {
                return ColSettings[col];
            }
            return DefaultColSettings;
        }
        
        public override void Update() {
            base.Update();

            if (Data == null) {
                return;
            }

            ColumnContentWidths = new Dictionary<DataColumn, float>();
            foreach (DataColumn col in Data.Columns) {
                ColumnContentWidths.Add(col, 0);
            }

            foreach (DataRow row in Data.Rows) {
                foreach (DataColumn col in Data.Columns) {
                    ColumnSettings colSettings = GetColumnSettings(col);
                    string text = colSettings.ValueFormatter(row[col]);

                    float width = ActiveFont.Measure(text).X * Settings.FontMultAll * 0.5f;
                    if (width > ColumnContentWidths[col]) {
                        ColumnContentWidths[col] = width;
                    }
                }
            }

            //Measure header and minimum widths
            foreach (DataColumn col in Data.Columns) {
                ColumnSettings colSettings = GetColumnSettings(col);
                if (Settings.ShowHeader) {
                    string text = col.ColumnName;
                    float width = ActiveFont.Measure(text).X * Settings.FontMultAll * 0.5f * Settings.FontMultHeader;
                    if (width > ColumnContentWidths[col]) {
                        ColumnContentWidths[col] = width;
                    }
                }

                if (colSettings.MinWidth.HasValue && colSettings.MinWidth.Value > ColumnContentWidths[col]) {
                    ColumnContentWidths[col] = colSettings.MinWidth.Value;
                }
            }

            //Calculate total width
            //Total width is the sum of all column widths + the width of the border between each column + the padding x2 per column
            TotalWidth = 0;
            foreach (DataColumn col in Data.Columns) {
                TotalWidth += ColumnContentWidths[col];
            }
            TotalWidth += (Data.Columns.Count - 1) * Settings.SeparatorWidth;
            TotalWidth += Data.Columns.Count * Settings.CellPadding * 2;
        }

        public override void Render() {
            base.Render();

            if (Data == null) {
                return;
            }

            Vector2 pointer = Position;

            //Draw title at the very top
            if (!string.IsNullOrEmpty(Settings.Title)) {
                
                Vector2 measure = ActiveFont.Measure(Settings.Title) * Settings.FontMultAll * 0.5f * Settings.FontMultTitle;
                Vector2 titleStart = DrawHelper.MoveCopy(pointer, TotalWidth / 2, measure.Y / 2);
                DrawHelper.DrawText(Settings.Title, titleStart, Settings.FontMultAll * 0.5f * Settings.FontMultTitle, Settings.TextColor, new Vector2(0.5f, 0.5f));
                DrawHelper.Move(ref pointer, 0, measure.Y + 5);
                TitleHeight = measure.Y + 5;
            }

            Vector2 contentStart = DrawHelper.MoveCopy(pointer, 0, 0);
            float rowContentHeight = 0;
            
            if (Settings.ShowHeader) {

                //Draw separator at the top
                Draw.Line(pointer, DrawHelper.MoveCopy(pointer, TotalWidth, 0), Settings.SeparatorColor, Settings.SeparatorWidth);

                //Draw header
                Vector2 headerStart = DrawHelper.MoveCopy(pointer, 0, Settings.SeparatorWidth);
                foreach (DataColumn col in Data.Columns) {
                    Vector2 padded = DrawHelper.MoveCopy(headerStart, Settings.CellPadding, Settings.CellPadding);
                    string text = col.ColumnName;

                    Vector2 measure = ActiveFont.Measure(text) * Settings.FontMultAll * 0.5f * Settings.FontMultHeader;
                    DrawHelper.Move(ref padded, ColumnContentWidths[col] / 2, measure.Y / 2);

                    measure = DrawHelper.DrawText(text, padded, Settings.FontMultAll * 0.5f * Settings.FontMultHeader, Settings.TextColor, new Vector2(0.5f, 0.5f));

                    if (rowContentHeight < measure.Y) rowContentHeight = measure.Y;

                    DrawHelper.Move(ref headerStart, ColumnContentWidths[col] + Settings.CellPadding * 2 + Settings.SeparatorWidth, 0);
                }

                //Draw another separator
                DrawHelper.Move(ref contentStart, 0, Settings.SeparatorWidth + Settings.CellPadding * 2 + rowContentHeight);
                Draw.Line(contentStart, DrawHelper.MoveCopy(contentStart, TotalWidth, 0), Settings.SeparatorColor, Settings.SeparatorWidth);
                DrawHelper.Move(ref contentStart, 0, Settings.SeparatorWidth);
            }


            //Draw content
            int rowNumber = 0;
            foreach (DataRow row in Data.Rows) {
                rowContentHeight = 0;
                //Find the height of the row
                foreach (DataColumn col in Data.Columns) {
                    string text = GetColumnSettings(col).ValueFormatter(row[col]);
                    Vector2 measure = ActiveFont.Measure(text) * Settings.FontMultAll * 0.5f;
                    if (rowContentHeight < measure.Y) rowContentHeight = measure.Y;
                }

                contentStart.Y = (float)Math.Floor(contentStart.Y);

                //Draw row background
                Color bgColor = rowNumber % 2 == 0 ? Settings.BackgroundColorEven : Settings.BackgroundColorOdd;
                Draw.Rect(contentStart, TotalWidth, rowContentHeight + Settings.CellPadding * 2, bgColor);

                //Draw row content
                Vector2 rowStart = DrawHelper.MoveCopy(contentStart, Settings.CellPadding, Settings.CellPadding);
                foreach (DataColumn col in Data.Columns) {
                    ColumnSettings colSettings = GetColumnSettings(col);
                    string text = colSettings.ValueFormatter(row[col]);
                    ColumnSettings.TextAlign align = colSettings.Alignment;

                    Vector2 contentPos;
                    Vector2 justify;

                    switch (align) {
                        case ColumnSettings.TextAlign.Left:
                            contentPos = DrawHelper.MoveCopy(rowStart, 0, rowContentHeight / 2);
                            justify = new Vector2(0, 0.5f);
                            break;
                        case ColumnSettings.TextAlign.Center:
                            contentPos = DrawHelper.MoveCopy(rowStart, ColumnContentWidths[col] / 2, rowContentHeight / 2);
                            justify = new Vector2(0.5f, 0.5f);
                            break;
                        case ColumnSettings.TextAlign.Right:
                            contentPos = DrawHelper.MoveCopy(rowStart, ColumnContentWidths[col], rowContentHeight / 2);
                            justify = new Vector2(1, 0.5f);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    DrawHelper.DrawText(text, contentPos, Settings.FontMultAll * 0.5f, Settings.TextColor, justify);

                    DrawHelper.Move(ref rowStart, ColumnContentWidths[col] + Settings.CellPadding * 2 + Settings.SeparatorWidth, 0);
                }

                //Move contentStart to next row
                DrawHelper.Move(ref contentStart, 0, rowContentHeight + Settings.CellPadding * 2);
                rowNumber++;
            }

            //Extract total height from contentStart, as difference to Position
            TotalHeight = contentStart.Y - Position.Y;

            //Draw vertical separators
            bool isFirst = true;
            foreach (DataColumn col in Data.Columns) {
                if (isFirst) {
                    isFirst = false;
                    DrawHelper.Move(ref pointer, ColumnContentWidths[col] + Settings.CellPadding * 2, 0);
                    continue;
                }

                Draw.Line(pointer, DrawHelper.MoveCopy(pointer, 0, (float)Math.Floor(TotalHeight - TitleHeight)), Settings.SeparatorColor, Settings.SeparatorWidth);
                DrawHelper.Move(ref pointer, ColumnContentWidths[col] + Settings.CellPadding * 2 + Settings.SeparatorWidth, 0);
            }
        }
    }
}
