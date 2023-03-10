using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses
{
    public class SettingsResponse : Response
    {
        public Settings settings { get; set; }



        public class Settings
        {
            public General general { get; set; }
            public RoomAttemptsDisplay roomAttemptsDisplay { get; set; }
            public GoldenShareDisplay goldenShareDisplay { get; set; }
            public GoldenPBDisplay goldenPBDisplay { get; set; }
            public ChapterBarDisplay chapterBarDisplay { get; set; }
            public TextStatsDisplay textStatsDisplay { get; set; }
        }

        public class ChapterBarDisplay
        {
            public bool enabled { get; set; }
            public int borderWidthMultiplier { get; set; }
            public float lightGreenCutoff { get; set; }
            public float greenCutoff { get; set; }
            public float yellowCutoff { get; set; }
        }

        public class General
        {
            public int refreshTimeSeconds { get; set; }
            public int attemptsCount { get; set; }
            public int textOutlineSize { get; set; }
            public string fontFamily { get; set; }
            public bool colorblindMode { get; set; }
        }

        public class GoldenPBDisplay
        {
            public bool enabled { get; set; }
        }

        public class GoldenShareDisplay
        {
            public bool enabled { get; set; }
            public bool showSession { get; set; }
        }

        public class RoomAttemptsDisplay
        {
            public bool enabled { get; set; }
        }

        public class TextStatsDisplay
        {
            public bool enabled { get; set; }
            public string preset { get; set; }
            public bool leftEnabled { get; set; }
            public bool middleEnabled { get; set; }
            public bool rightEnabled { get; set; }
        }
    }
}
