﻿using Celeste.Mod.ConsistencyTracker.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses
{
    public class GetFormatListResponse : Response
    {
        public List<StatFormat> customFormats { get; set; }
        public List<StatFormat> defaultFormats { get; set; }
    }
}
