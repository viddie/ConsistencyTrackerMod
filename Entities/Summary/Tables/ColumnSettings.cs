using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables {
    public class ColumnSettings {
        public enum TextAlign {
            Left,
            Center,
            Right
        }

        public TextAlign Alignment { get; set; } = TextAlign.Left;
        public Func<object, string> ValueFormatter { get; set; } = (obj) => obj.ToString();

        public float? MinWidth { get; set; }

    }
}
